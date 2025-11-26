using System.Reactive;
using PlanIt.UI.Services;
using ReactiveUI;

namespace PlanIt.UI.ViewModels;

public class FilterCompletedViewModel : ViewModelBase
{
    #region Initialization

    public FilterCompletedViewModel(ViewController viewController, TaskManagerViewModel taskManagerVM)
    {
        TaskManagerVM = taskManagerVM;
        ViewController = viewController;
    }
    #endregion
    
    #region Attributes
    public ViewController ViewController { get; }
    public TaskManagerViewModel TaskManagerVM { get; }
    #endregion
    
}