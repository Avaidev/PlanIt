using System;

namespace PlanIt.Models;
using MongoDB.Bson;

public class Task
{
    public ObjectId Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public DateTime? CompleteDate { get; set; }
    public bool IsDone { get; set; }
    public bool IsImportant { get; set; }
    public ObjectId? Notification { get; set; }

    public Task(string title, string description, DateTime? completeDate, bool isDone, ObjectId notification, bool isImportant)
    {
        Title = title;
        Description = description;
        CompleteDate = completeDate;
        IsDone = isDone;
        Notification = notification;
        IsImportant = isImportant;
    }

    public Task()
    {
        Title = "";
        Description = "";
        CompleteDate = null;
        IsDone = false;
        IsImportant = false;
        Notification = null;
    }

}