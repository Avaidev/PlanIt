using MongoDB.Bson.Serialization.Attributes;
using ReactiveUI;
using MongoDB.Bson;

namespace PlanIt.Core.Models;

public class Category : ReactiveObject
{
    [BsonId] 
        public ObjectId Id { get; set; } = ObjectId.GenerateNewId();
    [BsonElement("title")] 
        private string _title = "NoNameCategory";
    [BsonElement("color")] 
        private string _color = "Default";
    [BsonElement("icon")] 
        private string _icon = "CubesIcon";
    [BsonElement("tasksCount")] 
        private int  _tasksCount = 0;
    
    public Category(){}

    public Category(Category other)
    {
        Id = other.Id;
        Title = other.Title;
        Color = other.Color;
        Icon = other.Icon;
        TasksCount = other.TasksCount;
    }
        

    [BsonIgnore]
    public string Title
    {
        get => _title;
        set => this.RaiseAndSetIfChanged(ref _title, value);
    }

    [BsonIgnore]
    public string Color
    {
        get => _color;
        set => this.RaiseAndSetIfChanged(ref _color, value);
    }

    [BsonIgnore]
    public string Icon
    {
        get => _icon;
        set => this.RaiseAndSetIfChanged(ref _icon, value);
    }
    
    [BsonIgnore]
    public int TasksCount { get => _tasksCount; set => this.RaiseAndSetIfChanged(ref _tasksCount, value);}

    public void ChangeObject(Category newVariant)
    {
        Title = newVariant.Title;
        Color = newVariant.Color;
        Icon = newVariant.Icon;
        TasksCount = newVariant.TasksCount;
    }
    
    public override string ToString() => Title;
    public override bool Equals(object? obj)
    {
        return Id == (obj as Category)?.Id;
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }
}