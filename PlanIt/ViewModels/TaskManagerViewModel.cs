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

public class TaskManagerViewModel : ViewModelBase
{
    #region Private attributes
    private readonly DbAccessService _db;
    private TaskItem _newTaskItem;
    private int _selectedRepeatIndex;
    private int _selectedNotifyIndex;
    private int _selectedImportanceIndex;
    #endregion
     
    #region Public attributes
    public OverlayService OverlayService { get; }
    public ViewRepository ViewRepository { get; }
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
    
    public TaskManagerViewModel(OverlayService overlayService,  DbAccessService db, ViewRepository repository)
    {
        OverlayService = overlayService;
        _db = db;
        ViewRepository = repository;
        
        ReturnToDefault();
    }
    
    private void ReturnToDefault()
    {
        NewTaskItem = new TaskItem { Title = "" };
        SelectedRepeatIndex = 0;
        SelectedNotifyIndex = 0;
        SelectedImportanceIndex = 0;
    }
    
    public ReactiveCommand<Unit, Unit> HideTaskOverlay => ReactiveCommand.Create(() =>
    {
        OverlayService.ToggleVisibility(1);
        ReturnToDefault();
    });

    private static DateTime CalculateOffsetToDateTime(int offset, DateTime dateTime) => 
        offset < 0 ? dateTime.AddHours(offset) : dateTime.AddDays(-1 * offset);

    private async Task<bool> CreateNewTask(TaskItem newTask, Notification? notification)
    {
        newTask.Notification = notification?.Id;
        ViewRepository.SelectedCategory!.TasksCount++;
        
        var insertTask = _db.InsertTask(newTask);
        var insertNotification = notification == null ? null : _db.InsertNotification(notification);
        var updateCategory = _db.UpdateCategory(ViewRepository.SelectedCategory);

        if (await insertTask && (insertNotification == null || await insertNotification) && await updateCategory)
        {
            ViewRepository.TasksCollection.Add(newTask);
            ViewRepository.AddingTask(newTask);
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

        var updateTask = _db.UpdateTask(newTask);
        bool? removeOldNotification = oldNotification == null ? null : await _db.RemoveNotification((ObjectId)oldNotification);
        var insertNotification = notification == null ? null : _db.InsertNotification(notification);

        if (await updateTask && (removeOldNotification == null || (bool)removeOldNotification!) && (insertNotification == null || await insertNotification))
        {
            var index = ViewRepository.TasksCollection.IndexOf(newTask);
            ViewRepository.TasksCollection[index] = newTask;
            ViewRepository.RemovingTask(newTask); //TODO Change
            ViewRepository.AddingTask(newTask); //TODO Change
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
        
        newTask.Category ??= ViewRepository.SelectedCategory!.Id;
        Notification? notification = null;
        var importance = ViewRepository.ImportanceComboValues[SelectedImportanceIndex];
        var repeat = ViewRepository.RepeatComboValues[SelectedRepeatIndex];
        var notify = ViewRepository.NotifyBeforeComboValues[SelectedNotifyIndex];
        
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

        if (OverlayService.EditMode) return await UpdateTask(newTask, notification);
        return await CreateNewTask(newTask, notification);
    });
}