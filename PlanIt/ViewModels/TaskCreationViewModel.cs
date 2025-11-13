using System;
using System.Reactive;
using System.Threading.Tasks;
using MongoDB.Bson;
using PlanIt.Models;
using PlanIt.Services;
using PlanIt.Services.DataServices;
using ReactiveUI;
using Notification = PlanIt.Models.Notification;

namespace PlanIt.ViewModels;

public class TaskCreationViewModel : ViewModelBase
{
    private readonly OverlayService _overlayService;
    private readonly DbAccessService _db;
    private readonly ViewRepository _viewRepository;
    

    public TaskCreationViewModel(OverlayService overlayService,  DbAccessService db, ViewRepository repository)
    {
        _overlayService = overlayService;
        _db = db;
        _viewRepository = repository;
        
        ReturnToDefault();
    }

    #region Private attributes
    private TaskItem _newTaskItem;
    private int _selectedRepeatIndex;
    private int _selectedNotifyIndex;
    private int _selectedImportanceIndex;
    #endregion
    
    #region Public attributes
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

    #endregion
    
    #region Commands

    private void ReturnToDefault()
    {
        NewTaskItem = new TaskItem { Title = "" };
        SelectedRepeatIndex = 0;
        SelectedNotifyIndex = 0;
        SelectedImportanceIndex = 0;
    }
    
    public ReactiveCommand<Unit, Unit> HideTaskOverlay => ReactiveCommand.Create(() =>
    {
        _overlayService.ToggleVisibility(1);
        ReturnToDefault();
    });

    private static DateTime CalculateOffsetToDateTime(int offset, DateTime dateTime) => 
        offset < 0 ? dateTime.AddHours(offset) : dateTime.AddDays(-1 * offset);

    private async Task<bool> CreateNewTask(TaskItem newTask, Notification? notification)
    {
        newTask.Notification = notification?.Id;
        _viewRepository.SelectedCategory!.TasksCount++;
        
        var insertTask = _db.InsertTask(newTask);
        var insertNotification = notification == null ? null : _db.InsertNotification(notification);
        var updateCategory = _db.UpdateCategory(_viewRepository.SelectedCategory);

        if (await insertTask && (insertNotification == null || await insertNotification) && await updateCategory)
        {
            _viewRepository.TasksCollection.Add(newTask);
            HideTaskOverlay.Execute().Subscribe();
            Console.WriteLine($"[TaskCreation] Task '{newTask.Title}' was created");
            return true;
        }
        Console.WriteLine("[TaskCreation] Error: Task wasn't created!");
        return false;
    }

    private async Task<bool> UpdateTask(TaskItem newTask, Notification? notification)
    {
        var oldNotification = newTask.Notification;
        newTask.Notification = notification?.Id;
        Console.WriteLine("TOREMOVE: " + oldNotification);
        Console.WriteLine("NEW: " + newTask.Notification);

        var updateTask = _db.UpdateTask(newTask);
        bool? removeOldNotification = oldNotification == null ? null : await _db.RemoveNotification((ObjectId)oldNotification);
        var insertNotification = notification == null ? null : _db.InsertNotification(notification);

        if (await updateTask && (removeOldNotification == null || (bool)removeOldNotification!) && (insertNotification == null || await insertNotification))
        {
            var index = _viewRepository.TasksCollection.IndexOf(newTask);
            _viewRepository.TasksCollection[index] = newTask;
            HideTaskOverlay.Execute().Subscribe();
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
        
        newTask.Category ??= _viewRepository.SelectedCategory!.Id;
        Notification? notification = null;
        var importance = _viewRepository.ImportanceComboValues[SelectedImportanceIndex];
        var repeat = _viewRepository.RepeatComboValues[SelectedRepeatIndex];
        var notify = _viewRepository.NotifyBeforeComboValues[SelectedNotifyIndex];
        
        newTask.IsImportant = importance;
        newTask.Repeat = repeat;
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

        if (_overlayService.ToEditObject != null) return await UpdateTask(newTask, notification);
        return await CreateNewTask(newTask, notification);
    });

    #endregion
}