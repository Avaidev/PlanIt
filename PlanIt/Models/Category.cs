using System;
using MongoDB.Bson.Serialization.Attributes;
using ReactiveUI;
using MongoDB.Bson;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive;

namespace PlanIt.Models;

public class Category : ReactiveObject
{
    [BsonId]
    public ObjectId Id { get; set; }

    [BsonElement("title")] private string _title = "NoNameCategory";
    [BsonElement("color")] private string _color = "Default";
    [BsonElement("icon")] private string _icon = "Cubes";
    [BsonElement("tasks")] private ObservableCollection<Task> _tasks = [];
    
    [BsonIgnore]
    public required string Title
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
    public int TasksCount => Tasks.Count;

    [BsonIgnore]
    public ObservableCollection<Task> Tasks
    {
        get => _tasks;
        set => this.RaiseAndSetIfChanged(ref _tasks, value);
    }

    public ReactiveCommand<Task, Unit> AddTask => ReactiveCommand.Create<Task>(task => Tasks.Add(task));
    public ReactiveCommand<Task, Unit> RemoveTask => ReactiveCommand.Create<Task>(task => Tasks.Remove(task));

}