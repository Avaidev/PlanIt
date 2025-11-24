using PlanIt.Data.Interfaces;
using PlanIt.Data.Models;

namespace PlanIt.Data.Services;

public class TasksRepository : IObjectRepository<TaskItem>
{
    private ObjectRepository<TaskItem> _taskRepo = new(Utils.GetFilePath("tasks.bson"));
    
    #region Tasks
    public async Task<bool> Insert(TaskItem task)
    {
        return await _taskRepo.AddAsync(task);
    }

    public async Task<bool> Remove(TaskItem task)
    {
        return await _taskRepo.DeleteAsync(task);
    }

    public async Task<bool> Update(TaskItem task)
    {
        return await _taskRepo.UpdateAsync(task);
    }

    public async Task<bool> RemoveMany(List<TaskItem> tasks)
    {
        bool allDeleted = true;
        foreach (var task in tasks)
        {
            allDeleted = await Remove(task);
        }
        return allDeleted;
    }

    public async Task ReplaceList(List<TaskItem> tasks)
    {
        await _taskRepo.ReplaceAllAsync(tasks);
    }

    public async Task<List<TaskItem>> GetAll()
    {
        return (await _taskRepo.FindManyAsync(t => true)).ToList();
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
        return await _taskRepo.CountAsync(t => Utils.CheckDateForScheduled(t.CompleteDate) && !t.IsDone);
    }

    public async Task<int> CountAll()
    {
        return await _taskRepo.CountAsync();
    }

    public async Task<int> CountImportantTasks()
    {
        return await _taskRepo.CountAsync(t => t.IsImportant == true, "important");
    }
    #endregion

}