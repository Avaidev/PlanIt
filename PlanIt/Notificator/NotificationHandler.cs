using System.Diagnostics;
#if WINDOWS
using Windows.UI.Notifications;
#endif
using Microsoft.Extensions.Logging;
using Microsoft.Toolkit.Uwp.Notifications;
using MongoDB.Bson;
using PlanIt.Core.Services;
using PlanIt.Core.Services.Pipe;
using PlanIt.Data.Models;
using PlanIt.Data.Services;

namespace PlanIt.Notificator;

public class NotificationHandler : IDisposable
{
    public NotificationHandler(ILogger<NotificationHandler> logger, PipeClientController clientController)
    {
        _logger = logger;
        _pipeClientController = clientController;
        _taskRepository = new TasksRepository();
        _settings = AppConfigManager.Settings;
        _pipeClientController.BufferSize = _settings.BufferSize;
        _pipeClientController.AddConnectionBrokeCallback(OnConnectionBroke);
        _pipeClientController.AddReceivedCallback(OnDataReceived);
    }
    
    private readonly TasksRepository _taskRepository;
    private readonly AppSettings _settings;
    private readonly ILogger<NotificationHandler> _logger;
    private readonly PipeClientController _pipeClientController;
    private CancellationTokenSource _cancellationTokenSource;

    public async Task RunAsync()
    {
        _cancellationTokenSource = new CancellationTokenSource();
        _logger.LogInformation("[NotificationHandler] Starting");
        if (await _pipeClientController.Connect(_settings.PipeName + ".Notificator", 10000))
        {
            _logger.LogInformation("[NotificationHandler] Connected");
            await WaitingForStopAsync(_cancellationTokenSource.Token);
        }
    }
    private async Task WaitingForStopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(1000, cancellationToken);
        }
    }
    public void Stop()
    {
        _cancellationTokenSource.Cancel();
        _pipeClientController.Disconnect();
    }

    public void OnConnectionBroke() => Stop();

    // Data = [0 target][1 function][2:14 ObjectId] = 14 - operate with task; 
    // Target: 0 - Server; 1 - UI; 2 - Notificator;
    // Function: 0 - ConnectionBroke; 1 - Task Missed; 2 - Task Notification;
    public void OnDataReceived(byte[] data)
    {
        if (data.Length is not (14 or 2))
        {
            _logger.LogWarning("[NotificationHandler] Received data of wrong length");
            return;
        }

        var target = data[0];
        if (target != 2)
        {
            _logger.LogWarning("[NotificationHandler] Client '2' received data for client '{0}'", target);
            return;
        }

        var function = data[1];
        var isMissed = false;
        switch (function)
        {
            case 0:
                OnConnectionBroke();
                break;
            
            case 1:
                isMissed = true;
                goto case 2;
            case 2:
            {
                if (data.Length != 14) break;
                var idBytes = new byte[12];
                Array.Copy(data, 2, idBytes, 0, 12);
                var objectId = new ObjectId(idBytes);
                _ = Task.Run(() => SendNotification(objectId, isMissed));
                break;
            }

            default:
                _logger.LogWarning($"[NotificationHandler] Received data with wrong function");
                break;
        }
    }

    private async Task SendNotification(ObjectId id, bool isMissed = false)
    {
        var task = await _taskRepository.GetTask(id);
        if (task == null)
        {
            _logger.LogWarning($"[NotificationHandler] Received task with id {id} not found");
            return;
        }

        ShowNotification(task.Title, isMissed ? "You missed the task" : $"{Utils.CutString(task.Description, 15)}",
            $"{task.CompleteDate:f}", id.ToString(), isMissed ? "taskMissed0" : "taskNotif0");
    }
    
    private void ShowNotification(string title, string message, string other, string id, string? group = null)
    {
        if (OperatingSystem.IsWindows())
        {
#if WINDOWS
            var tag = id[..8];
            group ??= Guid.NewGuid().ToString()[..4];
            new ToastContentBuilder().AddText(title).AddText(message).AddText(other).Show(t =>
            {
                t.Tag = tag;
                t.Group = group;

                t.Activated += (sender, args) =>
                {
                    var history = ToastNotificationManagerCompat.History.GetHistory();
                    if (history.Any(n => n.Tag == tag && n.Group == group))
                    {
                        ToastNotificationManagerCompat.History.Remove(tag, group);
                    }
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

    public void Dispose()
    {
        _pipeClientController.Dispose();
    }
}