using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using PlanIt.Data.Interfaces;
using ReactiveUI;

namespace PlanIt.Data.Models;

public class TaskItem : ReactiveObject, ITimedObject
{
    [BsonId] public ObjectId Id { get; set; } = ObjectId.GenerateNewId();
    [BsonElement("title")] private string _title = "NoNameTask";
    [BsonElement("description")] private string _description = "";
    [BsonElement("completed")] private DateTime _completeDate = DateTime.Now.ToUniversalTime();
    [BsonElement("notify")] private DateTime? _notifyDate;
    [BsonElement("repeat")] private int? _repeat;
    [BsonElement("done")] private bool _isDone;
    [BsonElement("important")] private bool _isImportant;
    [BsonElement("categoryId")] private ObjectId? _category;

    public TaskItem(){}
    public TaskItem(TaskItem other)
    {
        Id = other.Id;
        Title = other.Title;
        Description = other.Description;
        CompleteDate = other.CompleteDate;
        Repeat = other.Repeat;
        IsDone = other.IsDone;
        IsImportant = other.IsImportant;
        NotifyDate =  other.NotifyDate;
        Category = other.Category;
        CategoryObject = other.CategoryObject;
    }
        
    [BsonIgnore] public string Title
    {
        get => _title;
        set => this.RaiseAndSetIfChanged(ref _title, value);
    }
    [BsonIgnore] public string Description
    {
        get => _description;
        set => this.RaiseAndSetIfChanged(ref _description, value);
    }
    [BsonIgnore] public DateTime CompleteDate
    {
        get => _completeDate.ToLocalTime();
        set => this.RaiseAndSetIfChanged(ref _completeDate, value.ToUniversalTime());
    }
    [BsonIgnore] public DateTime? NotifyDate
    {
        get => _notifyDate?.ToLocalTime();
        set => this.RaiseAndSetIfChanged(ref _notifyDate, value?.ToUniversalTime());
    }
    [BsonIgnore] public int? Repeat
    {
        get => _repeat;
        set => this.RaiseAndSetIfChanged(ref _repeat, value);
    }
    [BsonIgnore] public bool IsDone
    {
        get => _isDone;
        set => this.RaiseAndSetIfChanged(ref _isDone, value);
    }
    [BsonIgnore] public bool IsImportant
    {
        get => _isImportant;
        set => this.RaiseAndSetIfChanged(ref _isImportant, value);
    }
    [BsonIgnore] public ObjectId? Category
    {
        get => _category;
        set => this.RaiseAndSetIfChanged(ref _category, value);
    }
    [BsonIgnore] public Category? CategoryObject { get; set; }
    [BsonIgnore] public bool IsMissed => !IsDone && CompleteDate < DateTime.Now;
    [BsonIgnore] public DateTime TargetTime => NotifyDate is {} notify && notify > DateTime.Now ? notify : CompleteDate;


    public void ChangeObject(TaskItem newVariant)
    {
        Title = newVariant.Title;
        Description = newVariant.Description;
        CompleteDate = newVariant.CompleteDate;
        Repeat = newVariant.Repeat;
        IsDone = newVariant.IsDone;
        IsImportant = newVariant.IsImportant;
        NotifyDate = newVariant.NotifyDate;
        Category = newVariant.Category;
        CategoryObject = newVariant.CategoryObject;
    }
    public override string ToString()
    {
        return Title;
    }
    public override bool Equals(object? obj)
    {
        return Id == (obj as TaskItem)?.Id;
    }
    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }
}
