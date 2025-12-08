using Microsoft.Extensions.Logging;
using Microsoft.Toolkit.Uwp.Notifications;
using PlanIt.Core.Services.Pipe;

namespace PlanIt.Notificator;

class Program
{
    [STAThread]
    static async Task Main(string[] args)
    {
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Information);
            builder.AddConsole();
            builder.AddDebug();
        });
        
        var notificatorLogger = loggerFactory.CreateLogger<NotificationHandler>();
        var pipeLogger = loggerFactory.CreateLogger<PipeClientController>();
        
        var pipeClient = new PipeClientController(pipeLogger);
        var notificator = new NotificationHandler(notificatorLogger, pipeClient);
        await notificator.RunAsync();
    }
}