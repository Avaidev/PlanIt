namespace PlanIt.Data.Models;

public class AppSettings
{
    public string AppName { get; set; } = "PlanIt";
    public string ExeName { get; set; } = "Notifier";
    public int MaxConnections { get; set; } = 1;
    public int BufferSize { get; set; } = 1024;
    public string PipeName { get; set; } = "PlanItPipe";
    public string Theme { get; set; } = "light";
    
    public AppSettings(){}
}