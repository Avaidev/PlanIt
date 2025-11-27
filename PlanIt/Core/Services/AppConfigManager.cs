using System.Text.Json;
using PlanIt.Data.Models;
using PlanIt.Data.Services;

namespace PlanIt.Core.Services;

public static class AppConfigManager
{
    static AppConfigManager()
    {
        _settingsFilePath = Utils.GetFilePath("appsettings.json");
    }

    private static readonly string _settingsFilePath;
    private static AppSettings? _settings;

    public static AppSettings Settings
    {
        get
        {
            if (_settings == null)
                LoadSettings();
            return _settings!;
        }
    }
    
    private static void LoadSettings()
    {
        try
        {
            if (File.Exists(_settingsFilePath))
            {
                var json = File.ReadAllText(_settingsFilePath);
                _settings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
            else
            {
                SaveSettings();
            }
        }
        catch
        {
            _settings = new AppSettings();
        }
    }

    public static void SaveSettings()
    {
        try
        {
            var directory = Path.GetDirectoryName(_settingsFilePath);
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            var json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
            
            File.WriteAllText(_settingsFilePath, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving settings: {ex.Message}");
        }
    }

    public static void SaveSettings(AppSettings newSettings)
    {
        _settings = newSettings;
        SaveSettings();
    }
}