using MongoDB.Bson;
using PlanIt.Data.Services;
using PlanIt.Core.Models;

namespace PlanIt.Core.Services;

public class DbAccessService
{
    #region Initialization
    public DbAccessService()
    {
        _baseDirectory = Utils.GetDataDirectory();
        _taskRepo = new ObjectRepository<TaskItem>(Utils.GetFilePath("tasks.bson"));
        _categoryRepo = new ObjectRepository<Category>(Utils.GetFilePath("categories.bson"));
        _notificationRepo = new ObjectRepository<Notification>(Utils.GetFilePath("notifications.bson"));
    }
    #endregion

    #region Attributes
    private string _baseDirectory { get; set; }
    private ObjectRepository<TaskItem> _taskRepo;
    private ObjectRepository<Category> _categoryRepo;
    private ObjectRepository<Notification> _notificationRepo;
    #endregion

    

    #region Categories
    public async Task<Category?> GetCategoryById(ObjectId id)
    {
        return await _categoryRepo.FindAsync(c => c.Id == id);
    }
    
    public async Task<List<Category>> GetAllCategories()
    {
        return await _categoryRepo.GetAllAsync("all");
    }

    public async Task<bool> InsertCategory(Category category)
    {
        return await _categoryRepo.AddAsync(category);
    }

    public async Task<bool> RemoveCategory(Category category)
    {
        return await _categoryRepo.DeleteAsync(category);
    }

    public async Task<bool> UpdateCategory(Category category)
    {
        return await _categoryRepo.UpdateAsync(category);
    }

    public async Task<int> CountCategories()
    {
        return await _categoryRepo.CountAsync();
    }
    #endregion

    #region Tasks
    public async Task<bool> InsertTask(TaskItem task)
    {
        return await _taskRepo.AddAsync(task);
    }

    public async Task<bool> RemoveTask(TaskItem task)
    {
        return await _taskRepo.DeleteAsync(task);
    }

    public async Task<bool> UpdateTask(TaskItem task)
    {
        return await _taskRepo.UpdateAsync(task);
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
        return (await _taskRepo.FindManyAsync(task => task.Category == category.Id, $"category_{category.Id}")).ToList();
    }

    public async Task<List<TaskItem>> GetTasksForTodayWithCategories(IEnumerable<Category> enumerable)
    {
        var categories = enumerable.ToList();
        var todayTasks = (await _taskRepo.FindManyAsync(task => Utils.CheckDateForToday(task.CompleteDate), "today")).ToList();
        foreach (var task in todayTasks)
        {
            var category = categories.FirstOrDefault(c => c.Id == task.Category);
            task.CategoryObject = category;
        }

        return todayTasks;
    }

    public async Task<List<TaskItem>> GetTasksForImportantWithCategories(IEnumerable<Category> enumerable)
    {
        var categories = enumerable.ToList();
        var importantTasks = (await _taskRepo.FindManyAsync(task => task.IsImportant == true, "important")).ToList();
        foreach (var task in importantTasks)
        {
            var category =  categories.FirstOrDefault(c => c.Id == task.Category);
            task.CategoryObject = category;
        }
        return importantTasks;
    }

    public async Task<List<Node>> GetNodesForAllByCategories(IEnumerable<Category> categories)
    {
        var nodes = new List<Node>();
        foreach (var category in categories)
        {
            var tasks = await GetTasksByCategory(category);
            Utils.OrderTasks(tasks);
            nodes.Add(new Node(category, tasks));
        }

        return nodes;
    }
   
    public async Task<List<Node>> GetNodesForScheduledWithCategories(IEnumerable<Category> enumerable)
    {
        List<Node> nodes = [new Node("Today", []), new Node("Tomorrow", []), new Node("Others", [])];
        var categories = enumerable.ToList();
        foreach (var node in nodes)
        {
            switch (node.NodeTitle)
            {
                case "Today":
                    foreach (var task in await _taskRepo.FindManyAsync(t => Utils.CheckDateForToday(t.CompleteDate) &&  Utils.CheckDateForScheduled(t.CompleteDate), "today"))
                    {
                        task.CategoryObject = categories.FirstOrDefault(c => c.Id == task.Category);
                        node.Tasks.Add(task);
                    }

                    break;
                
                case "Tomorrow":
                    foreach (var task in await _taskRepo.FindManyAsync(t => Utils.CheckDateForTomorrow(t.CompleteDate), "tomorrow"))
                    {
                        task.CategoryObject = categories.FirstOrDefault(c => c.Id == task.Category);
                        node.Tasks.Add(task);
                    }

                    break;
                
                case "Others":
                    foreach (var task in await _taskRepo.FindManyAsync(t => Utils.CheckDateForLater(t.CompleteDate)))
                    {
                        task.CategoryObject = categories.FirstOrDefault(c => c.Id == task.Category);
                        node.Tasks.Add(task);
                    }

                    break;
            }
            Utils.OrderTasks(node.Tasks);
        }
        return nodes;
    }

    public async Task<List<TaskItem>> GetTasksBySearchWithCategories(string searchParameter, IEnumerable<Category> enumerable)
    {
        var categories = enumerable.ToList();
        var searched = (await _taskRepo.FindManyAsync(t => t.Title.ToLowerInvariant().Contains(searchParameter.ToLowerInvariant()))).ToList();
        foreach (var task in searched)
        {
            var category =  categories.FirstOrDefault(c => c.Id == task.Category);
            task.CategoryObject = category;
        }
        return searched;
    }

    public async Task<int> CountTodayTasks()
    {
        return await _taskRepo.CountAsync(t => Utils.CheckDateForToday(t.CompleteDate), "today");
    }

    public async Task<int> CountScheduledTasks()
    {
        return await _taskRepo.CountAsync(t => Utils.CheckDateForScheduled(t.CompleteDate));
    }

    public async Task<int> CountAllTasks()
    {
        return await _taskRepo.CountAsync();
    }

    public async Task<int> CountImportantTasks()
    {
        return await _taskRepo.CountAsync(t => t.IsImportant == true, "important");
    }
    #endregion

    #region Notifications
    public async Task<Notification?> GetNotification(ObjectId notificationId)
    {
        return await _notificationRepo.GetByIdAsync(notificationId);
    }
    public async Task<bool> InsertNotification(Notification notification)
    {
        return await _notificationRepo.AddAsync(notification);
    }

    public async Task<bool> RemoveNotification(ObjectId notificationId)
    {
        return await _notificationRepo.DeleteAsync(notificationId);
    }
    #endregion
}