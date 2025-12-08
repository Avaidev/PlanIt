using PlanIt.Core.Services;
using PlanIt.Core.Services.DateTimeMonitor;
using PlanIt.Core.Services.Pipe;
using PlanIt.MonitorService;

var builder = Host.CreateApplicationBuilder(args);

bool isAutoStart = args.Contains("--autostart");

if (isAutoStart)
{
    builder.Services.Configure<HostOptions>(options =>
    {
        options.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore;
    });
}

builder.Services.AddSingleton<TimeMonitor>();
builder.Services.AddSingleton<PipeServerController>();
builder.Services.AddSingleton<ConditionMonitor>();
builder.Services.AddHostedService<Worker>();

if (!isAutoStart)
{
    builder.Logging.AddConsole();
}
builder.Logging.AddDebug();


var host = builder.Build();
host.Run();