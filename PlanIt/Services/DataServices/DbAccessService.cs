using System;
using System.Collections.Generic;
using System.IO;
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
        SetDataDirectory();
        _taskRepo = new ObjectRepository<TaskItem>(Path.Combine(_baseDirectory, "tasks.bson"));
        _categoryRepo = new ObjectRepository<Category>(Path.Combine(_baseDirectory, "categories.bson"));
        _notificationRepo = new ObjectRepository<Notification>(Path.Combine(_baseDirectory, "notifications.bson"));
    }

    private void SetDataDirectory()
    {   
        var binPath = AppContext.BaseDirectory;
        var projectPath = Path.Combine(binPath, "..", "..", "..");
        var fullPath = Path.GetFullPath(projectPath);
        
        _baseDirectory = Path.Combine(fullPath, "Data");
    }
    
    //Categories
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
        return await _taskRepo.FindManyAsync(task => task.Category == category.Id);
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