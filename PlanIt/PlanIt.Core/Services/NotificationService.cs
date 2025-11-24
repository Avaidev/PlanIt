using System.Diagnostics;
using Microsoft.Toolkit.Uwp.Notifications;
using PlanIt.Data.Models;
using PlanIt.Data.Services;

namespace PlanIt.Core.Services;
public static class NotificationService
{
    public static void ShowMissedTask(TaskItem taskItem) =>
        ShowNotification(taskItem.Title, "You missed the task", $"{taskItem.CompleteDate:f}");
    
    public static void ShowNotificationTask(TaskItem taskItem) =>
        ShowNotification(taskItem.Title, $"{Utils.CutString(taskItem.Description, 15)}", $"{taskItem.CompleteDate:f}");
    
    private static void ShowNotification(string title, string message, string other)
    {
        if (OperatingSystem.IsWindows())
        {
#if WINDOWS
            new ToastContentBuilder().AddText(title).AddText(message).AddText(other).Show();
#endif
        }
        else if (OperatingSystem.IsLinux())
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "notify-send",
                    Arguments = $"\"{title}\" \"{message} at {other}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };
            process.Start();
        }
        else if (OperatingSystem.IsMacOS())
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "osascript",
                    Arguments = $"-e 'display notification \"{message} at {other}\" with title \"{title}\"'",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };
            process.Start();
        }
    }
}