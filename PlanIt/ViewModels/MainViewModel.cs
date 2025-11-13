using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using PlanIt.Models;
using PlanIt.Services;
using PlanIt.Services.DataServices;
using ReactiveUI;

namespace PlanIt.ViewModels;

public class MainViewModel : ViewModelBase
{
    public CategoryCreationViewModel CategoryCreationVM { get; }
    public TaskCreationViewModel TaskCreationVM { get; }
    public WindowViewModel WindowVM { get; }

    public OverlayService OverlayService { get; }
    public DbAccessService Db { get; }
    public ViewRepository ViewRepository { get; }
    
    public MainViewModel()
    {
        Db = new DbAccessService();
        OverlayService = new OverlayService();
        ViewRepository = new ViewRepository();
        WindowVM = new WindowViewModel(OverlayService, Db, ViewRepository);
        CategoryCreationVM = new CategoryCreationViewModel(OverlayService, Db, ViewRepository);
        TaskCreationVM = new TaskCreationViewModel(OverlayService, Db, ViewRepository);

        OverlayService.WhenAnyValue(x => x.ToEditObject!)
            .OfType<TaskItem>()
            .Subscribe(async taskItem =>
            {
                TaskCreationVM.NewTaskItem = new TaskItem(taskItem);
                TaskCreationVM.SelectedImportanceIndex = taskItem.IsImportant ? 1 : 0;
                TaskCreationVM.SelectedRepeatIndex = ViewRepository.RepeatComboValues.IndexOf(taskItem.Repeat);
                if (taskItem.Notification == null) TaskCreationVM.SelectedNotifyIndex = 0;
                else
                {
                    var notification = await Db.GetNotification((ObjectId)taskItem.Notification);
                    if (notification == null)
                    {
                        TaskCreationVM.SelectedNotifyIndex = 0;
                        TaskCreationVM.NewTaskItem.Notification = null;
                        Console.WriteLine("[ToEditObject : TaskItem] Invalid find notification that is not null");
                    }
                    else
                    {
                        var difference = taskItem.CompleteDate - notification.Notify;
                        TaskCreationVM.SelectedNotifyIndex = difference.Days == 0 
                            ? ViewRepository.NotifyBeforeComboValues.IndexOf(-difference.Hours) 
                            : ViewRepository.NotifyBeforeComboValues.IndexOf(difference.Days);
                    }
                }
            });
        
        OverlayService.WhenAnyValue(x => x.ToEditObject!)
            .OfType<Category>()
            .Subscribe(category => CategoryCreationVM.NewCategory = new Category(category));
            
    }
}