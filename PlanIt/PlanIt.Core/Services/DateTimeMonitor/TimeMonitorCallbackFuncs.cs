using MongoDB.Bson;
using PlanIt.Data.Interfaces;
using PlanIt.Data.Models;

namespace PlanIt.Core.Services.DateTimeMonitor;

public static class TimeMonitorCallbackFuncs
{
    public static event Action<ObjectId> TaskEndingEvent;
    public static void TaskBasicCallback(TaskItem task, IMonitorItem.TargetTimeContext context)
    {
        switch (context)
        {
            case IMonitorItem.TargetTimeContext.NOTIFICATION:
            {
                NotificationService.ShowNotificationTask(task);
                break;
            }

            case IMonitorItem.TargetTimeContext.ENDING:
                NotificationService.ShowMissedTask(task);
                TaskEndingEvent.Invoke(task.Id);
                break;

            case IMonitorItem.TargetTimeContext.CYCLED:
            default:
                break;
        }
    }

    public static void NonObjectBasicCallback()
    {
        Console.WriteLine($"[NonObjectBasicCallback] Callback in {DateTime.Now}]");
    }
}