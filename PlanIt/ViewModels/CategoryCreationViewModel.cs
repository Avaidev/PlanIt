using PlanIt.Services;

namespace PlanIt.ViewModels;

public class CategoryCreationViewModel : ViewModelBase
{
    private readonly OverlayService _overlayService;
    
    public  CategoryCreationViewModel(OverlayService overlayService)
    {
        _overlayService = overlayService;
    }
}