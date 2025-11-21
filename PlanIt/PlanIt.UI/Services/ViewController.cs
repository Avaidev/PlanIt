using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using DynamicData;
using PlanIt.Core.Models;
using PlanIt.Core.Services;
using ReactiveUI;

namespace PlanIt.Services;

public class ViewController : ReactiveObject
{
    
    #region Initialization
    public ViewController(DbAccessService db)
    {
        _db = db;
        _ = InitializeAsync();
        _tasksCollection = [];
        _nodesCollection = [];
    }

    private async Task InitializeAsync()
    {
        CategoriesCollection = new ObservableCollection<Category>(await _db.GetAllCategories());
        await SetTodayCounter();
        await SetScheduledCounter();
        await SetImportantCounter();
        await SetAllCounter();
    }

    private async Task SetTodayCounter() => TodayFilterCounter = await _db.CountTodayTasks();
    private async Task SetScheduledCounter() => ScheduleFilterCounter = await _db.CountScheduledTasks();
    private async Task SetImportantCounter() => ImportantFilterCounter = await _db.CountImportantTasks();
    private async Task SetAllCounter() => AllFilterCounter = await _db.CountAllTasks();
    #endregion
    
    #region Attributes
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
    private bool _isCategoryOverlayVisible;
    private bool _isTaskOverlayVisible;

    public ViewStates ViewState { get; set; }
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
            if (value != null)
            {
                ViewState = ViewStates.CATEGORY;
                _ = LoadTasksByCategoryAsync();
            }
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

    public bool IsCategoryOverlayVisible
    {
        get => _isCategoryOverlayVisible;
        set => this.RaiseAndSetIfChanged(ref _isCategoryOverlayVisible, value);
    }

    public bool IsTaskOverlayVisible
    {
        get => _isTaskOverlayVisible;
        set => this.RaiseAndSetIfChanged(ref _isTaskOverlayVisible, value);
    }
    
    public bool IsAnyOverlayVisible => IsCategoryOverlayVisible || IsTaskOverlayVisible;
    #endregion
    
    
    public void CloseCategoryOverlay() => IsCategoryOverlayVisible = false;
    public void OpenCategoryOverlay() => IsCategoryOverlayVisible = true;
    public void CloseTaskOverlay() => IsTaskOverlayVisible = false;
    public void OpenTaskOverlay() => IsTaskOverlayVisible = true;
    
    
    public void AddCategoryToView(Category category)
    {
        CategoriesCollection.Add(category);
        if (ViewState == ViewStates.ALL)
        {
            NodesCollection.Add(new Node(category, []));
        }
    }
    public void ChangeCategoryInView(Category category)
    {
        var index = CategoriesCollection.IndexOf(category);
        CategoriesCollection[index].ChangeObject(category);
    }
    public void RemoveCategoryFromView(Category category)
    {
        CategoriesCollection.Remove(category);
        switch (ViewState)
        {
            case ViewStates.CATEGORY:
                if (SelectedCategory != null && category.Id == SelectedCategory.Id) SelectedCategory = null;
                break;
            
            case ViewStates.ALL:
            {
                var node = NodesCollection.FirstOrDefault(n => n.Category!.Equals(category));
                if (node != null) NodesCollection.Remove(node);
                break;
            }
            
            case ViewStates.IMPORTANT:
            case ViewStates.TODAY:
                TasksCollection.RemoveMany(TasksCollection.Where(t => t.Category == category.Id));
                break;
            
            case ViewStates.SCHEDULED:
                foreach (var node in NodesCollection)
                {
                    node.Tasks.RemoveMany(node.Tasks.Where(t => t.Category == category.Id));
                }

                break;
            
            default:
                throw new ArgumentOutOfRangeException();
        }
    }


