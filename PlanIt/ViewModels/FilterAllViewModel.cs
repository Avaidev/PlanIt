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
    public ViewRepository ViewRepository { get; }
    public OverlayService OverlayService { get; }
    #endregion
    
    public FilterAllViewModel(ViewRepository viewRepository, DbAccessService db, OverlayService overlayService, TaskManagerViewModel taskManagerViewModel)
    {
        ViewRepository = viewRepository;
        _db = db;
        OverlayService = overlayService;
        TaskManagerVm = taskManagerViewModel;
    }

}