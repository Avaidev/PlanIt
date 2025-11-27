using MongoDB.Bson;
using PlanIt.Data.Interfaces;

namespace PlanIt.Data.Models;

public delegate void ObjectCallback<in T>(T obj, IMonitorItem.TargetTimeContext context) where T : ITimedObject;
public delegate void NonObjectCallback();

public class MonitorItem<T> : IMonitorItem where T : ITimedObject
{
    public ObjectId Id { get;}
    public T? Item { get; }
    public DateTime TargetTime { get;}
    public IMonitorItem.TargetTimeContext TimeContext { get; }
    public int CycleRepeater { get; set; }
    private ObjectCallback<T>? TimeReachedCallbackObject { get; }
    private NonObjectCallback? TimeReachedCallbackNonObject { get; }

    public MonitorItem(T obj, ObjectCallback<T> callback)
    {
        Item = obj;
        Id = Item.Id;
        TargetTime = Item.TargetTime;
        CycleRepeater = 0;
        TimeContext = Item.NotifyDate is { } notify && notify > DateTime.Now 
            ? IMonitorItem.TargetTimeContext.NOTIFICATION
            : IMonitorItem.TargetTimeContext.ENDING;
        TimeReachedCallbackObject = callback;
    }

    public MonitorItem(DateTime targetTime, NonObjectCallback callback, int repeat = 0)
    {
        Id =  ObjectId.GenerateNewId();
        TargetTime = targetTime;
        TimeReachedCallbackNonObject = callback;
        CycleRepeater = 0;
        if (repeat <= 0) TimeContext = IMonitorItem.TargetTimeContext.ENDING;
        else
        {
            CycleRepeater = repeat;
            TimeContext = IMonitorItem.TargetTimeContext.CYCLED;
        }
    }

    public void ExecuteCallback()
    {
        if (TimeReachedCallbackObject != null) TimeReachedCallbackObject.Invoke(Item!, this.TimeContext);
        else TimeReachedCallbackNonObject?.Invoke();
    }
}