using System;
using System.Reactive;
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
        set => this.RaiseAndSetIfChanged(ref _newTaskItem, value);
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

        newTask.Notification = notification?.Id;
        _viewRepository.SelectedCategory!.TasksCount++;
        
        var insertTask = _db.InsertTask(newTask);
        var insertNotification = notification != null ? _db.InsertNotification(notification) : null;
        var updateCategory = _db.UpdateCategory(_viewRepository.SelectedCategory);

        if (await insertTask && (insertNotification == null || await insertNotification) && await updateCategory)
        {
            Console.WriteLine($"[TaskCreation] Task '{newTask.Title}' was created");
            _viewRepository.TasksCollection.Add(newTask);
            HideTaskOverlay.Execute().Subscribe();
            return true;
        }
        Console.WriteLine("[TaskCreation] Error: Task wasn't created!");
        return true;
    });

    #endregion
}