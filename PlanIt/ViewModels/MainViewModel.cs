using PlanIt.Models;
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
        var db = new DataAccess("127.0.0.1", 27017, "planit");
        OverlayService = new OverlayService();
        WindowVM = new WindowViewModel(OverlayService, db);
        CategoryCreationVM = new CategoryCreationViewModel(OverlayService, db);
        TaskCreationVM = new TaskCreationViewModel(OverlayService, db);
        
    }
}