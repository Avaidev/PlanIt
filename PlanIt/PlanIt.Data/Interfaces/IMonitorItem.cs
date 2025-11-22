using MongoDB.Bson;

namespace PlanIt.Data.Interfaces;

public interface IMonitorItem
{
    enum TargetTimeContext {NOTIFICATION, ENDING, CYCLED};
    ObjectId Id { get; }
    DateTime TargetTime { get; }
    TargetTimeContext TimeContext { get; }
    int CycleRepeater { get; set; }
    void ExecuteCallback();
}