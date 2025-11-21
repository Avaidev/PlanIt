using System.Diagnostics;
using PlanIt.Core.Models;

namespace PlanIt.Core.Services;

public static class Utils
{
    public static bool CheckDateForToday(DateTime date) => date.Date == DateTime.Today;
    public static bool CheckDateForTomorrow(DateTime date) => date.Date == DateTime.Today.AddDays(1).Date;
    public static bool CheckDateForLater(DateTime date) => date.Date >= DateTime.Today.AddDays(2).Date;
    public static bool CheckDateForScheduled(DateTime date) => date > DateTime.Now;

    public static void OrderTasks(IList<TaskItem> tasks)
    {
        var ordered = tasks.OrderByDescending(t => t.IsImportant)
            .ThenBy(t => t.IsDone)
            .ThenBy(t => t.CompleteDate).ToList();

        tasks.Clear();
        foreach (var item in ordered)
        {
            tasks.Add(item);
        }
    }


    private static string GetAppDataPath()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appDataPath, "PlanIt", "Data");
    }
    
    public static string GetDataDirectory()
    {
        var possibleDataPaths = new[]
        {
            // 1. Application base directory (production and development output)
            Path.Combine(AppContext.BaseDirectory, "Data"),
            // 2. Current working directory
            Path.Combine(Directory.GetCurrentDirectory(), "Data"),
            // 3. AppData as fallback
            GetAppDataPath()
        };
            
        foreach (var path in possibleDataPaths)
        {
            if (Directory.Exists(path))
            {
                Debug.WriteLine($"Found Data directory at: {path}");
                return Path.GetFullPath(path);
            }
        }
        
        var defaultPath = Path.Combine(AppContext.BaseDirectory, "Data");
        Debug.WriteLine($"Creating Data directory at: {defaultPath}");
        Directory.CreateDirectory(defaultPath);
        return Path.GetFullPath(defaultPath);
    }

    public static string GetFilePath(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
            throw new ArgumentException("File name cannot be null or empty", nameof(fileName));
        
        var dataDirectory = GetDataDirectory();
        return Path.Combine(dataDirectory, fileName);
    }
}