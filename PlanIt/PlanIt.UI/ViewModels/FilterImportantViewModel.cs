using System.Reactive;
using PlanIt.UI.Services;
using ReactiveUI;

namespace PlanIt.UI.ViewModels;

public class FilterImportantViewModel : ViewModelBase
{
    #region Initialization

    public FilterImportantViewModel(ViewController viewController, TaskManagerViewModel taskManagerVM)
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
        TaskManagerVM.SetStartParameters(isImportant: true);
    });
}