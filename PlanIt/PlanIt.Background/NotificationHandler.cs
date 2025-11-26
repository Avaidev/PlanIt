using MongoDB.Bson;
using PlanIt.Core.Services;
using PlanIt.Core.Services.DateTimeMonitor;
using PlanIt.Core.Services.Pipe;
using PlanIt.Data.Interfaces;
using PlanIt.Data.Models;
using PlanIt.Data.Services;

namespace PlanIt.Background;

public class NotificationHandler : IDisposable
{
    private readonly ILogger<NotificationHandler> _logger;
    private TasksRepository _tasksRepo;
    private TimeMonitor _monitor;
    private TwoWayPipeServer _server;

    public NotificationHandler(ILogger<NotificationHandler> logger, TimeMonitor monitor,TwoWayPipeServer server)
    {
        _server = server;
        _logger = logger;
        _monitor = monitor;
        _tasksRepo = new TasksRepository();
    }

    public async Task PrepareHandler()
    {
        _logger.LogInformation("[NotificationHandler] Preparing handler]");
        await _monitor.PrepareMonitorAsync(new TimeObjectRepositoryAdapter<TaskItem>(_tasksRepo), TasksCallback);
        _server.AddReceivedCallback(OnDataReceived);
        RegisterDailyChecker();
        await Task.Run(RenovateTasksAsync);
    }
    
    public void TasksCallback(ITimedObject obj, IMonitorItem.TargetTimeContext context)
    {
        if (obj is not TaskItem task) return;
        switch (context)
        {
            case IMonitorItem.TargetTimeContext.NOTIFICATION:
            {
                NotificationService.ShowNotificationTask(task);
                break;
            }

            case IMonitorItem.TargetTimeContext.ENDING:
                NotificationService.ShowMissedTask(task);
                SendMissedTask(task.Id);
                break;

            case IMonitorItem.TargetTimeContext.CYCLED:
            default:
                break;
        }
    }

    public void StartHandling()
    {
        _server.StartServer();
        _monitor.StartMonitoring();
    }

    private void RegisterDailyChecker()
    {
        var targetTime = DateTime.Today.AddDays(1);
        NonObjectCallback callback = DailyCheckerCallback;
        var repeat = 24;
        
        _monitor.RegisterNonObjForMonitoring(targetTime, callback, repeat);
    }

    public void SendMissedTask(ObjectId id)
    {
        _ = _server.SendData(id.ToByteArray());
    }
    
    public void OnDataReceived(byte[] data)
    {
        if (data.Length == 12)
        {
            var id = new ObjectId(data);
            _monitor.RemoveMonitor(id);
        }
        else if (data.Length == 13)
        {
            var flag = data[12];
            var id = new ObjectId(data.Take(12).ToArray());

            switch (flag)
            {
                case 0:
                    _monitor.RemoveMonitor(id, false);
                    _monitor.TryAddOne(id);
                    break;
                case 1:
                    _monitor.TryAddOne(id);
                    break;
            }
        }
    }

    public void DailyCheckerCallback()
    {
        _logger.LogInformation("Daily renovating...");
        _monitor.StopMonitoring();
         Task.Run(RenovateTasksAsync).GetAwaiter().GetResult();
        _monitor.ClearAllItems();
        RegisterDailyChecker();
        _monitor.StartMonitoring();
        _ = _server.SendData([1]);
        _logger.LogInformation("Daily renovating completed");
    }

    private async Task RenovateTasksAsync()
    {
        var tasks = await _tasksRepo.GetAll();
        foreach (var task in tasks)
        {
            if (task.Repeat == null) continue;
            var difference = (int)(DateTime.Today - task.CompleteDate.Date).TotalDays;
            
            if (difference <= 0) continue;
            task.IsDone = false;
            var intervalsNum = difference / task.Repeat.Value;
            if (difference % task.Repeat.Value != 0) intervalsNum++;
            
            task.CompleteDate = task.CompleteDate.AddDays(intervalsNum * task.Repeat.Value);
            task.NotifyDate = task.NotifyDate?.AddDays(intervalsNum * task.Repeat.Value);
        }
        
        await _tasksRepo.ReplaceList(tasks);
    }

    public void Dispose()
    {
        _logger.LogInformation("[NotificationHandler] Stopping at: {time}", DateTimeOffset.Now);
        try
        {
            _server.SendData([0]).Wait(2000);
        }
        catch (Exception ex)
        {
            _logger.LogError("[NotificationHandler] Failed to send message when disposing: {message}]", ex.Message);
        }
        _monitor.Dispose();
        _server.Dispose();
    }
}