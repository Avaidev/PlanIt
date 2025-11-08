using System;
using MongoDB.Bson.Serialization.Attributes;

namespace PlanIt.Models;
using MongoDB.Bson;

public class Notification
{
    [BsonId] public ObjectId Id { get; set; }
    [BsonElement("title")] public string Title { get; set; } = "PlanIt Notification";
    [BsonElement("message")] public required string Message { get; set; }
    [BsonElement("date")] public required DateTime Date { get; set; }
    [BsonElement("task")] public ObjectId? Task { get; set; }
}