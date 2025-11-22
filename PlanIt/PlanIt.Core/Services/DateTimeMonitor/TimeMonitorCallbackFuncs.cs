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
                //TODO Send Notification
                break;
            
            case IMonitorItem.TargetTimeContext.ENDING:
                //TODO Send Notification
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