using System;

namespace PlanIt.Models;
using MongoDB.Bson;

public class Notification
{
    public ObjectId Id { get; set; }
    public string Title { get; set; } = "PlanIt Notification";
    public required string Message { get; set; }
    public DateTime Date { get; set; }
    public ObjectId? Task { get; set; }
}