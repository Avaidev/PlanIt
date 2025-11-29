using System.Diagnostics;
using MongoDB.Bson;
using PlanIt.Core.Services;
using PlanIt.Core.Services.DateTimeMonitor;
using PlanIt.Core.Services.Pipe;
using PlanIt.Data.Interfaces;
using PlanIt.Data.Models;
using PlanIt.Data.Services;

namespace PlanIt.MonitorService;

public class ConditionMonitor : IDisposable
{
    private readonly ILogger<ConditionMonitor> _logger;
    private readonly AppSettings _settings;
    private TasksRepository _tasksRepo;
    private TimeMonitor _monitor;
    private PipeServerController _server;
    private byte _uiId;
    private byte _notificatorId;

    public ConditionMonitor(ILogger<ConditionMonitor> logger, TimeMonitor monitor, PipeServerController server)
    {
        _server = server;
        _logger = logger;
        _monitor = monitor;
        _tasksRepo = new TasksRepository();
        _settings = AppConfigManager.Settings;
    }

    public async Task PrepareHandler()
    {
        _logger.LogInformation("[ConditionMonitor] Preparing handler");
        _server.BufferSize = _settings.BufferSize;
        await _monitor.PrepareMonitorAsync(new TimeObjectRepositoryAdapter<TaskItem>(_tasksRepo), TasksCallback);
        RegisterDailyChecker();
        await Task.Run(RenovateTasksAsync);
    }

    private bool StartNotificator()
    {
        try
        {
            var appPath = Utils.GetAppPath(_settings.NotificatorNameExe, "Notificator");
            if (string.IsNullOrEmpty(appPath))
            {
                _logger.LogError("[ConditionMonitor] No notificator app executable path provided");
                return false;
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = appPath,
                UseShellExecute = true,
                CreateNoWindow = true
            };

            if (OperatingSystem.IsWindows())
            {
                startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            }
            else
            {
                startInfo.UseShellExecute = false;
                startInfo.RedirectStandardOutput = true;
                startInfo.RedirectStandardError = true;
            }

            var process = Process.Start(startInfo);
            if (process == null)
            {
                _logger.LogError("[ConditionMonitor] Failed to start notificator app process");
                return false;
            }

            return true;
        }
        catch
        {
            _logger.LogError("[ConditionMonitor] Failed to start notificator app process");
            return false;
        }
    }
    
    private bool StopNotificator()
    {
        try
        {
            var processes = Process.GetProcessesByName(_settings.NotificatorNameExe);

            bool killed = true;
            foreach (var process in processes)
            {
                try
                {
                    if (!process.HasExited)
                    {
                        process.Kill();
                        if (process.WaitForExit(2000))
                        {
                            _logger.LogDebug("[ConditionMonitor] Stopped background app process {id}", process.Id);
                        }
                        else
                        {
                            _logger.LogWarning("[ConditionMonitor] Failed to stop notificator app process {Id}",
                                process.Id);
                            killed = false;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError("[ConditionMonitor] Failed to stop notificator app process: {ex}]", ex.Message);
                    killed = false;
                }
                finally
                {
                    process.Dispose();
                }
            }

            return killed;
        }
        catch (Exception ex)
        {
            _logger.LogError("[ConditionMonitor] Failed to stop notificator app process: {ex}", ex.Message);
            return false;
        }
    }
    public void TasksCallback(ITimedObject obj, IMonitorItem.TargetTimeContext context)
    {
        if (obj is not TaskItem task) return;
        switch (context)
        {
            case IMonitorItem.TargetTimeContext.NOTIFICATION:
            {
                _server.SendData(_notificatorId, [2, 2, ..task.Id.ToByteArray()]);
                break;
            }

            case IMonitorItem.TargetTimeContext.ENDING:
                _server.SendData(_notificatorId, [2, 1, ..task.Id.ToByteArray()]);
                _server.SendData(_uiId, [1, 2, ..task.Id.ToByteArray()]);
                break;

            case IMonitorItem.TargetTimeContext.CYCLED:
            default:
                return;
        }
    }

    public void StartHandling()
    {
        _server.StartServer();
        _uiId = _server.RegisterClient("PlanItPipe.UI", OnDataReceived); // UI - id = 1;
        _notificatorId = _server.RegisterClient("PlanItPipe.Notificator", OnDataReceived); // Notificator - id = 2;
        _monitor.StartMonitoring();
        if (!StartNotificator()) _logger.LogError("Error starting notificator app");
    }

    private void RegisterDailyChecker()
    {
        var targetTime = DateTime.Today.AddDays(1);
        NonObjectCallback callback = DailyCheckerCallback;
        var repeat = 24;
        
        _monitor.RegisterNonObjForMonitoring(targetTime, callback, repeat);
    }
    
    // Data = [0 target][1 function][2:14 ObjectId] = 14 - operate with task; 
    // Target: 0 - Server; 1 - UI; 2 - Notificator;
    // Function: 0 - Remove Monitor; 1 - Update Monitor; 2 - Add Monitor;
    public void OnDataReceived(byte[] data)
    {
        if (data.Length != 14)
        {
            _logger.LogWarning("[ConditionMonitor] Received data of wrong length");
            return;
        }

        var target = data[0];
        if (target != 0)
        {
            _logger.LogWarning("[ConditionMonitor] Client '0' received data for client '{0}'", target);
            return;
        }

        var function = data[1];
        var idBytes = new byte[12];
        Array.Copy(data, 2, idBytes, 0, 12);
        var objectId = new ObjectId(idBytes);
        switch (function)
        {
            case 0:
                _monitor.RemoveMonitor(objectId);
                break;

            case 1:
                _monitor.RemoveMonitor(objectId, false);
                _monitor.TryAddOne(objectId);
                break;
            
            case 2:
                _monitor.TryAddOne(objectId);
                break;
            
            default:
                _logger.LogWarning($"[ConditionMonitor] Received data with wrong function");
                break;
        }
    }

    public void DailyCheckerCallback()
    {
        _logger.LogInformation("Daily renovating...");
        _monitor.StopMonitoring();
         Task.Run(RenovateTasksAsync).GetAwaiter().GetResult();
        _monitor.ClearAllItems();
        RegisterDailyChecker();
        _monitor.StartMonitoring();
        _server.SendData(_uiId, [1, 1]);
        _logger.LogInformation("Daily renovating completed");
    }

    private async Task RenovateTasksAsync()
    {
        var tasks = await _tasksRepo.GetAll();
        foreach (var task in tasks)
        {
            if (task.Repeat == null) continue;
            var difference = (int)(DateTime.Today - task.CompleteDate.Date).TotalDays;
            
            if (difference <= 0) continue;
            task.IsDone = false;
            var intervalsNum = difference / task.Repeat.Value;
            if (difference % task.Repeat.Value != 0) intervalsNum++;
            
            task.CompleteDate = task.CompleteDate.AddDays(intervalsNum * task.Repeat.Value);
            task.NotifyDate = task.NotifyDate?.AddDays(intervalsNum * task.Repeat.Value);
        }
        
        await _tasksRepo.ReplaceList(tasks);
    }

    public void Dispose()
    {
        _logger.LogInformation("[ConditionMonitor] Stopping at: {time}", DateTimeOffset.Now);
        try
        {
            _server.SendData(_uiId, [1, 0]);
            _server.SendData(_notificatorId, [2, 0]);
        }
        catch (Exception ex)
        {
            _logger.LogError("[ConditionMonitor] Failed to send message when disposing: {message}]", ex.Message);
            StopNotificator();
        }
        _monitor.Dispose();
        _server.Dispose();
    }
}