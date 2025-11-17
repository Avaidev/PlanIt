using System;
using PlanIt.Services;
using PlanIt.Models;
using ReactiveUI;
using System.Reactive;
using System.Reactive.Linq;
using MongoDB.Bson;
using PlanIt.Services.DataServices;

namespace PlanIt.ViewModels;

public class WindowViewModel : ViewModelBase
{
    #region Private attributes
    private DbAccessService _db { get;  }
    private TaskManagerViewModel TaskManagerVm { get; }
    #endregion
    
    #region Public attributes
    public OverlayService OverlayService { get; }
    public ViewRepository ViewRepository { get;  }
    #endregion
    
    public WindowViewModel(OverlayService overlayService, DbAccessService db, ViewRepository repository, TaskManagerViewModel taskManagerViewModel)
    {
        OverlayService = overlayService;
        _db = db;
        ViewRepository = repository;
        TaskManagerVm = taskManagerViewModel;
        
        // OverlayService.WhenAnyValue(x => x.ToEditObject!)
        //     .OfType<TaskItem>()
        //     .Subscribe(async taskItem =>
        //     {
        //         TaskManagerVm.NewTaskItem = new TaskItem(taskItem);
        //         TaskManagerVm.SelectedImportanceIndex = taskItem.IsImportant ? 1 : 0;
        //         TaskManagerVm.SelectedRepeatIndex = ViewRepository.RepeatComboValues.IndexOf(taskItem.Repeat);
        //         if (taskItem.Notification == null) TaskManagerVm.SelectedNotifyIndex = 0;
        //         else
        //         {
        //             var notification = await _db.GetNotification((ObjectId)taskItem.Notification);
        //             if (notification == null)
        //             {
        //                 TaskManagerVm.SelectedNotifyIndex = 0;
        //                 TaskManagerVm.NewTaskItem.Notification = null;
        //                 Console.WriteLine("[ToEditObject : TaskItem] Invalid in finding notification that is not null");
        //             }
        //             else
        //             {
        //                 var difference = taskItem.CompleteDate - notification.Notify;
        //                 TaskManagerVm.SelectedNotifyIndex = difference.Days == 0 
        //                     ? ViewRepository.NotifyBeforeComboValues.IndexOf(-difference.Hours) 
        //                     : ViewRepository.NotifyBeforeComboValues.IndexOf(difference.Days);
        //             }
        //         }
        //     });
    }
    
    #region Commands
    public ReactiveCommand<Unit, Unit> ShowTaskOverlay => ReactiveCommand.Create(() =>
    {
        OverlayService.ToggleVisibility(1);
    });
    
    public ReactiveCommand<TaskItem, bool> RemoveTask => ReactiveCommand.CreateFromTask<TaskItem, bool>( async task =>
    {
        if (!await MessageService.AskYesNoMessage($"Do you want to delete '{task.Title}' task?")) return false;
        var notificationRemove = task.Notification == null ? null : _db.RemoveNotification((ObjectId)task.Notification);
        var taskRemove = _db.RemoveTask(task);
        ViewRepository.SelectedCategory!.TasksCount--;
        var updateCategory = _db.UpdateCategory(ViewRepository.SelectedCategory!);

        if ((notificationRemove == null || await notificationRemove) && await taskRemove && await updateCategory)
        {
            Console.WriteLine($"[WindowVM > RemoveTask] Task '{task.Title}' was removed");
            ViewRepository.RemovingTask(task);
            ViewRepository.TasksCollection.Remove(task);
            return true;
        }
        Console.WriteLine($"[WindowVM > RemoveTask] Error: '{task.Title}' was not removed");
        return false;
    });

    public ReactiveCommand<TaskItem, Unit> EditTask => ReactiveCommand.Create<TaskItem, Unit>(task =>
    {
        OverlayService.ToggleVisibility(1, true);
        return Unit.Default;
    });

    #endregion
}