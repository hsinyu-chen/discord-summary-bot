using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Hcs.LightI18n.AspNetCore
{
    public static class LightI18nApplicationBuilderExtensions
    {
        /// <summary>
        /// 將 LightI18n 的語系設定中間件加入到應用程式的 Request Pipeline 中。
        /// 此中間件會自動從 Request 中解析語系並設定作用域。
        /// </summary>
        /// <param name="app">IApplicationBuilder 實例。</param>
        /// <returns>IApplicationBuilder 實例，用於鏈式呼叫。</returns>
        public static IApplicationBuilder UseLightI18nLocalization(this IApplicationBuilder app)
        {
            return app.Use(async (context, next) =>
            {
                string? currentLang = null;

                if (context.Request.Headers.TryGetValue("Accept-Language", out var header))
                    currentLang = header.ToString().Split(',')[0].Split('-')[0].Trim();

                if (context.Request.Query.TryGetValue("lang", out var lang))
                    currentLang = lang.ToString();

                if (!string.IsNullOrEmpty(currentLang))
                {
                    using (L.GetScope(currentLang))
                        await next(context);     // ← 需傳入 context
                }
                else
                {
                    await next(context);
                }
            });
        }
    }
}