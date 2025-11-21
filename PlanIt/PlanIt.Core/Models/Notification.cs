using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace PlanIt.Core.Models;

public class Notification
{
    [BsonId] public ObjectId Id { get; set; } = ObjectId.GenerateNewId();
    [BsonElement("title")] public string Title { get; set; } = "PlanIt Notification";
    [BsonElement("message")] public required string Message { get; set; }

    [BsonElement("notify")] private DateTime _notify;
    [BsonElement("repeat")] public int? Repeat { get; set; }

    [BsonIgnore] public required DateTime Notify
    {
        get => _notify.ToLocalTime();
        set => _notify = value.ToUniversalTime();
    }
}