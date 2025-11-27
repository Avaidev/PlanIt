using System.Diagnostics;
#if WINDOWS
using Windows.UI.Notifications;
#endif
using Microsoft.Toolkit.Uwp.Notifications;
using PlanIt.Data.Models;
using PlanIt.Data.Services;

namespace PlanIt.Core.Services;
public static class NotificationService
{
    public static void ShowMissedTask(TaskItem taskItem) =>
        ShowNotification(taskItem.Title, "You missed the task", $"{taskItem.CompleteDate:f}", taskItem.Id.ToString());
    
    public static void ShowNotificationTask(TaskItem taskItem) =>
        ShowNotification(taskItem.Title, $"{Utils.CutString(taskItem.Description, 15)}", $"{taskItem.CompleteDate:f}", taskItem.Id.ToString());
    
    private static void ShowNotification(string title, string message, string other, string id)
    {
        if (OperatingSystem.IsWindows())
        {
#if WINDOWS
            var tag = Math.Abs(id.GetHashCode()).ToString()[..4];
            var group = Guid.NewGuid().ToString()[..4];
            new ToastContentBuilder().AddText(title).AddText(message).AddText(other).Show(t =>
            {
                t.Tag = tag;
                t.Group = group;

                t.Activated += (sender, args) =>
                {
                    ToastNotificationManager.History.Remove(tag, group);
                };
            });
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