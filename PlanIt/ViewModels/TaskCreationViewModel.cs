using PlanIt.Models;
using PlanIt.Services;

namespace PlanIt.ViewModels;

public class TaskCreationViewModel : ViewModelBase
{
    private readonly OverlayService _overlayService;
    private readonly DataAccess _db;

    public TaskCreationViewModel(OverlayService overlayService,  DataAccess db)
    {
        _overlayService = overlayService;
        _db = db;
    }
}