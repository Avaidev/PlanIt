using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using PlanIt.Models;
using PlanIt.Services.DataServices;
using ReactiveUI;

namespace PlanIt.Services;

public class ViewRepository : ReactiveObject
{
    #region Initializing
    public ViewRepository(DbAccessService db)
    {
        _db = db;
        InitializeAsync();
        _tasksCollection = [];
        _nodesCollection = [];
    }

    private async Task InitializeAsync()
    {
        var categories = _db.GetAllCategories();
        var todayCounter = _db.CountTodayTasks();
        var scheduleCounter = _db.CountScheduledTasks();
        var importantCounter = _db.CountImportantTasks();
        var allCounter = _db.CountAllTasks();
        
        CategoriesCollection = new ObservableCollection<Category>(await categories);
        TodayFilterCounter = await todayCounter;
        ScheduleFilterCounter = await scheduleCounter;
        ImportantFilterCounter = await importantCounter;
        AllFilterCounter = await allCounter;
    }
    #endregion
    
    private DbAccessService _db;
    private ObservableCollection<Category> _categoriesCollection;
    private ObservableCollection<TaskItem> _tasksCollection;
    private ObservableCollection<Node> _nodesCollection;
    private Category? _selectedCategory;
    private int _todayFilterCounter;
    private int _scheduleFilterCounter;
    private int _allFilterCounter;
    private int _importantFilterCounter;
    private bool _isDarkTheme = true;

    public ObservableCollection<Category> CategoriesCollection
    {
        get => _categoriesCollection;
        set => this.RaiseAndSetIfChanged(ref _categoriesCollection, value);
    }

    public ObservableCollection<TaskItem> TasksCollection
    {
        get => _tasksCollection;
        set => this.RaiseAndSetIfChanged(ref _tasksCollection, value);
    }

    public ObservableCollection<Node> NodesCollection
    {
        get => _nodesCollection;
        set => this.RaiseAndSetIfChanged(ref _nodesCollection, value);
    }

    public Category? SelectedCategory
    {
        get => _selectedCategory;
        set {
            this.RaiseAndSetIfChanged(ref _selectedCategory, value);
            if (value != null) LoadTasksByCategoryAsync();
        }
    }

    public int TodayFilterCounter
    {
        get => _todayFilterCounter;
        set => this.RaiseAndSetIfChanged(ref _todayFilterCounter, value);
    }

    public int ScheduleFilterCounter
    {
        get => _scheduleFilterCounter;
        set => this.RaiseAndSetIfChanged(ref _scheduleFilterCounter, value);
    }

    public int AllFilterCounter
    {
        get => _allFilterCounter;
        set => this.RaiseAndSetIfChanged(ref _allFilterCounter, value);
    }

    public int ImportantFilterCounter
    {
        get => _importantFilterCounter;
        set => this.RaiseAndSetIfChanged(ref _importantFilterCounter, value);
    }
    
    public bool IsDarkTheme
    {
        get => _isDarkTheme;
        set => this.RaiseAndSetIfChanged(ref _isDarkTheme, value);
    }

    #region Not change collections
    public ObservableCollection<string> Colors { get; } = ["Default", "Red", "Orange", "Yellow", "Pink", "Purple", "Green", "Blue", "Emerald"];

    public ObservableCollection<string> Icons { get; } =
    [
        "CubesIcon", "EnvelopeIcon", "SmileIcon", "StarIcon", "MusicIcon", "BookIcon", "CardIcon", "DollarIcon",
        "PizzaIcon", "PillsIcon", "FolderIcon", "WeatherIcon", "CarIcon", "BusIcon", "BanIcon", "AppleIcon",
        "GhostIcon", "KeyIcon"
    ];

    public ObservableCollection<string> RepeatComboCollection { get; } =
    [
        "Don't repeat", "Every day", "Every 2 days", "Every 5 days", "Every week", "Every month", "Every year"
    ];
    public List<int?> RepeatComboValues { get; } = [null, 1, 2, 5, 7, 30, 365];

    public ObservableCollection<string> NotifyBeforeComboCollection { get; } =
    [
        "Don't notify", "1 hour before", "3 hours before", "1 day before", "2 days before", "5 days before", "week before"
    ];
    public List<int?> NotifyBeforeComboValues { get; } = [null, -1, -3, 1, 2, 5, 7];

    public ObservableCollection<string> ImportanceComboCollection { get; } =
    [
        "Not important", "Important"
    ];

    public List<bool> ImportanceComboValues { get; } = [false, true];
    #endregion

    #region Commands

    public void RemovingTask(TaskItem task)
    {
        AllFilterCounter--;
        if (task.IsImportant) ImportantFilterCounter--;
        if (task.CompleteDate.Date == DateTime.Now.Date) TodayFilterCounter--;
        if (task.CompleteDate > DateTime.Now) ScheduleFilterCounter--;
    }
    
    public void RemovingTasksMany(List<TaskItem> tasks)
    {
        foreach (var task in tasks)
        {
            RemovingTask(task);
        }
    }

    public void AddingTask(TaskItem task)
    {
        AllFilterCounter++;
        if (task.IsImportant) ImportantFilterCounter++;
        if (task.CompleteDate.Date == DateTime.Now.Date) TodayFilterCounter++;
        if (task.CompleteDate > DateTime.Now) ScheduleFilterCounter++;
    }

    public void ChangingTask(TaskItem oldTask, TaskItem newTask)
    {
        if (oldTask.IsImportant != newTask.IsImportant)
        {
            if (newTask.IsImportant) ImportantFilterCounter++;
            else ImportantFilterCounter--;
        }

        if (oldTask.CompleteDate != newTask.CompleteDate)
        {
            if (oldTask.CompleteDate < DateTime.Now) ScheduleFilterCounter++;
            if (oldTask.CompleteDate.Date == DateTime.Now.Date) TodayFilterCounter--;
            if (newTask.CompleteDate == DateTime.Now.Date) TodayFilterCounter++;
        }
    }

    public async Task LoadTasksByCategoryAsync()
    {
        var tasks = await _db.GetTasksByCategory(SelectedCategory!);
        TasksCollection.Clear();
        NodesCollection.Clear();
        foreach (var task in tasks)
        {
            TasksCollection.Add(task);
        }
    }

    public async Task LoadNodesForAllAsync()
    {
        var nodes = await _db.GetNodesForAll();
        TasksCollection.Clear();
        NodesCollection.Clear();
        foreach (var node in nodes)
        {
            NodesCollection.Add(node);
        }
    }

    public async Task LoadNodesForScheduledAsync()
    {
        var nodes = await _db.GetNodesForScheduled();
        TasksCollection.Clear();
        NodesCollection.Clear();
        foreach (var node in nodes)
        {
            NodesCollection.Add(node);
        }
    }

    public async Task LoadTasksForTodayAsync()
    {
        var tasks = await _db.GetTasksForToday();
        TasksCollection.Clear();
        NodesCollection.Clear();
        foreach (var task in tasks)
        {
            TasksCollection.Add(task);
        }
    }

    public async Task LoadTasksForImportantAsync()
    {
        var tasks = await _db.GetTasksForImportant();
        TasksCollection.Clear();
        NodesCollection.Clear();
        foreach (var task in tasks)
        {
            TasksCollection.Add(task);
        }
    }
    
    #endregion
}
