using PlanIt.Services;
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

    private string _panelSearchText = "Results for .. search:";

    public string PanelSearchText
    {
        get =>  _panelSearchText;
        set => this.RaiseAndSetIfChanged(ref _panelSearchText, $"Results for '{value}' search:");
    }
    public ViewController ViewController { get; }
    public TaskManagerViewModel TaskManagerVM { get; }
    #endregion
}