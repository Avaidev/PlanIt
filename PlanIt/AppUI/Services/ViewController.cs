using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Styling;
using Avalonia.Threading;
using DynamicData;
using MongoDB.Bson;
using PlanIt.Core.Services;
using PlanIt.Data.Models;
using PlanIt.Data.Services;
using ReactiveUI;

namespace PlanIt.UI.Services;

public class ViewController : ReactiveObject
{
    #region Initialization
    public ViewController(DataAccessService db)
    {
        _db = db;
        _categoriesCollection = [];
        _tasksCollection = [];
        _nodesCollection = [];
        _isDarkTheme = AppConfigManager.Settings.Theme.Equals("dark", StringComparison.InvariantCultureIgnoreCase);
    }

    public async Task InitializeAsync()
    {
        var categories = _db.Categories.GetAll();
        LoadingMessage = "Loading categories...";
        CategoriesCollection.Clear();
        foreach (var category in await categories)
        {
            CategoriesCollection.Add(category);
        }

        LoadingMessage = "Checking your tasks...";
        await CountCategoriesTasks();
        
        LoadingMessage = "Loading filters...";
        await SetTodayCounter();
        await SetScheduledCounter();
        await SetCompletedCounter();
        await SetAllCounter();
    }

    private async Task SetTodayCounter() => TodayFilterCounter = await _db.Tasks.CountTodayTasks();
    private async Task SetScheduledCounter() => ScheduleFilterCounter = await _db.Tasks.CountScheduledTasks();
    private async Task SetCompletedCounter() => CompletedFilterCounter = await _db.Tasks.CountCompletedTasks();
    private async Task SetAllCounter() => AllFilterCounter = await _db.Tasks.CountAll();
    private async Task CountCategoriesTasks()
    {
        foreach (var category in CategoriesCollection)
        {
            var count = await _db.Tasks.CountByCategory(category);
            category.TasksCount = count;
        }
    }
    #endregion
    
    #region Attributes
    private DataAccessService _db;
    private ObservableCollection<Category> _categoriesCollection;
    private ObservableCollection<TaskItem> _tasksCollection;
    private ObservableCollection<Node> _nodesCollection;
    private Category? _selectedCategory;
    private int _todayFilterCounter;
    private int _scheduleFilterCounter;
    private int _allFilterCounter;
    private int _completedFilterCounter;
    private bool _isDarkTheme;
    private bool _isCategoryOverlayVisible;
    private bool _isTaskOverlayVisible;
    private bool _isLoadingVisible = true;
    private string _loadingMessage = "";
    private string _searchParameter = "Results of your search:";
    private string _createWindowTitle = "";

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

    public bool IsLoadingVisible
    {
        get => _isLoadingVisible;
        set => this.RaiseAndSetIfChanged(ref _isLoadingVisible, value);
    }

