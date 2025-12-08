namespace PlanIt.MonitorService;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly ConditionMonitor _conditionMonitor;

    public Worker(ILogger<Worker> logger, ConditionMonitor conditionMonitor)
    {
        _logger = logger;
        _conditionMonitor = conditionMonitor;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("[Worker] Starting at: {time}", DateTimeOffset.Now);
        
        await _conditionMonitor.PrepareHandler();
        _conditionMonitor.StartHandling();
        OptimizeProcess();
        
        _logger.LogInformation("[Worker] Started successfully");
        
        await WaitForStop(stoppingToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("[Worker] Stopping at: {time}", DateTimeOffset.Now);
        await base.StopAsync(cancellationToken);
        
        _conditionMonitor.Dispose();
        
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