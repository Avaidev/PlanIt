using System.Reactive;
using PlanIt.Services;
using ReactiveUI;

namespace PlanIt.UI.ViewModels;

public class FilterTodayViewModel : ViewModelBase
{
    #region Initialization

    public FilterTodayViewModel(ViewController viewController, TaskManagerViewModel taskManagerVM)
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
        ViewController.OpenTaskOverlay();
    });
}