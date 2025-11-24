using PlanIt.Background;
using PlanIt.Core.Services.DateTimeMonitor;
using PlanIt.Core.Services.Pipe;

var builder = Host.CreateApplicationBuilder(args);

bool isAutoStart = args.Contains("--autostart");

if (isAutoStart)
{
    builder.Services.Configure<HostOptions>(options =>
    {
        options.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore;
    });
}

builder.Services.AddSingleton<PipeConfig>(config => new PipeConfig
{
    PipeName = "PlanItPipe",
    MaxConnections = 1,
    BufferSize = 1024,
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