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
        var currentDir = AppContext.BaseDirectory;
        var dataFolder = "Data";

        var availablePaths = new[]
        {
            Path.Combine(currentDir, "..", dataFolder),
            Path.Combine(currentDir, "..", "..", dataFolder),
            Path.Combine(currentDir, "..", "..", "..", "..", dataFolder),
        };

        foreach (var path in availablePaths)
        {
            var fullPath = Path.GetFullPath(path);
            if (Directory.Exists(fullPath))
            {
                return fullPath;
            }
        }
        
        var localDataPath = Path.Combine(currentDir, "..", "Data");
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

    public static string CutString(string input, int maxWords)
    {
        if (String.IsNullOrEmpty(input)) return input;
        var words = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length <= maxWords) return input;
        
        return string.Join(" ",  words.Take(maxWords)) + "...";
    }

    public static string GetAppPath(string appName, string folder)
    {
        try
        {
            var exeName = OperatingSystem.IsWindows() ? appName + ".exe" : appName;
            var currentDir = AppContext.BaseDirectory;
            var tfm = OperatingSystem.IsWindows() ? "net9.0-windows10.0.26100.0" : "net9.0";
            
            var availablePaths = new[]
            {
                Path.Combine(currentDir, "..", folder, exeName),
                Path.Combine(currentDir, "..", "..", folder, exeName),
                Path.Combine(currentDir, "..", "..", "..", "..", folder, "bin", "Debug", tfm, exeName),
                Path.Combine(currentDir, "..", "..", "..", "..", folder, "bin", "Release", tfm, exeName),
                Path.Combine(currentDir, exeName),
                Path.Combine(currentDir, "..", exeName)
            };

            foreach (var path in availablePaths)
            {
               var fullPath = Path.GetFullPath(path);
               Console.WriteLine($"[LOOKUP] {fullPath}");
               if (File.Exists(fullPath))
               {
                   Console.WriteLine($"[FOUND] Found in: {fullPath}");
                   return fullPath;
               }
            }

            Console.WriteLine($"[NOT FOUND] Background app not found");
            return string.Empty;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[BackgroundController] Error finding background app path: {ex.Message}");
            return string.Empty;
        }
    }
}