using MongoDB.Bson;

namespace PlanIt.Data.Interfaces;

public interface ITimedObject
{
    ObjectId Id { get; }
    DateTime CompleteDate { get; }
    DateTime? NotifyDate { get; }
    DateTime TargetTime { get; }
    bool IsDone { get; }
}