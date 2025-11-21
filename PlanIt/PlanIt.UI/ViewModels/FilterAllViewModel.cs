using System.Reactive;
using PlanIt.Services;
using ReactiveUI;

namespace PlanIt.UI.ViewModels;

public class FilterAllViewModel : ViewModelBase
{
    #region Initialization
    public FilterAllViewModel(ViewController viewController, TaskManagerViewModel taskManagerViewModel)
    {
        ViewController = viewController;
        TaskManagerVM = taskManagerViewModel;
    }

    #endregion

    #region Attributes
    public TaskManagerViewModel TaskManagerVM { get; }
    public ViewController ViewController { get; }
    #endregion
    
    public ReactiveCommand<Unit, Unit> AddNewTask => ReactiveCommand.Create(() =>
    {
        ViewController.OpenTaskOverlay();
    });
}