namespace PlanIt.Data.Models;

public class AppSettings
{
    public string BackgroundName { get; set; } = "Monitor";
    public string BackgroundNameExe { get; set; } = "Monitor";
    public string NotificatorName { get; set; } = "Notifier";
    public string NotificatorNameExe { get; set; } = "Notifier";
    public int BufferSize { get; set; } = 1024;
    public string PipeName { get; set; } = "PlanItPipe";
    public string Theme { get; set; } = "light";
    
    public AppSettings(){}
}