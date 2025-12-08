using System.Reactive;
using PlanIt.UI.Services;
using ReactiveUI;

namespace PlanIt.UI.ViewModels;

public class FilterScheduledViewModel : ViewModelBase
{
    #region Initialization

    public FilterScheduledViewModel(ViewController viewController, TaskManagerViewModel taskManagerVM)
    {
        TaskManagerVM = taskManagerVM;
        ViewController = viewController;
    }
    #endregion
        
    #region Attributes
    public ViewController ViewController { get; }
    public TaskManagerViewModel TaskManagerVM { get; }
    #endregion
        
    public ReactiveCommand<Unit, Unit> AddNewTask => ReactiveCommand.Create(() =>
    {
        ViewController.CreateWindowTitle = "New Task";
        ViewController.OpenTaskOverlay();
    });
}