using System.Reactive;
using PlanIt.Services;
using ReactiveUI;

namespace PlanIt.UI.ViewModels;

public class WindowViewModel : ViewModelBase
{
    #region Initialization
    public WindowViewModel(ViewController controller, TaskManagerViewModel taskManagerViewModel)
    {
        ViewController = controller;
        TaskManagerVM = taskManagerViewModel;
    }
    #endregion
    
    #region Attributes
    public TaskManagerViewModel TaskManagerVM { get; }
    public ViewController ViewController { get;  }
    #endregion
    
    public ReactiveCommand<Unit, Unit> AddNewTask => ReactiveCommand.Create(() =>
    {
        ViewController.OpenTaskOverlay();
        TaskManagerVM.SetStartParameters(category: ViewController.SelectedCategory);
    });
}