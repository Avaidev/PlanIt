using MongoDB.Bson;
using PlanIt.Core.Services.DateTimeMonitor;
using PlanIt.Core.Services.Pipe;
using PlanIt.Data.Models;
using PlanIt.Data.Services;

namespace PlanIt.Notifications;

public class NotificationHandler
{
    private TasksRepository _tasksRepo;
    private TimeMonitor _monitor;
    private TwoWayPipeServer _server;

    public NotificationHandler(TimeMonitor monitor,TwoWayPipeServer server, TasksRepository tasksRepo)
    {
        _server = server;
        _monitor = monitor;
        _tasksRepo = tasksRepo;
    }

    public async Task PrepareHandler()
    {
        RegisterDailyChecker();
        TimeMonitorCallbackFuncs.TaskEndingEvent += SendMissedTask;
        await RenovateTasksAsync();
    }

    public void ChangeRepository(TasksRepository repository)
    {
        _tasksRepo = repository;
    }
    public void ChangeMonitor(TimeMonitor monitor)
    {
        _monitor = monitor;
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
        _monitor.StopMonitoring();
        var renovating = Task.Run(RenovateTasksAsync);
        renovating.Wait();
        _monitor.ClearAllItems();
        RegisterDailyChecker();
        _monitor.StartMonitoring();
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
}