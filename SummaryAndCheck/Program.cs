using Hcs.Discord;
using Hcs.LightI18n;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.PostgreSQL;
using Serilog.Sinks.PostgreSQL.ColumnWriters;
using SummaryAndCheck;
using SummaryAndCheck.Models;
using SummaryAndCheck.Options;
using SummaryAndCheck.Services;
using System.Text.RegularExpressions;
#if ANTIBOT_MODE

using var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Debug);
});
var capture = new WebCaptureService(loggerFactory.CreateLogger<WebCaptureService>());
var content = await capture.GetWebPageContent("https://bot.sannysoft.com/", "zh-TW", CancellationToken.None);
Console.WriteLine(Regex.Replace(content, @"\n+", " "));
return 0;
#endif
var host = Host.CreateDefaultBuilder(args)
    .ConfigureLogging((hostContext, log) =>
    {
        log.ClearProviders();
        var serilogConfig = new LoggerConfiguration().Enrich.FromLogContext();
        var logLevelSection = hostContext.Configuration.GetSection("Logging:LogLevel");
        if (Enum.TryParse(logLevelSection["Default"], true, out LogEventLevel defaultLevel))
        {
            serilogConfig.MinimumLevel.Is(defaultLevel);
        }
        else
        {
            serilogConfig.MinimumLevel.Is(LogEventLevel.Information);
        }
        foreach (var overrideConfig in logLevelSection.GetChildren())
        {
            if (overrideConfig.Key != "Default" && Enum.TryParse(overrideConfig.Value, true, out LogEventLevel overrideLevel))
            {
                serilogConfig.MinimumLevel.Override(overrideConfig.Key, overrideLevel);
            }
        }
        log.AddSerilog(serilogConfig
                .WriteTo.Console(LogEventLevel.Verbose, "[{Timestamp:HH:mm:ss} {Level:u3}][{SourceContext}] {Message:lj}{NewLine}{Exception}")
                .WriteTo.PostgreSQL(
                    connectionString: hostContext.Configuration.GetConnectionString("PostgresLog")!,
                    tableName: "SummaryAndCheck",
                    needAutoCreateTable: true,
                    needAutoCreateSchema: true,
                    columnOptions: new Dictionary<string, ColumnWriterBase>
                    {
                                { "message", new RenderedMessageColumnWriter() },
                                { "message_template", new MessageTemplateColumnWriter() },
                                { "level", new LevelColumnWriter(true, NpgsqlTypes.NpgsqlDbType.Varchar) },
                                { "timestamp", new TimestampColumnWriter(NpgsqlTypes.NpgsqlDbType.TimestampTz) },
                                { "exception", new ExceptionColumnWriter() },
                                { "source",new SinglePropertyColumnWriter("SourceContext", PropertyWriteMethod.ToString, NpgsqlTypes.NpgsqlDbType.Text, format: "l") },
                                { "properties", new PropertiesColumnWriter(NpgsqlTypes.NpgsqlDbType.Jsonb) }
                    }
                ).CreateLogger());
    }).ConfigureAppConfiguration((hostContext, config) =>
    {
        config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
              .AddJsonFile($"appsettings.{hostContext.HostingEnvironment.EnvironmentName}.json", optional: true, reloadOnChange: true)
              .AddJsonFile("local.json", optional: false, reloadOnChange: true)
              .AddJsonFile("local.zh-TW.json", optional: true, reloadOnChange: true)
              .AddEnvironmentVariables()
              .AddCommandLine(args);
    })
    .ConfigureServices((hostContext, services) =>
    {
        services.AddLightI18n();
        string? connectionString = hostContext.Configuration.GetConnectionString("Postgres");
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException("未找到名為 'Postgres' 的連接字串。請在 appsettings.json 中配置。");
        }
        services.AddDbContext<SummaryAndCheckDbContext>(options =>
            options.UseNpgsql(connectionString)
        );
        services.AddMemoryCache();
        //host services
        services.AddHostedService<DiscordService>();
        services.AddHostedService<GeminiTaskScheduleService>();
        //services
        services.AddScoped<DbConfigureOptions>();
        services.AddTransient<Main>();
        services.AddScoped<SummaryService>();
        services.AddScoped<GeminiService>();
        services.AddSingleton<WebCaptureService>();
        services.AddHttpClient(Options.DefaultName, client =>
        {
            client.Timeout = TimeSpan.FromMinutes(3);
        });
        services.AddSingleton<SummaryQueue>();
        foreach (var (type, attr) in DiscordCommandTypes.CommandTypes)
        {
            services.AddScoped(type);
            services.AddScoped(attr.RegisterType);
        }
        //options
        services.AddOptions<DiscordOptions>().ConfigureFromDatabase();
        services.AddOptions<GeminiOptions>().ConfigureFromDatabase();
    })
    .Build();
await using var scope = host.Services.CreateAsyncScope();
await host.StartAsync();
var services = scope.ServiceProvider;
var logger = services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Application started");
try
{
    var mainApp = services.GetRequiredService<Main>();
    await mainApp.RunAsync();
    await host.WaitForShutdownAsync();
    logger.LogInformation("Application stoping");
    await host.StopAsync();
    logger.LogInformation("Application stopped");
}
catch (Exception ex)
{
    logger.LogCritical(ex, "main error");
}

