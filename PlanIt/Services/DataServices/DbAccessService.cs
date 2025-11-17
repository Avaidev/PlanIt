using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using PlanIt.Models;

namespace PlanIt.Services.DataServices;

public class DbAccessService
{
    private string _baseDirectory { get; set; }
    private ObjectRepository<TaskItem> _taskRepo;
    private ObjectRepository<Category> _categoryRepo;
    private ObjectRepository<Notification> _notificationRepo;

    public DbAccessService()
    {
        _baseDirectory = GetDataDirectory();
        _taskRepo = new ObjectRepository<TaskItem>(Path.Combine(_baseDirectory, "tasks.bson"));
        _categoryRepo = new ObjectRepository<Category>(Path.Combine(_baseDirectory, "categories.bson"));
        _notificationRepo = new ObjectRepository<Notification>(Path.Combine(_baseDirectory, "notifications.bson"));
    }

    private static string GetDataDirectory()
    {   
        var binPath = AppContext.BaseDirectory;
        var projectPath = Path.Combine(binPath, "..", "..", "..");
        var fullPath = Path.GetFullPath(projectPath);
        
        return Path.Combine(fullPath, "Data");
    }
    
    //Categories
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

    //Tasks
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
        return await _taskRepo.FindManyAsync(task => task.Category == category.Id, $"category_{category.Id}");
    }

    public async Task<List<TaskItem>> GetTasksForToday()
    {
        var todayTasks = await _taskRepo.FindManyAsync(task => task.CompleteDate.Date == DateTime.Now.Date, "today");
        var categories = await GetAllCategories();
        foreach (var task in todayTasks)
        {
            var category = categories.FirstOrDefault(c => c.Id == task.Category);
            task.CategoryObject = category;
        }

        return todayTasks;
    }

    public async Task<List<TaskItem>> GetTasksForImportant()
    {
        var importantTasks = await _taskRepo.FindManyAsync(task => task.IsImportant == true, "important");
        var categories = await GetAllCategories();
        foreach (var task in importantTasks)
        {
            var category =  categories.FirstOrDefault(c => c.Id == task.Category);
            task.CategoryObject = category;
        }
        return importantTasks;
    }

    public async Task<List<Node>> GetNodesForAll()
    {
        var categories = await GetAllCategories();
        var nodes = new List<Node>();
        foreach (var category in categories)
        {
            var tasks = await GetTasksByCategory(category);
            nodes.Add(new Node(category, tasks));
        }

        return nodes;
    }

    public async Task<List<Node>> GetNodesForScheduled()
    {
        var nodes = new List<Node>();
        for (var i = 0; i < 3; i++)
        {
            switch (i)
            {
                case 0:
                    nodes.Add(new Node("Today", await _taskRepo.FindManyAsync(t => t.CompleteDate.Date == DateTime.Now.Date && t.CompleteDate.TimeOfDay > DateTime.Now.TimeOfDay)));
                    break;
                case 1:
                    nodes.Add(new Node("Tomorrow", await _taskRepo.FindManyAsync(t => t.CompleteDate.AddDays(1).Date == DateTime.Now.AddDays(1).Date)));
                    break;
                default:
                    nodes.Add(new Node("Others", await _taskRepo.FindManyAsync(t => t.CompleteDate.Date >= DateTime.Now.AddDays(2).Date)));
                    break;
            }
        }

        return nodes;
    }

    public async Task<int> CountTodayTasks()
    {
        return await _taskRepo.CountAsync(t => t.CompleteDate.Date == DateTime.Now.Date, "today");
    }

    public async Task<int> CountScheduledTasks()
    {
        return await _taskRepo.CountAsync(t => t.CompleteDate > DateTime.Now, "scheduled");
    }

    public async Task<int> CountAllTasks()
    {
        return await _taskRepo.CountAsync();
    }

    public async Task<int> CountImportantTasks()
    {
        return await _taskRepo.CountAsync(t => t.IsImportant == true, "important");
    }

    //Notifications
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
}