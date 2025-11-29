using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using PlanIt.Core.Services;
using PlanIt.Core.Services.Pipe;
using PlanIt.Data.Models;
using PlanIt.Data.Services;
#if WINDOWS
using Microsoft.Win32;
#endif

namespace PlanIt.UI.Services;

public class BackgroundController : IDisposable
{
    #region Initialization

    public BackgroundController(ILogger<BackgroundController> logger, PipeClientController pipeClientController, ViewController viewController)
    {
        _logger = logger;
        _settings = AppConfigManager.Settings;
        _viewController = viewController;
        _pipeClientController = pipeClientController;
        _pipeClientController.BufferSize = _settings.BufferSize;
        _pipeClientController.AddReceivedCallback(OnDataReceived);
        _pipeClientController.AddConnectionBrokeCallback(OnConnectionBroke);
    }
    #endregion

    private enum State {SHUT, CONNECTING, STARTING, CONNECTED, WORKING, RECONNECTING}

    private AppSettings _settings;
    private readonly ViewController _viewController;
    private readonly ILogger<BackgroundController> _logger;
    private PipeClientController? _pipeClientController;
    private bool _startedByUs = false;
    private State _state = State.SHUT;

    public async Task Connect()
    {
        if (_state == State.SHUT) _state = State.CONNECTING;
        if (await _pipeClientController!.Connect(_settings.PipeName + ".UI"))
        {
            _state = State.CONNECTED;
            return;
        }

        if (_state == State.CONNECTING) await StartManually();
        if (_state == State.CONNECTED && _startedByUs &&
            OperatingSystem.IsWindows() &&
            await MessageService.AskYesNoMessage("Do you want to add this app to autostart for better working?"))
        {
            if (SetWindowsRegistry(true))
                await MessageService.SuccessMesssage("The autostart has been successfully installed");
            else await MessageService.ErrorMessage("Error in installing autostart");
        }
    }
    
    private async Task StartManually()
    {
        _state = State.STARTING;
        _logger.LogWarning("[BackgroundController] Error connecting to pipe. Starting server manually");
        if (CheckForExisting())
        {
            _startedByUs = true;
            StopExeApp();
            _startedByUs = false;
        }
        if (OperatingSystem.IsWindows())
        {
            if (TryStartForWindows()) _state = State.WORKING;
        }
        else
        {
            if (StartExeApp()) _state = State.WORKING;
        }
        
        if (_state == State.WORKING)
        {
            await Connect();
            if (_state == State.CONNECTED) return;
        }

        await MessageService.ErrorMessage("Invalid process start. Shutting down");
        _logger.LogCritical("[BackgroundController] Invalid start. Shutting down");
        ShutDown();
    }

    public void StopConnection()
    {
        _pipeClientController!.Disconnect();
        if (_startedByUs) StopExeApp();
        _startedByUs = false;
        _state = State.SHUT;
        _logger.LogInformation("[BackgroundController] Disconnected form background app");
    }

    public async Task ReconnectAsync()
    {
        await MessageService.ErrorMessage("Connection broke ");
        await Task.Delay(1000);
        _state = State.RECONNECTING;
        await Connect();
    
        if (_state == State.CONNECTED)
        {
            _viewController.ReloadView();
            _viewController.IsLoadingVisible = false;
        }
        else
        {
            await MessageService.ErrorMessage($"Reconnection failed. Shutting down");
            ShutDown();
        }
    }

    public async Task SendData(byte[] data, byte function)
    {
        await _pipeClientController!.SendData([0, function, ..data]);
    }

    // Data = [0 target][1 function][2:14 ObjectId] = 14 - operate with task; 
    // Target: 0 - Server; 1 - UI; 2 - Notificator;
    // Function: 0 - ConnectionBroke; 1 - Reload View; 2 - Mark Task as Missed;
    public void OnDataReceived(byte[] data)
    {
        if (data.Length is not (14 or 2))
        {
            _logger.LogWarning("[BackgroundController] Received data of wrong length");
            return;
        }

        var target = data[0];
        if (target != 1)
        {
            _logger.LogWarning("[BackgroundController] Client '1' received data for client '{0}'", target);
            return;
        }

        var function = data[1];
        switch (function)
        {
            case 0:
                OnConnectionBroke();
                break;

            case 1:
                Dispatcher.UIThread.Post(() => _viewController.ReloadView());
                break;
            
            case 2:
            {
                var idBytes = new byte[12];
                Array.Copy(data, 2, idBytes, 0, 12);
                var objectId = new ObjectId(idBytes);
                Dispatcher.UIThread.Post(() => _viewController.MarkTaskAsMissed(objectId));
                break;
            }

            default:
                _logger.LogWarning($"[BackgroundController] Received data with wrong function");
                break;
        }
    }

