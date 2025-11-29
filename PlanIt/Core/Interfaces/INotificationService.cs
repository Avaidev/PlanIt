namespace PlanIt.Core.Interfaces;

public interface INotificationService
{
    void Initialize();
    void ShowNotification(string title, string message);
}