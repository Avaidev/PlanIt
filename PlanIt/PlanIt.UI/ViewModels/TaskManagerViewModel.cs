using System;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PlanIt.Core.Services;
using PlanIt.Data.Models;
using PlanIt.UI.Services;
using ReactiveUI;

namespace PlanIt.UI.ViewModels;

public class TaskManagerViewModel : ViewModelBase
{
    #region Initialization
    public TaskManagerViewModel(DataAccessService db, ViewController controller, BackgroundController backgroundController, ILogger<TaskManagerViewModel> logger)
    {
        _logger = logger;
        _backgroundController = backgroundController;
        _db = db;
        ViewController = controller;
        ReturnToDefault();
    }
    
    private void ReturnToDefault()
    {
        NewTaskItem = new TaskItem { Title = "" };
        SelectedRepeatIndex = 0;
        SelectedNotifyIndex = 0;
        SelectedImportanceIndex = 0;
        SelectedCategoryIndex = 0;
        _editMode = false;
    }
    #endregion
    
    #region Attributes

    private readonly ILogger<TaskManagerViewModel> _logger;
    private readonly BackgroundController _backgroundController;
    private readonly DataAccessService _db;
    private TaskItem _newTaskItem;
    private int _selectedRepeatIndex;
    private int _selectedNotifyIndex;
    private int _selectedImportanceIndex;
    private int _selectedCategoryIndex;
    private bool _editMode;
    
    public ViewController ViewController { get; }
    public TaskItem NewTaskItem
    {
        get => _newTaskItem;
        set
        {
            this.RaiseAndSetIfChanged(ref _newTaskItem, value);
            this.RaisePropertyChanged(nameof(SelectedDatePart));
            this.RaisePropertyChanged(nameof(SelectedTimePart));
        }
    }

    public DateTimeOffset SelectedDatePart
    {
        get => new(NewTaskItem.CompleteDate.Date);
        set => NewTaskItem.CompleteDate = value.Date + NewTaskItem.CompleteDate.TimeOfDay;
    }

    public TimeSpan SelectedTimePart
    {
        get => NewTaskItem.CompleteDate.TimeOfDay;
        set => NewTaskItem.CompleteDate = NewTaskItem.CompleteDate.Date + value;
    }

    public int SelectedRepeatIndex
    {
        get => _selectedRepeatIndex;
        set => this.RaiseAndSetIfChanged(ref _selectedRepeatIndex, value);
    }

    public int SelectedNotifyIndex
    {
        get => _selectedNotifyIndex;
        set => this.RaiseAndSetIfChanged(ref _selectedNotifyIndex, value);
    }

    public int SelectedImportanceIndex
    {
        get => _selectedImportanceIndex;
        set => this.RaiseAndSetIfChanged(ref _selectedImportanceIndex, value);
    }

    public int SelectedCategoryIndex
    {
        get => _selectedCategoryIndex;
        set => this.RaiseAndSetIfChanged(ref _selectedCategoryIndex, value);
    }

    #endregion

    public void SetStartParameters(bool editMode = false, Category? category = null, bool? isImportant = null)
    {
        _editMode = editMode;
        if (category != null) SelectedCategoryIndex = ViewController.CategoriesCollection.IndexOf(category);
        if (isImportant != null) SelectedImportanceIndex = (bool)isImportant ? 1 : 0;
    }
    
    public ReactiveCommand<Unit, Unit> HideOverlay => ReactiveCommand.Create(() =>
    {
        ViewController.CloseTaskOverlay();
        ReturnToDefault();
    });

    public ReactiveCommand<TaskItem, Unit> MarkTask => ReactiveCommand.CreateFromTask<TaskItem, Unit>(async task =>
    {
        task.IsDone = !task.IsDone;
        await Task.Run(() => _db.Tasks.Update(task));
        await _backgroundController.SendData(task.Id.ToByteArray());
        await ViewController.SetScheduledCounter();
        ViewController.SortTasksIfFound(task.Id);
        ViewController.SortNodesWhereFound(task.Id);
        return Unit.Default;
    });
    
    public ReactiveCommand<TaskItem, bool> RemoveTask => ReactiveCommand.CreateFromTask<TaskItem, bool>( async task =>
    {
        if (!await MessageService.AskYesNoMessage($"Do you want to delete '{task.Title}' task?")) return false;
        var category = ViewController.CategoriesCollection.FirstOrDefault(c => c.Id == task.Category);
        if (category == null)
        {
            _logger.LogWarning("[TaskManager > RemoveTask] Error in finding category for task '{TaskTitle}'. Removing stopped!", task.Title);
            await MessageService.ErrorMessage($"Error in finding category for task '{task.Title}' Removing Stopped!");
            return false;
        }
        category.TasksCount--;
        var updateCategory = _db.Categories.Update(category);
        var taskRemove = _db.Tasks.Remove(task);

        if (await taskRemove && await updateCategory)
        {
            _logger.LogInformation("[TaskManager > RemoveTask] Task '{TaskTitle}' was removed", task.Title);
            await _backgroundController.SendData(task.Id.ToByteArray());
            ViewController.RemoveTaskFromView(task);
            return true;
        }
        _logger.LogError("[TaskManager > RemoveTask] Error: '{TaskTitle}' was not removed", task.Title);
        await MessageService.ErrorMessage($"Error: Task '{task.Title}' was not removed");
        return false;
    });

