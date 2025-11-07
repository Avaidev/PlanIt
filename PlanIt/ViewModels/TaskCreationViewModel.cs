using PlanIt.Services;

namespace PlanIt.ViewModels;

public class TaskCreationViewModel : ViewModelBase
{
    private readonly OverlayService _overlayService;

    public TaskCreationViewModel(OverlayService overlayService)
    {
        _overlayService = overlayService;
    }
}