using System;
using MongoDB.Bson.Serialization.Attributes;

namespace PlanIt.Models;
using MongoDB.Bson;

public class Notification
{
    [BsonId] public ObjectId Id { get; private set; } = ObjectId.GenerateNewId();
    [BsonElement("title")] public string Title { get; set; } = "PlanIt Notification";
    [BsonElement("message")] public required string Message { get; set; }
    [BsonElement("notify")] public required DateTime Notify { get; set; }
    [BsonElement("repeat")] public DateTime? Repeat { get; set; }
    [BsonElement("task")] public ObjectId? Task { get; set; } // Remove
}