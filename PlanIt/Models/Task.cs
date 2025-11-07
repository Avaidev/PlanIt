using System;

namespace PlanIt.Models;
using MongoDB.Bson;

public class Task
{
    public ObjectId Id { get; set; }
    public required string Title { get; set; }
    public string Description { get; set; } = "";
    public DateTime? CompleteDate { get; set; }
    public bool IsDone { get; set; } = false;
    public bool IsImportant { get; set; } = false;
    public ObjectId? Notification { get; set; }
}