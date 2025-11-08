using System;
using MongoDB.Bson.Serialization.Attributes;
using ReactiveUI;
using MongoDB.Bson;

namespace PlanIt.Models;

public class Task : ReactiveObject
{
    [BsonId] public ObjectId Id { get; set; }
    [BsonElement("title")] private string title;
    [BsonElement("description")] private string description = "";
    [BsonElement("completed")] private DateTime? completeDate;
    [BsonElement("done")] private bool isDone;
    [BsonElement("important")] private bool isImportant;
    [BsonElement("notification")] public ObjectId? Notification { get; set; }

    [BsonIgnore] public required string Title
    {
        get => title;
        set => this.RaiseAndSetIfChanged(ref title, value);
    }

    [BsonIgnore]
    public string Description
    {
        get => description;
        set => this.RaiseAndSetIfChanged(ref description, value);
    }

    [BsonIgnore]
    public DateTime? CompleteDate
    {
        get => completeDate;
        set => this.RaiseAndSetIfChanged(ref completeDate, value);
    }

    [BsonIgnore]
    public bool IsDone
    {
        get => isDone;
        set => this.RaiseAndSetIfChanged(ref isDone, value);
    }

    [BsonIgnore]
    public bool IsImportant
    {
        get => isImportant;
        set => this.RaiseAndSetIfChanged(ref isImportant, value);
    }
}
