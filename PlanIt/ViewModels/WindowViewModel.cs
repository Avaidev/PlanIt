using PlanIt.Services;

namespace PlanIt.ViewModels;

public class WindowViewModel : ViewModelBase
{
    private readonly OverlayService _overlayService;

    public WindowViewModel(OverlayService overlayService)
    {
        _overlayService = overlayService;
    }
}