    public ReactiveCommand<TaskItem, bool> EditTask => ReactiveCommand.Create<TaskItem, bool>( task =>
    {
        var category = ViewController.CategoriesCollection.FirstOrDefault(c => c.Id == task.Category);
        if (category == null)
        {
            _logger.LogWarning($"[TaskManager > EditTask] Invalid in finding category for task");
            return false;
        }
        NewTaskItem = new TaskItem(task);
        SelectedCategoryIndex = ViewController.CategoriesCollection.IndexOf(category);
        SelectedImportanceIndex = task.IsImportant ? 1 : 0;
        SelectedRepeatIndex = ViewController.RepeatComboValues.IndexOf(task.Repeat);
        if (task.NotifyDate == null) SelectedNotifyIndex = 0;
        else
        {
            var difference = task.CompleteDate - (DateTime)task.NotifyDate;
            SelectedNotifyIndex = difference.Days == 0
                ? ViewController.NotifyBeforeComboValues.IndexOf(-difference.Hours)
                : ViewController.NotifyBeforeComboValues.IndexOf(difference.Days);
        }
        SetStartParameters(true);
        ViewController.OpenTaskOverlay();
        return true;
    });
    
    private static DateTime CalculateOffsetToDateTime(int offset, DateTime dateTime) => 
        offset < 0 ? dateTime.AddHours(offset) : dateTime.AddDays(-1 * offset);

    private async Task<bool> Create(TaskItem newTask, Category category)
    {
        category.TasksCount++;
        var insertTask = _db.Tasks.Insert(newTask);
        var updateCategory = _db.Categories.Update(category);

        if (await insertTask && await updateCategory)
        {
            newTask.CategoryObject = category;
            ViewController.AddTaskToView(newTask);
            HideOverlay.Execute().Subscribe();
            await _backgroundController.SendData([..newTask.Id.ToByteArray(), 1]);
            _logger.LogInformation("[TaskCreation] Task '{NewTaskTitle}' was created", newTask.Title);
            return true;
        }
        _logger.LogError("[TaskCreation] Error: Task '{NewTaskTitle}' wasn't created!", newTask.Title);
        await MessageService.ErrorMessage($"Error: Task '{newTask.Title}' wasn't created!");
        return false;
    }

    private async Task<bool> Update(TaskItem newTask, Category category)
    {
        var updateTask = _db.Tasks.Update(newTask);

        if (await updateTask)
        {
            newTask.CategoryObject = category;
            ViewController.ChangeTaskInView(newTask);
            HideOverlay.Execute().Subscribe();
            await _backgroundController.SendData([..newTask.Id.ToByteArray(), 0]);
            _logger.LogInformation("[TaskCreation] Task '{NewTaskTitle}' was updated", newTask.Title);
            return true;
        }
        _logger.LogError("[TaskCreation] Error: Task '{NewTaskTitle}' wasn't updated!", newTask.Title);
        return false;
    }
    
    public ReactiveCommand<TaskItem, bool> ApplyCreation => ReactiveCommand.CreateFromTask<TaskItem, bool>(async
        newTask =>
    {
        if (newTask.Title.Length == 0)
        {
            await MessageService.ErrorMessage("Enter task title!");
            return false;
        }

        if (newTask.CompleteDate < DateTime.Now)
        {
            await MessageService.ErrorMessage("The date & time of task must not be in past!");
            return false;
        }
        
        var importance = ViewController.ImportanceComboValues[SelectedImportanceIndex];
        var repeat = ViewController.RepeatComboValues[SelectedRepeatIndex];
        var notify = ViewController.NotifyBeforeComboValues[SelectedNotifyIndex];
        var newCategory = ViewController.CategoriesCollection[SelectedCategoryIndex];
        var oldCategory = newTask.Category == null ? null : ViewController.CategoriesCollection.FirstOrDefault(c => c.Id == newTask.Category);

        if (oldCategory != null && !newCategory.Equals(oldCategory))
        {
            oldCategory.TasksCount--;
            newCategory.TasksCount++;
            await _db.Categories.Update(oldCategory);
        }
        
        newTask.Category = newCategory.Id;
        newTask.IsImportant = importance;
        newTask.Repeat = repeat;
        newTask.IsDone = false;
        newTask.NotifyDate = null;
        if (notify != null)
        {
            var notificationDate = CalculateOffsetToDateTime((int)notify, newTask.CompleteDate);
            if (notificationDate < DateTime.Now)
            {
                await MessageService.ErrorMessage("The date of notification must not be in past!");
                return false;
            }
            newTask.NotifyDate = notificationDate;
        }

        if (_editMode) return await Update(newTask, newCategory);
        return await Create(newTask, newCategory);
    });
}