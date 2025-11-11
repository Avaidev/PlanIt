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
    public ViewRepository ViewRepository { get; }
    
    public MainViewModel()
    {
        Db = new DbAccessService();
        OverlayService = new OverlayService();
        ViewRepository = new ViewRepository();
        WindowVM = new WindowViewModel(OverlayService, Db, ViewRepository);
        CategoryCreationVM = new CategoryCreationViewModel(OverlayService, Db, ViewRepository);
        TaskCreationVM = new TaskCreationViewModel(OverlayService, Db, ViewRepository);
    }
}