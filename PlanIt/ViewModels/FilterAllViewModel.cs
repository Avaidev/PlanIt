using System.Reactive;
using PlanIt.Models;
using PlanIt.Services;
using PlanIt.Services.DataServices;
using ReactiveUI;

namespace PlanIt.ViewModels;

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