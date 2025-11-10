using PlanIt.Models;
using PlanIt.Services;
using PlanIt.Services.DataServices;

namespace PlanIt.ViewModels;

public class TaskCreationViewModel : ViewModelBase
{
    private readonly OverlayService _overlayService;
    private readonly DbAccessService _db;

    public TaskCreationViewModel(OverlayService overlayService,  DbAccessService db)
    {
        _overlayService = overlayService;
        _db = db;
    }
}