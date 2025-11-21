using System;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;
using MongoDB.Bson;
using PlanIt.Core.Models;
using PlanIt.Core.Services;
using PlanIt.Services;
using ReactiveUI;
using Notification = PlanIt.Core.Models.Notification;

namespace PlanIt.UI.ViewModels;

public class TaskManagerViewModel : ViewModelBase
{
    #region Initialization
    public TaskManagerViewModel(DbAccessService db, ViewController controller)
    {
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
    private readonly DbAccessService _db;
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

    public ReactiveCommand<TaskItem, bool> RemoveTask => ReactiveCommand.CreateFromTask<TaskItem, bool>( async task =>
    {
        if (!await MessageService.AskYesNoMessage($"Do you want to delete '{task.Title}' task?")) return false;
        var category = ViewController.CategoriesCollection.FirstOrDefault(c => c.Id == task.Category);
        if (category == null)
        {
            Console.WriteLine($"[TaskManager > RemoveTask] Error in finding category for task '{task.Title}'. Removing stopped!");
            await MessageService.ErrorMessage($"Error in finding category for task '{task.Title}' Removing Stopped!");
            return false;
        }
        category.TasksCount--;
        var updateCategory = _db.UpdateCategory(category);
        var notificationRemove = task.Notification == null ? null : _db.RemoveNotification((ObjectId)task.Notification);
        var taskRemove = _db.RemoveTask(task);

        if ((notificationRemove == null || await notificationRemove) && await taskRemove && await updateCategory)
        {
            Console.WriteLine($"[TaskManager > RemoveTask] Task '{task.Title}' was removed");
            ViewController.RemoveTaskFromView(task);
            return true;
        }
        Console.WriteLine($"[TaskManager > RemoveTask] Error: '{task.Title}' was not removed");
        await MessageService.ErrorMessage($"Error: Task '{task.Title}' was not removed");
        return false;
    });

    public ReactiveCommand<TaskItem, bool> EditTask => ReactiveCommand.CreateFromTask<TaskItem, bool>(async task =>
    {
        var category = ViewController.CategoriesCollection.FirstOrDefault(c => c.Id == task.Category);
        if (category == null)
        {
            Console.WriteLine($"[TaskManager > EditTask] Invalid in finding category for task");
            return false;
        }
        NewTaskItem = new TaskItem(task);
        SelectedCategoryIndex = ViewController.CategoriesCollection.IndexOf(category);
        SelectedImportanceIndex = task.IsImportant ? 1 : 0;
        SelectedRepeatIndex = ViewController.RepeatComboValues.IndexOf(task.Repeat);
        if (task.Notification == null) SelectedNotifyIndex = 0;
        else
        {
            var notification = await _db.GetNotification((ObjectId)task.Notification);
            if (notification == null)
            {
                SelectedNotifyIndex = 0;
                NewTaskItem.Notification = null;
                Console.WriteLine("[TaskManager > EditTask : TaskItem] Invalid in finding notification that is not null");
            }
            else
            {
                var difference = task.CompleteDate - notification.Notify;
                SelectedNotifyIndex = difference.Days == 0
                    ? ViewController.NotifyBeforeComboValues.IndexOf(-difference.Hours)
                    : ViewController.NotifyBeforeComboValues.IndexOf(difference.Days);
            }
        }
        SetStartParameters(true);
        ViewController.OpenTaskOverlay();
        return true;
    });
    
    private static DateTime CalculateOffsetToDateTime(int offset, DateTime dateTime) => 
        offset < 0 ? dateTime.AddHours(offset) : dateTime.AddDays(-1 * offset);

    private async Task<bool> Create(TaskItem newTask, Notification? notification, Category category)
    {
        category.TasksCount++;
        newTask.Notification = notification?.Id;
        
        var insertTask = _db.InsertTask(newTask);
        var insertNotification = notification == null ? null : _db.InsertNotification(notification);
        var updateCategory = _db.UpdateCategory(category);

        if (await insertTask && (insertNotification == null || await insertNotification) && await updateCategory)
        {
            newTask.CategoryObject = category;
            ViewController.AddTaskToView(newTask);
            HideOverlay.Execute().Subscribe();
            Console.WriteLine($"[TaskCreation] Task '{newTask.Title}' was created");
            return true;
        }
        Console.WriteLine("[TaskCreation] Error: Task wasn't created!");
        await MessageService.ErrorMessage($"Error: Task '{newTask.Title}' wasn't created!");
        return false;
    }

    private async Task<bool> Update(TaskItem newTask, Notification? notification, Category category)
    {
        var oldNotification = newTask.Notification;
        newTask.Notification = notification?.Id;

        var updateTask = _db.UpdateTask(newTask);
        bool? removeOldNotification = oldNotification == null ? null : await _db.RemoveNotification((ObjectId)oldNotification);
        var insertNotification = notification == null ? null : _db.InsertNotification(notification);

        if (await updateTask && (removeOldNotification == null || (bool)removeOldNotification) && (insertNotification == null || await insertNotification))
        {
            newTask.CategoryObject = category;
            ViewController.ChangeTaskInView(newTask);
            HideOverlay.Execute().Subscribe();
            Console.WriteLine($"[TaskCreation] Task '{newTask.Title}' was updated");
            return true;
        }
        Console.WriteLine("[TaskCreation] Error: Task wasn't updated!");
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
        
        Notification? notification = null;
        var importance = ViewController.ImportanceComboValues[SelectedImportanceIndex];
        var repeat = ViewController.RepeatComboValues[SelectedRepeatIndex];
        var notify = ViewController.NotifyBeforeComboValues[SelectedNotifyIndex];
        var newCategory = ViewController.CategoriesCollection[SelectedCategoryIndex];
        var oldCategory = newTask.Category == null ? null : ViewController.CategoriesCollection.FirstOrDefault(c => c.Id == newTask.Category);

        if (oldCategory != null && !newCategory.Equals(oldCategory))
        {
            oldCategory.TasksCount--;
            newCategory.TasksCount++;
            await _db.UpdateCategory(oldCategory);
        }
        
        newTask.Category = newCategory.Id;
        newTask.IsImportant = importance;
        newTask.Repeat = repeat;
        newTask.IsDone = false;
        if (notify != null)
        {
            var notificationDate = CalculateOffsetToDateTime((int)notify, newTask.CompleteDate);
            if (notificationDate < DateTime.Now)
            {
                await MessageService.ErrorMessage("The date of notification must not be in past!");
                return false;
            }
            notification = new Notification
                { Title = newTask.Title, Message = newTask.Description, Repeat = repeat, Notify = notificationDate };
        }

        if (_editMode) return await Update(newTask, notification, newCategory);
        return await Create(newTask, notification, newCategory);
    });
}