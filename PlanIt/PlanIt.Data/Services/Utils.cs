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
            Console.WriteLine($"[APPDIR] UI App directory: {currentDir}");

            var projectDir = Directory.GetParent(currentDir)?.Parent?.Parent?.Parent?.FullName;
            if (!string.IsNullOrEmpty(projectDir))
            {
                var backgroundDir = Path.Combine(projectDir, "..", folder.Trim());
                backgroundDir = Path.GetFullPath(backgroundDir);
            
                Console.WriteLine($"[LOOKUP] Looking in background directory: {backgroundDir}");
            
                var productionPath = Path.Combine(backgroundDir, exeName);
                if (File.Exists(productionPath))
                {
                    Console.WriteLine($"[FOUND] Found in production: {productionPath}");
                    return productionPath;
                }
            
                var developmentPath = Path.Combine(backgroundDir, "bin", "Debug", OperatingSystem.IsWindows() ? "net9.0-windows10.0.26100.0" : "net9.0", exeName);
                if (File.Exists(developmentPath))
                {
                    Console.WriteLine($"[FOUND] Found in development: {developmentPath}");
                    return developmentPath;
                }
            
                var releasePath = Path.Combine(backgroundDir, "bin", "Release", OperatingSystem.IsWindows() ? "net9.0-windows10.0.26100.0" : "net9.0", exeName);
                if (File.Exists(releasePath))
                {
                    Console.WriteLine($"[FOUND] Found in release: {releasePath}");
                    return releasePath;
                }
            }

            var currentDirPath = Path.Combine(currentDir, exeName);
            if (File.Exists(currentDirPath))
            {
                Console.WriteLine($"[FOUND] Found in current directory: {currentDirPath}");
                return currentDirPath;
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