using PlanIt.Data.Services;

namespace PlanIt.Core.Services;

public class DataAccessService
{
    public TasksRepository Tasks = new TasksRepository();
    public CategoriesRepository Categories = new CategoriesRepository();
}