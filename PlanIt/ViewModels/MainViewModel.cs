using PlanIt.Services;

namespace PlanIt.ViewModels;

public class MainViewModel : ViewModelBase
{
    public CategoryCreationViewModel CategoryCreationVM { get; }
    public TaskCreationViewModel TaskCreationVM { get; }
    public WindowViewModel WindowVM { get; }
    
    public OverlayService OverlayService { get; }
    
    public MainViewModel()
    {
        OverlayService = new OverlayService();
        WindowVM = new WindowViewModel(OverlayService);
        CategoryCreationVM = new CategoryCreationViewModel(OverlayService);
        TaskCreationVM = new TaskCreationViewModel(OverlayService);
        
    }
}