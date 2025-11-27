using PlanIt.Core.Services;
using PlanIt.Core.Services.DateTimeMonitor;
using PlanIt.Core.Services.Pipe;
using PlanIt.Notificator;

var builder = Host.CreateApplicationBuilder(args);

bool isAutoStart = args.Contains("--autostart");

if (isAutoStart)
{
    builder.Services.Configure<HostOptions>(options =>
    {
        options.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore;
    });
}

var settings = AppConfigManager.Settings;

builder.Services.AddSingleton<PipeConfig>(config => new PipeConfig
{
    PipeName = settings.PipeName,
    MaxConnections = settings.MaxConnections,
    BufferSize = settings.BufferSize,
});

builder.Services.AddSingleton<TimeMonitor>();
builder.Services.AddSingleton<TwoWayPipeServer>();
builder.Services.AddSingleton<NotificationHandler>();
builder.Services.AddHostedService<Worker>();

if (!isAutoStart)
{
    builder.Logging.AddConsole();
}
builder.Logging.AddDebug();

var host = builder.Build();
await host.RunAsync();