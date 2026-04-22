// SummaryAndCheck/Services/WebSummaryService.cs
using Microsoft.Extensions.Logging;
using Microsoft.Playwright; // 修正命名空間為 Microsoft.Playwright
using System;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SummaryAndCheck.Services
{
    public class WebCaptureService : IAsyncDisposable
    {
        IPlaywright? playwright;
        IBrowser? browser;
        readonly Task initTask;
        private readonly ILogger<WebCaptureService> logger;
        private readonly Lazy<Task<string>> readabilityScriptContentLazy;
        public WebCaptureService(ILogger<WebCaptureService> logger)
        {
            initTask = Task.Run(async () =>
            {
                playwright = await Playwright.CreateAsync();
                browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
                {
                    Headless = true,
                    Channel = "chromium",
                    Args =
    [
        "--no-sandbox",
        "--disable-blink-features=AutomationControlled",
        "--disable-gpu",
        "--enable-webgl",
        "--use-gl=angle",
        "--enable-accelerated-2d-canvas",
        "--disable-software-rasterizer",
    ]
                });

            });
            this.logger = logger;
            readabilityScriptContentLazy = new Lazy<Task<string>>(async () =>
            {
                logger.LogInformation("Loading Readability.js content (Lazy initialization).");
                var assembly = Assembly.GetExecutingAssembly();
                string resourceName = $"{nameof(SummaryAndCheck)}.assets.Readability.js";

                using Stream? stream = assembly.GetManifestResourceStream(resourceName);
                if (stream == null)
                {
                    logger.LogError("Could not find embedded resource: {ResourceName}. Make sure 'Build Action' is set to 'Embedded Resource'.", resourceName);
                    throw new FileNotFoundException($"Embedded resource '{resourceName}' not found.");
                }

                using var reader = new StreamReader(stream, Encoding.UTF8);
                return await reader.ReadToEndAsync();
            }, LazyThreadSafetyMode.ExecutionAndPublication);
        }
        public async ValueTask DisposeAsync()
        {
            await initTask;
            if (browser != null)
            {
                await browser.DisposeAsync();
            }
            playwright?.Dispose();
        }

        /// <summary>
        /// 啟動 Playwright 瀏覽器並擷取指定網址的文字內容。
        /// </summary>
        /// <param name="url">要擷取的網頁網址。</param>
        /// <returns>網頁的純文字內容，如果失敗則返回空字串。</returns>
        public async Task<string> GetWebPageContent(string url, string locale, CancellationToken cancellationToken)
        {
            try
            {
                await initTask;
                await using var context = await browser!.NewContextAsync(new BrowserNewContextOptions
                {
                    UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36 Edg/138.0.0.0", // 使用較新的 Chrome User-Agent
                    ViewportSize = new ViewportSize { Width = 1440, Height = 3440 },
                    DeviceScaleFactor = 1,
                    IsMobile = false,
                    Locale = locale,
                    AcceptDownloads = true
                });
                await context.AddInitScriptAsync(WebCapturePatches.JsPatch);

                var page = await context.NewPageAsync();


                logger.LogInformation("Navigating to URL: {Url}", url);
                async Task ScrollDownAsync()
                {
                    for (var i = 0; i < 3; i++)
                    {
                        await page.Mouse.WheelAsync(0, 1000);
                        await Task.Delay(300, cancellationToken);
                    }
                }
                await page.GotoAsync(url);
                await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

                try
                {
                    await page.WaitForTimeoutAsync(10000);
                    await page.WaitForLoadStateAsync(LoadState.NetworkIdle, new PageWaitForLoadStateOptions { Timeout = 3000 });
                }
                catch (Exception)
                {
                }
                try
                {
                    double previousHeight = -1;
                    const int maxScrollAttempts = 30;
                    int scrollAttempts = 0;
                    while (scrollAttempts < maxScrollAttempts)
                    {
                        double currentHeight = await page.EvaluateAsync<double>("document.documentElement.scrollHeight");
                        logger.LogInformation("Attempt {scrollAttempts}: Current document scrollHeight = {currentHeight}, Previous = {previousHeight}"
                            , scrollAttempts + 1, currentHeight, previousHeight);

                        if (currentHeight == previousHeight)
                        {
                            await page.WaitForTimeoutAsync(1000);
                            double newHeightAfterDelay = await page.EvaluateAsync<double>("document.documentElement.scrollHeight");

                            if (newHeightAfterDelay == currentHeight)
                            {
                                break;
                            }
                        }
                        await ScrollDownAsync();
                        previousHeight = currentHeight;
                        scrollAttempts++;
                        await page.WaitForTimeoutAsync(3000);
                        try
                        {
                            await page.WaitForLoadStateAsync(LoadState.NetworkIdle, new PageWaitForLoadStateOptions { Timeout = 3000 });
                        }
                        catch (Exception)
                        {
                        }
                    }
                }
                catch (Exception)
                {

                }
                var allContent = new StringBuilder();
                var mainDomain = new Uri(url).Host;
                foreach (var frame in page.Frames)
                {
                    if (Uri.TryCreate(frame.Url, UriKind.Absolute, out var frameUri))
                    {
                        if (string.IsNullOrWhiteSpace(frame.Url) || frameUri.Host != mainDomain) continue;
                    }
                    else
                    {
                        continue;
                    }

                    logger.LogInformation("Extracting content from frame: {FrameUrl}", frame.Url);
                    try
                    {
                        var frameContent = await ExtractTextFromFrame(frame);
                        logger.LogInformation("Content extracted from frame: {FrameUrl},Length:{len}", frame.Url, frameContent.Length);
                        if (!string.IsNullOrWhiteSpace(frameContent))
                        {
                            if (allContent.Length > 0)
                            {
                                allContent.AppendLine($"\n--- FRAME CONTENT SEPARATOR[{frame.Name}]({frame.Url}) ---"); // 用分隔符號區分不同 frame 的內容
                            }
                            allContent.Append(frameContent);
                        }
                    }
                    catch (PlaywrightException ex)
                    {
                        logger.LogWarning("Could not extract content from same-origin frame {FrameUrl} due to Playwright error: {Message}", frame.Url, ex.Message);
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning("Could not extract content from same-origin frame {FrameUrl} due to general error: {Message}", frame.Url, ex.Message);
                    }

                }
                return allContent.ToString();
            }
            catch (PlaywrightException ex) // 修正為 PlaywrightException
            {
                logger.LogError(ex, "Playwright 執行錯誤: {Message}", ex.Message);
                return ""; // 失敗時返回空字串
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "網頁內容擷取服務錯誤: {Message}", ex.Message);
                return ""; // 失敗時返回空字串
            }
        }
        private async Task<string> ExtractTextFromFrame(IFrame frame)
        {
            string readabilityJs = await readabilityScriptContentLazy.Value;
            var content = await frame.EvaluateAsync<string>(@"() => {"
                +
                //readabilityJs +
                @"
                
                //try{
                //    var documentClone = document.cloneNode(true);
                //    var article = new Readability(documentClone).parse();
                //    return article.textContent;
                //}catch(e){
                //}
                const bodyClone = document.body.cloneNode(true);
                bodyClone.querySelectorAll('script, style, link, header, nav, footer, aside, .sidebar, .ad, [aria-hidden=true], .hidden, .d-none, .js-hidden').forEach(el => el.remove());
                let text = bodyClone.innerText;
                text = text.replace(/[ \t]+/g, ' ').trim();
                return text;
            }");
            return content?.Trim() ?? string.Empty;
        }
    }
}