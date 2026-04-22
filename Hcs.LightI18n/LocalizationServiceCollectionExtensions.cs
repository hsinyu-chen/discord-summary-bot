using Hcs.LightI18n.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Hcs.LightI18n
{
    public static class LocalizationServiceCollectionExtensions
    {
        public static IServiceCollection AddLightI18n(this IServiceCollection services, string pathPrefix = "Localization")
        {
            services.AddSingleton<LocalizationInitializerSetup>(sp => new(pathPrefix));
            services.AddSingleton<ILocalizationService, LocalizationService>();
            services.AddSingleton<InternalMemoryParsedStringCache>();
            services.AddSingleton<IParsedStringCache>(sp => sp.GetRequiredService<InternalMemoryParsedStringCache>());
            services.AddHostedService<LocalizationInitializerHostedService>();
            return services;
        }
        record class LocalizationInitializerSetup(string PathPrefix)
        {
        }
        private class LocalizationInitializerHostedService(ILocalizationService service, LocalizationInitializerSetup setup) : IHostedService
        {
            public async Task StartAsync(CancellationToken cancellationToken)
            {
                service.PathPrefix = setup.PathPrefix;
                L.Initialize(service);
                await Task.CompletedTask;
            }
            public async Task StopAsync(CancellationToken cancellationToken)
            {
                await Task.CompletedTask;
            }
        }
    }
}
