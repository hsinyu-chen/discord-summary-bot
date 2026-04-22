// GeminiService.cs
using Discord;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SummaryAndCheck.Options;

namespace SummaryAndCheck.Services
{
    internal class GeminiTaskScheduleService(SummaryQueue summaryQueue, ILogger<GeminiTaskScheduleService> logger, IOptions<GeminiOptions> options, IServiceProvider serviceProvider) : BackgroundService
    {
        readonly static TimeSpan requestLaunchInterval = TimeSpan.FromMilliseconds(300);
        readonly static TimeSpan queuePollingInterval = TimeSpan.FromMilliseconds(50);
        readonly static TimeSpan hardTaskExcutionLimit = TimeSpan.FromMinutes(5);
        readonly static SemaphoreSlim concurrentTaskLimit = new(2);
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                if (string.IsNullOrEmpty(options.Value.ApiKey))
                {
                    await Task.Delay(5000, stoppingToken);
                    continue;
                }
                if (summaryQueue.TryGetNext(out var next))
                {
                    await concurrentTaskLimit.WaitAsync(stoppingToken);
                    next.State = SummaryState.Running;
                    var request = next.SummaryRequest;
                    var context = request.SummaryContext;
                    var _ = Task.Run(async () =>
                    {
                        try
                        {
                            using var taskScopeCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                            taskScopeCancellationTokenSource.CancelAfter(hardTaskExcutionLimit);
                            await using var scope = serviceProvider.CreateAsyncScope();
                            var geminiService = scope.ServiceProvider.GetRequiredService<GeminiService>();
                            await geminiService.ProcessAsync(next, taskScopeCancellationTokenSource.Token);
                        }
                        catch (Exception ex)
                        {
                            next.State = SummaryState.Failed;
                            logger.LogError(ex, "request excution error ,type:{type},content:{content}", context.Type, context.Content);
                        }
                        finally
                        {
                            try
                            {

                                next.MarkAsDone();
                            }
                            finally
                            {
                                concurrentTaskLimit.Release();
                            }
                        }
                    }, stoppingToken);
                    await Task.Delay(requestLaunchInterval, stoppingToken);
                }
                await Task.Delay(queuePollingInterval, stoppingToken);
            }
        }

    }
}