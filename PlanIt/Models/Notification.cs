using System;

namespace PlanIt.Models;
using MongoDB.Bson;

public class Notification
{
    public ObjectId Id { get; set; }
    public string Title { get; set; }
    public string Message { get; set; }
    public DateTime Date { get; set; }
    public ObjectId? Task { get; set; }

    public Notification(string title, string message, ObjectId task)
    {
        Title = title;
        Message = message;
        Task = task;
    }

    public Notification()
    {
        Title = "";
        Message = "";
        Date = DateTime.Now;
        Task = null;
    }
}