    public void OnConnectionBroke()
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (_state == State.STARTING) return;
            _state = State.STARTING;

            _viewController.LoadingMessage = "Reconnecting...";
            _viewController.IsLoadingVisible = true;

            _ = ReconnectAsync();
        });
    }

    private void ShutDown()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
        {
            lifetime.Shutdown();
        }
    }

    #region ForWindows
    private bool TryStartForWindows()
    {
        try
        {
            if (StartWindowsRegistry()) return true;

            if (StartExeApp()) return true;

        }
        catch (Exception ex)
        {
            _logger.LogError("[BackgroundController > TryStartForWindows] Failed to start background app process: {ex}", ex.Message);
            return false;
        }

        return false;
    }

    private bool StartWindowsRegistry()
    {
#if WINDOWS
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", false);
            var value = key?.GetValue(_settings.BackgroundName) as string;
            if (!string.IsNullOrEmpty(value) && ParseRegistryValue(value) is string path)
            {
                Process.Start(path);
                return true;
            }
            return false;
        }
        catch
        {
            return false;
        }
#else
        return false;
#endif
    }

    private bool SetWindowsRegistry(bool enable)
    {
#if WINDOWS 
        try
        {
            if (!OperatingSystem.IsWindows())
            {
                _logger.LogWarning("Auto-start is only supported on Windows");
                return false;
            }

            const string registryKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";

            using var key = Registry.CurrentUser.OpenSubKey(registryKey, true);
        
            if (key == null)
            {
                _logger.LogError("Failed to open registry key: {Key}", registryKey);
                return false;
            }

            if (enable)
            {
                var appPath = Utils.GetAppPath(_settings.BackgroundNameExe, "MonitorService");
                if (string.IsNullOrEmpty(appPath))
                {
                    _logger.LogError("Cannot set auto-start: Background app not found");
                    return true;
                }

                var registryValue = $"\"{appPath}\" --autostart";
                key.SetValue(_settings.BackgroundName, registryValue);
            
                _logger.LogInformation("Auto-start enabled for background app: {Path}", appPath);
            }
            else
            {
                key.DeleteValue(_settings.BackgroundName, false);
                _logger.LogInformation("Auto-start disabled for background app");
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set auto-start");
            return false;
        }
#else
        return false;
#endif
    }
    #endregion

    #region As Executable
    private bool StartExeApp()
    {
        try
        {
            var appPath = Utils.GetAppPath(_settings.BackgroundNameExe, "MonitorService");
            if (string.IsNullOrEmpty(appPath))
            {
                _logger.LogError("[BackgroundController] No background app executable path provided");
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
                _logger.LogError("[BackgroundController] Failed to start background app process");
                return false;
            }

            _startedByUs = true;
            return true;
        }
        catch
        {
            _logger.LogError("[BackgroundController] Failed to start background app process");
            return false;
        }
    }

    private bool CheckForExisting()
    {
        try
        {
            var processes = Process.GetProcessesByName(_settings.BackgroundNameExe);
            return (processes.Length > 0);
        }
        catch
        {
            _logger.LogError("[BackgroundController] Failed to check for existing background app processes");
            return false;
        }
    }

    private bool StopExeApp()
    {
        if (!_startedByUs)
        {
            _logger.LogWarning("[BackgroundController] Cant stop app that is not started by us");
            return false;
        }

        try
        {
            var processes = Process.GetProcessesByName(_settings.BackgroundNameExe);

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
                            _logger.LogDebug("[BackgroundController] Stopped background app process {id}", process.Id);
                        }
                        else
                        {
                            _logger.LogWarning("[BackgroundController] Failed to stop background app process {Id}",
                                process.Id);
                            killed = false;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError("[BackgroundController] Failed to stop background app process: {ex}]", ex.Message);
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
            _logger.LogError("[BackgroundController] Failed to stop background app process: {ex}", ex.Message);
            return false;
        }
    }
    #endregion

    #region Utils
    private string ParseRegistryValue(string value)
    {
        var regex = new Regex(@"""(.)+""", RegexOptions.Compiled);
        foreach (Match match in regex.Matches(value))
        {
            if (match.Value.Contains(".exe")) return match.Value;
        }
        return string.Empty;
    }
    #endregion
    
    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}