using System.Reactive;
using PlanIt.Models;
using PlanIt.Services;
using PlanIt.Services.DataServices;
using ReactiveUI;

namespace PlanIt.ViewModels;

public class FilterAllViewModel : ViewModelBase
{
    #region Private attributes
    private DbAccessService _db { get; }
    private TaskManagerViewModel TaskManagerVm { get; }
    #endregion
    
    #region Public attributes
    public ViewController ViewController { get; }
    #endregion
    
    public FilterAllViewModel(ViewController viewController, DbAccessService db, TaskManagerViewModel taskManagerViewModel)
    {
        ViewController = viewController;
        _db = db;
        TaskManagerVm = taskManagerViewModel;
    }

}