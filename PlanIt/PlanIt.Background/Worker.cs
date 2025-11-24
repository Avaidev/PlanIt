namespace PlanIt.Background;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly NotificationHandler _notificationHandler;

    public Worker(ILogger<Worker> logger, NotificationHandler notificationHandler)
    {
        _logger = logger;
        _notificationHandler = notificationHandler;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("[Worker] Starting at: {time}", DateTimeOffset.Now);
        
        await _notificationHandler.PrepareHandler();
        _notificationHandler.StartHandling();
        OptimizeProcess();
        
        _logger.LogInformation("[Worker] Started successfully");
        
        await WaitForStop(stoppingToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("[Worker] Stopping at: {time}", DateTimeOffset.Now);
        await base.StopAsync(cancellationToken);
        
        _notificationHandler.Dispose();
        
    }

    private async Task WaitForStop(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(1000,  cancellationToken);
        }
    }

    private void OptimizeProcess()
    {
        try
        {
            System.Diagnostics.Process.GetCurrentProcess().PriorityClass =
                System.Diagnostics.ProcessPriorityClass.BelowNormal;

            GC.Collect();
            GC.WaitForPendingFinalizers();

            _logger.LogInformation("[Worker] Process optimized");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Worker] Could not optimize process");
        }
    }
}