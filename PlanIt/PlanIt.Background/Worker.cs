using MongoDB.Bson;
using PlanIt.Core.Services.DateTimeMonitor;
using PlanIt.Core.Services.Pipe;
using PlanIt.Data.Models;
using PlanIt.Data.Services;
using PlanIt.Notifications;

namespace PlanIt.Background;

public class Worker : BackgroundService
{
    private readonly TimeMonitor _timeMonitor;
    private readonly TwoWayPipeServer _pipeServer;
    private readonly ILogger<Worker> _logger;
    private readonly TasksRepository _repository;
    private readonly NotificationHandler _notificationHandler;

    public Worker(ILogger<Worker> logger, TimeMonitor timeMonitor, TwoWayPipeServer pipeServer)
    {
        _repository = new TasksRepository();
        _logger = logger;
        _timeMonitor = timeMonitor;
        _pipeServer = pipeServer;
        _notificationHandler = new NotificationHandler(_timeMonitor, _pipeServer, _repository);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("[Worker] Starting at: {time}", DateTimeOffset.Now);
        
        await _notificationHandler.PrepareHandler();
        _timeMonitor.SetRepository(new TimeObjectRepositoryAdapter<TaskItem>(_repository));
        _pipeServer.AddCallback(_notificationHandler.OnDataReceived);
        
        OptimizeProcess();
        _pipeServer.StartServer();
        _timeMonitor.StartMonitoring();
        
        _logger.LogInformation("[Worker] Started successfully");
        
        await WaitForStop(stoppingToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("[Worker] Stopping at: {time}", DateTimeOffset.Now);
        
        _timeMonitor.Dispose();
        _pipeServer.Dispose();
        
        await base.StopAsync(cancellationToken);
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