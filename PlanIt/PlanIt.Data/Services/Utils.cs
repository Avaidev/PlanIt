using System.Diagnostics;
using PlanIt.Data.Models;

namespace PlanIt.Data.Services;

public static class Utils
{
    public static bool CheckDateForToday(DateTime date) => date.Date == DateTime.Today;
    public static bool CheckDateForTomorrow(DateTime date) => date.Date == DateTime.Today.AddDays(1).Date;
    public static bool CheckDateForLater(DateTime date) => date.Date >= DateTime.Today.AddDays(2).Date;
    public static bool CheckDateForScheduled(DateTime date) => date > DateTime.Now;
    public static bool CheckDateForTodayScheduled(DateTime date) => CheckDateForToday(date) && CheckDateForScheduled(date);

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
    
    public static string GetDataDirectory()
    {
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        string dataPath = Path.Combine(baseDir, "..", "..", "..", "..", "Data");
        dataPath = Path.GetFullPath(dataPath);
        
        if (Directory.Exists(dataPath))
        {
            return dataPath;
        }
        
        string parentDataPath = Path.Combine(baseDir, "..", "Data");
        parentDataPath = Path.GetFullPath(parentDataPath);
        
        if (Directory.Exists(parentDataPath))
        {
            return parentDataPath;
        }
        
        string localDataPath = Path.Combine(baseDir, "Data");
        Directory.CreateDirectory(localDataPath);
        Console.WriteLine($"Created Data directory at {localDataPath}");
        return localDataPath;
    }

    public static string GetFilePath(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
            throw new ArgumentException("File name cannot be null or empty", nameof(fileName));
        
        var dataDirectory = GetDataDirectory();
        return Path.Combine(dataDirectory, fileName);
    }
}