using System;
using MongoDB.Bson.Serialization.Attributes;
using ReactiveUI;
using MongoDB.Bson;

namespace PlanIt.Models;

public class Task : ReactiveObject
{
    [BsonId] public ObjectId Id { get; } = ObjectId.GenerateNewId();
    [BsonElement("title")] private string _title = "NoNameTask";
    [BsonElement("description")] private string _description = "";
    [BsonElement("completed")] private DateTime? _completeDate;
    [BsonElement("repeat")] private DateTime? _repeat;
    [BsonElement("done")] private bool _isDone;
    [BsonElement("important")] private bool _isImportant;
    [BsonElement("notification")] public ObjectId? Notification { get; set; }
    [BsonElement("category")] private ObjectId? _category;

    [BsonIgnore] public required string Title
    {
        get => _title;
        set => this.RaiseAndSetIfChanged(ref _title, value);
    }

    [BsonIgnore]
    public string Description
    {
        get => _description;
        set => this.RaiseAndSetIfChanged(ref _description, value);
    }

    [BsonIgnore]
    public DateTime? CompleteDate
    {
        get => _completeDate;
        set => this.RaiseAndSetIfChanged(ref _completeDate, value);
    }

    [BsonIgnore]
    public DateTime? Repeat
    {
        get => _repeat;
        set => this.RaiseAndSetIfChanged(ref _repeat, value);
    }

    [BsonIgnore]
    public bool IsDone
    {
        get => _isDone;
        set => this.RaiseAndSetIfChanged(ref _isDone, value);
    }

    [BsonIgnore]
    public bool IsImportant
    {
        get => _isImportant;
        set => this.RaiseAndSetIfChanged(ref _isImportant, value);
    }
    
    [BsonIgnore]
    public ObjectId? Category
    {
        get => _category;
        set => this.RaiseAndSetIfChanged(ref _category, value);
    }

    public override string ToString()
    {
        return Title;
    }

    public override bool Equals(object? obj)
    {
        return Id ==  (obj as Task)?.Id;
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }
}
