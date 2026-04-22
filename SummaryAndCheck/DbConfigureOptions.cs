using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SummaryAndCheck.Models;
using System.Reflection;
using System.Transactions;

namespace SummaryAndCheck
{
    internal class DbConfigHelper(SystemConfig[] systemConfigs)
    {
        public string? Get(string key)
        {
            return systemConfigs.FirstOrDefault(x => x.Key == key)?.Value;
        }
    }
    public static class DbOptionsExtensions
    {
        /// <summary>
        /// 配置 IOptions<T>，使其從資料庫 SystemConfig 表載入組態值。
        /// 此方法依賴於 DbConfigureOptions 服務，該服務必須已註冊到 DI 容器中。
        /// </summary>
        /// <typeparam name="TOptions">要配置的選項類型。</typeparam>
        /// <param name="optionsBuilder">IOptionsBuilder<TOptions> 實例。</param>
        /// <returns>IOptionsBuilder<TOptions> 實例。</returns>
        public static OptionsBuilder<TOptions> ConfigureFromDatabase<TOptions>(
            this OptionsBuilder<TOptions> optionsBuilder)
            where TOptions : class // 限制 TOptions 必須是引用類型，因為它會被 DI 實例化
        {
            // Configure<TService> 允許我們注入 DbConfigureOptions 服務
            optionsBuilder.Configure<DbConfigureOptions>((options, dbConfigService) =>
            {
                dbConfigService.Configure(options);
            });

            return optionsBuilder;
        }
    }
    internal class DbConfigureOptions(SummaryAndCheckDbContext dbContext, ILogger<DbConfigureOptions> logger)
    {
        public void Configure(string scope, Action<DbConfigHelper> config)
        {
            dbContext.Database.EnsureCreated();
            using var transaction = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.ReadUncommitted }, TransactionScopeAsyncFlowOption.Enabled);
            var configs = dbContext.Set<SystemConfig>().AsNoTracking().Where(x => x.Scope == scope).ToArray();
            config(new DbConfigHelper(configs));
            transaction.Complete();
        }
        public void Configure<T>(T options)
        {
            Configure(typeof(T).Name, config =>
            {
                foreach (var p in typeof(T).GetProperties().Where(x => x.CanWrite))
                {
                    if (p.IsDefined(typeof(IgnoreConfigAttribute), true))
                    {
                        continue;
                    }
                    string configKey = p.GetCustomAttribute<ConfigKeyAttribute>()?.KeyName ?? p.Name;
                    var value = config.Get(configKey);
                    if (value != null)
                    {

                        if (p.PropertyType == typeof(string))
                        {
                            p.SetValue(options, value);
                        }
                        else
                        {
                            Type pType;
                            if (p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
                            {
                                pType = p.PropertyType.GetGenericArguments()[0];
                            }
                            else
                            {
                                pType = p.PropertyType;
                            }
                            if (pType.IsAssignableTo(typeof(IConvertible)))
                            {
                                try
                                {
                                    p.SetValue(options, Convert.ChangeType(value, pType, System.Globalization.CultureInfo.InvariantCulture));
                                }
                                catch (Exception ex)
                                {
                                    logger.LogError(ex, "Config value convert failed ,property [{property}],value [{value}]", pType.Name, value);
                                }
                            }
                        }
                    }
                }
            });
        }
    }
    [AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
    public class ConfigKeyAttribute : Attribute
    {
        public string KeyName { get; }
        public ConfigKeyAttribute(string keyName)
        {
            if (string.IsNullOrWhiteSpace(keyName))
            {
                throw new ArgumentException("ConfigKey 必須指定一個非空字串。", nameof(keyName));
            }
            KeyName = keyName;
        }
    }

    [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    public class IgnoreConfigAttribute : Attribute
    {
    }
}
