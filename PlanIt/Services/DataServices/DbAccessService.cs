using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using PlanIt.Models;
using TaskItem = PlanIt.Models.Task;

namespace PlanIt.Services.DataServices;

public class DbAccessService
{
    private Repository<TaskItem> _taskRepo;
    private Repository<Category> _categoryRepo;

    public DbAccessService()
    {
        const string baseDirectory = "Data";
        _taskRepo = new Repository<TaskItem>(Path.Combine(baseDirectory, "tasks.bson"));
        _categoryRepo = new Repository<Category>(Path.Combine(baseDirectory, "categories.bson"));
    }

    public async Task<List<Category>> GetAllCategories()
    {
        return await _categoryRepo.GetAllAsync(true);
    }

    public async Task<bool> InsertCategory(Category category)
    {
        return await _categoryRepo.AddAsync(category);
    }

    public async Task<bool> RemoveCategory(Category category)
    {
        return await _categoryRepo.DeleteAsync(category);
    }

    public async Task<int> CountCategories()
    {
        return await _categoryRepo.CountAsync();
    }

    public async Task<bool> RemoveTask(TaskItem task)
    {
        return await _taskRepo.DeleteAsync(task);
    }

    public async Task<bool> RemoveTasksMany(List<TaskItem> tasks)
    {
        bool allDeleted = true;
        foreach (var task in tasks)
        {
            allDeleted = await RemoveTask(task);
        }
        return allDeleted;
    }

    public async Task<List<TaskItem>> GetTasksByCategory(Category category)
    {
        return await _taskRepo.FindManyAsync(task => task.Category == category.Id);
    }
}