    public string LoadingMessage
    {
        get => _loadingMessage;
        set => this.RaiseAndSetIfChanged(ref _loadingMessage, value);
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

    public int CompletedFilterCounter
    {
        get => _completedFilterCounter;
        set => this.RaiseAndSetIfChanged(ref _completedFilterCounter, value);
    }
    
    public string SearchParameter
    {
        get =>  _searchParameter;
        set => this.RaiseAndSetIfChanged(ref _searchParameter, $"Results for '{value}' search:");
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

    public string CreateWindowTitle
    {
        get => _createWindowTitle;
        set => this.RaiseAndSetIfChanged(ref _createWindowTitle, value);
    }

    public bool IsAnyOverlayVisible => IsCategoryOverlayVisible || IsTaskOverlayVisible;
    #endregion

    public void SortTasksIfFound(ObjectId? id=null)
    {
        if (id == null || TasksCollection.FirstOrDefault(t => t.Id == (ObjectId)id) != null)
        {
            Utils.OrderTasks(TasksCollection);
        }
    }

    public void SortNodesWhereFound(ObjectId id)
    {
        foreach (var node in NodesCollection)
        {
            if (node.Tasks.FirstOrDefault(t => t.Id == id) != null)
            {
                Utils.OrderTasks(node.Tasks);
                break;
            }
        }
    }
    
    public void CloseCategoryOverlay() => IsCategoryOverlayVisible = false;
    public void OpenCategoryOverlay() => IsCategoryOverlayVisible = true;
    public void CloseTaskOverlay() => IsTaskOverlayVisible = false;
    public void OpenTaskOverlay() => IsTaskOverlayVisible = true;

    public void ReloadView()
    {
        _ = SetTodayCounter();
        _ = SetCompletedCounter();
        _ = SetScheduledCounter();
        
        switch (ViewState)
        {
            case ViewStates.CATEGORY:
                _ = LoadTasksByCategoryAsync();
                break;
            case ViewStates.TODAY:
                _ = LoadTasksForTodayAsync();
                break;
            case ViewStates.COMPLETED:
                _ = LoadTasksForCompletedAsync();
                break;
            case ViewStates.SCHEDULED:
                _ = LoadNodesForScheduledAsync();
                break;
            case ViewStates.ALL:
                _ = LoadNodesForAllAsync();
                break;
            
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
    
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
                TasksCollection.Clear();
                break;
            
            case ViewStates.ALL:
            {
                var node = NodesCollection.FirstOrDefault(n => n.Category!.Equals(category));
                if (node != null) NodesCollection.Remove(node);
                break;
            }
            
            case ViewStates.COMPLETED:
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


    public void DecreaseFiltersNumbers(TaskItem task)
    {
        AllFilterCounter--;
        if (task.IsDone) CompletedFilterCounter--;
        if (Utils.CheckDateForToday(task.CompleteDate)) TodayFilterCounter--;
        if (Utils.CheckDateForScheduled(task.CompleteDate) && !task.IsDone) ScheduleFilterCounter--;
    }
    public void IncreaseFiltersNumbers(TaskItem task)
    {
        AllFilterCounter++;
        if (task.IsDone) CompletedFilterCounter++;
        if (Utils.CheckDateForToday(task.CompleteDate)) TodayFilterCounter++;
        if (Utils.CheckDateForScheduled(task.CompleteDate) && !task.IsDone) ScheduleFilterCounter++;
    }

    public void MarkTaskInView(TaskItem task)
    {
        _ = SetScheduledCounter();
        if (task.IsDone) CompletedFilterCounter++;
        else CompletedFilterCounter--;
        if (ViewState == ViewStates.COMPLETED && !task.IsDone) TasksCollection.Remove(task);
        else if (ViewState == ViewStates.SCHEDULED && task.IsDone)
        {
            foreach (var node in NodesCollection)
            {
                if (node.Tasks.Remove(task)) break;
            }
        }
        SortTasksIfFound(task.Id);
        SortNodesWhereFound(task.Id);
    }
    
    public void MarkTaskAsMissed(ObjectId taskId)
    {
        ScheduleFilterCounter--;
        switch (ViewState)
        {
            case ViewStates.COMPLETED:
                break;
            case ViewStates.CATEGORY:
            case ViewStates.TODAY:
            {
                var task = TasksCollection.FirstOrDefault(t => t.Id == taskId);
                task?.RaisePropertyChanged(nameof(task.IsMissed));
                break;
            }

            case ViewStates.SCHEDULED:
            case ViewStates.ALL:
                foreach (var node in NodesCollection)
                {
                    var task = node.Tasks.FirstOrDefault(t => t.Id == taskId);
                    if (task == null) continue;
                    task.RaisePropertyChanged(nameof(task.IsMissed));
                    Task.Delay(10).Wait();
                    if (ViewState == ViewStates.SCHEDULED) node.Tasks.Remove(task);
                }
                break;

            default:
                throw new ArgumentOutOfRangeException();
        }
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
            
            case ViewStates.COMPLETED:
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
            
            case  ViewStates.COMPLETED:
            {
                var index = TasksCollection.IndexOf(task);
                TasksCollection[index].ChangeObject(task);
                Utils.OrderTasks(TasksCollection);
                break;
            }

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
            case ViewStates.COMPLETED:
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
    
    public async Task LoadTasksByCategoryAsync()
    {
        var tasks = await _db.Tasks.GetTasksByCategory(SelectedCategory!);
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
        var nodes = await _db.Tasks.GetNodesForAllByCategories(CategoriesCollection);
        TasksCollection.Clear();
        NodesCollection.Clear();
        foreach (var node in nodes)
        {
            NodesCollection.Add(node);
        }
    }
    public async Task LoadNodesForScheduledAsync()
    {
        var nodes = await _db.Tasks.GetNodesForScheduledWithCategories(CategoriesCollection);
        TasksCollection.Clear();
        NodesCollection.Clear();
        foreach (var node in nodes)
        {
            NodesCollection.Add(node);
        }
    }
    public async Task LoadTasksForTodayAsync()
    {
        var tasks = await _db.Tasks.GetTasksForTodayWithCategories(CategoriesCollection);
        Utils.OrderTasks(tasks);
        TasksCollection.Clear();
        NodesCollection.Clear();
        foreach (var task in tasks)
        {
            TasksCollection.Add(task);
        }
    }
    public async Task LoadTasksForCompletedAsync()
    {
        var tasks = await _db.Tasks.GetTasksForCompletedWithCategories(CategoriesCollection);
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
        var tasks = await _db.Tasks.GetTasksBySearchWithCategories(search, CategoriesCollection);
        TasksCollection.Clear();
        NodesCollection.Clear();
        foreach (var task in tasks)
        {
            TasksCollection.Add(task);
        }
    }
    
    #region No change collections
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public enum ViewStates {TODAY, SCHEDULED, ALL, COMPLETED, CATEGORY, SEARCH}
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
