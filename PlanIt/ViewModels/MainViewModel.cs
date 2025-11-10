using PlanIt.Models;
using PlanIt.Services;
using PlanIt.Services.DataServices;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

namespace PlanIt.ViewModels;

public class MainViewModel : ViewModelBase
{
    public CategoryCreationViewModel CategoryCreationVM { get; }
    public TaskCreationViewModel TaskCreationVM { get; }
    public WindowViewModel WindowVM { get; }
    
    public OverlayService OverlayService { get; }
    public DbAccessService Db { get; }
    
    public MainViewModel()
    {
        Db = new DbAccessService();
        OverlayService = new OverlayService();
        WindowVM = new WindowViewModel(OverlayService, Db);
        CategoryCreationVM = new CategoryCreationViewModel(OverlayService, Db);
        TaskCreationVM = new TaskCreationViewModel(OverlayService, Db);
    }
}