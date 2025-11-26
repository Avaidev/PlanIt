using PlanIt.UI.Services;
using ReactiveUI;

namespace PlanIt.UI.ViewModels;

public class SearchViewModel : ViewModelBase
{
    #region Initialization

    public SearchViewModel(ViewController viewController, TaskManagerViewModel taskManagerViewModel)
    {
        ViewController = viewController;
        TaskManagerVM = taskManagerViewModel;
    }
    #endregion
    
    #region Attributes
    public ViewController ViewController { get; }
    public TaskManagerViewModel TaskManagerVM { get; }
    #endregion
}