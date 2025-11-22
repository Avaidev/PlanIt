using PlanIt.Background;
using PlanIt.Core.Services.DateTimeMonitor;
using PlanIt.Core.Services.Pipe;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "PlanIt.Background";
});

builder.Services.AddSingleton<PipeConfig>(config => new PipeConfig
{
    PipeName = "PlanItPipe",
    MaxConnections = 1,
    BufferSize = 1024,
});

builder.Services.AddSingleton<TimeMonitor>();
builder.Services.AddSingleton<TwoWayPipeServer>();
builder.Services.AddHostedService<Worker>();

builder.Logging.AddConsole();
builder.Logging.AddDebug();

var host = builder.Build();
await host.RunAsync();