    private void DecreaseFiltersNumbers(TaskItem task)
    {
        AllFilterCounter--;
        if (task.IsImportant) ImportantFilterCounter--;
        if (Utils.CheckDateForToday(task.CompleteDate)) TodayFilterCounter--;
        if (Utils.CheckDateForScheduled(task.CompleteDate)) ScheduleFilterCounter--;
    }
    private void IncreaseFiltersNumbers(TaskItem task)
    {
        AllFilterCounter++;
        if (task.IsImportant) ImportantFilterCounter++;
        if (Utils.CheckDateForToday(task.CompleteDate)) TodayFilterCounter++;
        if (Utils.CheckDateForScheduled(task.CompleteDate)) ScheduleFilterCounter++;
    }
    
    
    public void AddTaskToView(TaskItem task)
    {
        IncreaseFiltersNumbers(task);
        switch (ViewState)
        {
            case ViewStates.CATEGORY:
                task.CategoryObject = null;
                if (SelectedCategory != null && SelectedCategory.Id == task.Category) TasksCollection.Add(task);
                break;
            
            case ViewStates.TODAY:
                if (task.CompleteDate.Date == DateTime.Today)
                {
                    TasksCollection.Add(task);
                    Utils.OrderTasks(TasksCollection);
                }
                break;
            
            case ViewStates.IMPORTANT:
                if (task.IsImportant)
                {
                    TasksCollection.Add(task);
                    Utils.OrderTasks(TasksCollection);
                }
                break;
            
            case ViewStates.SCHEDULED:
                if (task.CompleteDate.Date == DateTime.Today)
                {
                    NodesCollection[0].Tasks.Add(task);
                    Utils.OrderTasks(NodesCollection[0].Tasks);
                }
                else if (task.CompleteDate.Date == DateTime.Today.AddDays(1))
                {
                    NodesCollection[1].Tasks.Add(task);
                    Utils.OrderTasks(NodesCollection[1].Tasks);
                }
                else
                {
                    NodesCollection[2].Tasks.Add(task);
                    Utils.OrderTasks(NodesCollection[2].Tasks);
                }
                break;
            
            case ViewStates.ALL:
                task.CategoryObject = null;
                foreach (var node in NodesCollection)
                {
                    if (node.Category!.Id == task.Category)
                    {
                        node.Tasks.Add(task);
                        Utils.OrderTasks(node.Tasks);
                        break;
                    }
                }
                break;
            
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
    public void ChangeTaskInView(TaskItem task)
    {
        _ = SetTodayCounter();
        _ = SetImportantCounter();
        _ = SetScheduledCounter();

        switch (ViewState)
        {
            case ViewStates.CATEGORY:
                if (SelectedCategory == null) break;
                if (SelectedCategory.Id != task.Category) TasksCollection.Remove(task);
                else
                {
                    var index = TasksCollection.IndexOf(task);
                    TasksCollection[index].ChangeObject(task);
                    Utils.OrderTasks(TasksCollection);
                }
                break;
            
            case  ViewStates.TODAY:
                if (!Utils.CheckDateForToday(task.CompleteDate)) TasksCollection.Remove(task);
                else
                {
                    var index = TasksCollection.IndexOf(task);
                    TasksCollection[index].ChangeObject(task);
                    Utils.OrderTasks(TasksCollection);
                }
                break;
            
            case  ViewStates.IMPORTANT:
                if (!task.IsImportant) TasksCollection.Remove(task);
                else
                {
                    var index = TasksCollection.IndexOf(task);
                    TasksCollection[index].ChangeObject(task);
                    Utils.OrderTasks(TasksCollection);
                }
                break;
            
            case ViewStates.SCHEDULED:
                if (!Utils.CheckDateForScheduled(task.CompleteDate)) TasksCollection.Remove(task);
                else
                {
                    foreach (var node in NodesCollection)
                    {
                        var index =  node.Tasks.IndexOf(task);
                        if (index != -1)
                        {
                            node.Tasks.Remove(task);
                            break;
                        }
                    }

                    if (Utils.CheckDateForToday(task.CompleteDate))
                    {
                        NodesCollection[0].Tasks.Add(task);
                        Utils.OrderTasks(NodesCollection[0].Tasks);
                    }
                    else if (Utils.CheckDateForToday(task.CompleteDate))
                    {
                        NodesCollection[1].Tasks.Add(task);
                        Utils.OrderTasks(NodesCollection[1].Tasks);
                    }
                    else
                    {
                        NodesCollection[2].Tasks.Add(task);
                        Utils.OrderTasks(NodesCollection[2].Tasks);
                    }
                }
                break;
            
            case ViewStates.ALL:
                foreach (var node in NodesCollection)
                {
                    var index = node.Tasks.IndexOf(task);
                    if (index != -1)
                    {
                        if (node.Category!.Id == task.Category)
                        {
                            node.Tasks[index].ChangeObject(task);
                            Utils.OrderTasks(node.Tasks);
                        }
                        else
                        {
                            node.Tasks.Remove(task);
                            var newNode = NodesCollection.FirstOrDefault(n => n.Category!.Id == task.Category);
                            if (newNode != null)
                            {
                                var newIndex = NodesCollection.IndexOf(newNode);
                                NodesCollection[newIndex].Tasks.Add(task);
                                Utils.OrderTasks(NodesCollection[newIndex].Tasks);
                            }
                            else
                            {
                                Console.WriteLine($"[ViewController > ChangeTaskInView] Error finding node with category of task '{task.Title}'");
                            }
                        }
                        break;
                    }
                }
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
    public void RemoveTaskFromView(TaskItem task)
    {
        DecreaseFiltersNumbers(task);
        switch (ViewState)
        {
            case ViewStates.CATEGORY:
            case ViewStates.TODAY:
            case ViewStates.IMPORTANT:
                TasksCollection.Remove(task);
                break;
            
            case ViewStates.SCHEDULED:
            case ViewStates.ALL:
                foreach (var node in NodesCollection)
                {
                    if (node.Tasks.Remove(task)) break;
                }
                break;
            
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public void AfterRemovingTasksMany(IEnumerable<TaskItem> tasks)
    {
        foreach (var task in tasks)
        {
            DecreaseFiltersNumbers(task);
        }
    }
    
    
    public async Task LoadTasksByCategoryAsync()
    {
        var tasks = await _db.GetTasksByCategory(SelectedCategory!);
        Utils.OrderTasks(tasks);
        TasksCollection.Clear();
        NodesCollection.Clear();
        foreach (var task in tasks)
        {
            TasksCollection.Add(task);
        }
    }
    public async Task LoadNodesForAllAsync()
    {
        var nodes = await _db.GetNodesForAllByCategories(CategoriesCollection);
        TasksCollection.Clear();
        NodesCollection.Clear();
        foreach (var node in nodes)
        {
            NodesCollection.Add(node);
        }
    }
    public async Task LoadNodesForScheduledAsync()
    {
        var nodes = await _db.GetNodesForScheduledWithCategories(CategoriesCollection);
        TasksCollection.Clear();
        NodesCollection.Clear();
        foreach (var node in nodes)
        {
            NodesCollection.Add(node);
        }
    }
    public async Task LoadTasksForTodayAsync()
    {
        var tasks = await _db.GetTasksForTodayWithCategories(CategoriesCollection);
        Utils.OrderTasks(tasks);
        TasksCollection.Clear();
        NodesCollection.Clear();
        foreach (var task in tasks)
        {
            TasksCollection.Add(task);
        }
    }
    public async Task LoadTasksForImportantAsync()
    {
        var tasks = await _db.GetTasksForImportantWithCategories(CategoriesCollection);
        Utils.OrderTasks(tasks);
        TasksCollection.Clear();
        NodesCollection.Clear();
        foreach (var task in tasks)
        {
            TasksCollection.Add(task);
        }
    }

    public async Task LoadTasksForSearchAsync(string search)
    {
        var tasks = await _db.GetTasksBySearchWithCategories(search, CategoriesCollection);
        TasksCollection.Clear();
        NodesCollection.Clear();
        foreach (var task in tasks)
        {
            TasksCollection.Add(task);
        }
    }
    
    #region No change collections
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public enum ViewStates {TODAY, SCHEDULED, ALL, IMPORTANT, CATEGORY, SEARCH}
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
}
