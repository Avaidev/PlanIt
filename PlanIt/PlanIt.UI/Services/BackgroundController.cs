using System;
using System.Diagnostics;
using System.IO;
using System.ServiceProcess;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using MongoDB.Bson;
using PlanIt.Core.Services;
using PlanIt.Core.Services.Pipe;

namespace PlanIt.UI.Services;

public class BackgroundController : IDisposable
{
    #region Initialization

    public BackgroundController(ILogger<BackgroundController> logger, TwoWayPipeClient pipeClient, ViewController viewController)
    {
        _logger = logger;
        _viewController = viewController;
        _pipeClient = pipeClient;
        _pipeClient.AddCallback(bytes =>
        {
            var objectId = new ObjectId(bytes);
            _viewController.MarkTaskAsMissed(objectId);
        });
        _pipeClient.AddConnectionBrokeCallback(() =>
        {
            _ = MessageService.WarningMessage("Connection with background broke");
            _ = StartConnection();
        });
    }
    #endregion

    private const string SERVICE_NAME = "PlanIt.Background";
    private const string EXE_NAME = "PlanIt.Background";
    private readonly ViewController _viewController;
    private ILogger<BackgroundController> _logger;
    private TwoWayPipeClient? _pipeClient;
    private bool _startedByUs = false;
    
    public async Task StartConnection()
    {
        var connected = await _pipeClient!.Connect();
        if (!connected) await StartManually();
    }

    private async Task StartManually()
    {
        switch (GetPlatform())
        {
            case "win":
            {
                var ask = await MessageService.AskYesNoCancelMessage(
                    "Do you want to add out app to autostart for better working (Yes - add / No - start without adding / Cancel - exit)");
                if (ask == null) throw new Exception("Failed to load background");
                if ((bool)ask)
                {
                    if (await StartForWindowsAsync()) break;
                }
                else if (await StartExeApp()) break;
                _logger.LogCritical("[BackgroundController] Failed to start background app process");
                throw new Exception("[BackgroundController] Failed to start background app process");
            }

            case "linux":
            case "macos":
                throw new NotImplementedException();
                
            default:
                _logger.LogCritical("[BackgroundController] Invalid OS: {GetPlatform}", GetPlatform());
                throw new Exception($"[BackgroundController] Invalid OS: {GetPlatform()}");
        }

        var connected = await _pipeClient!.Connect();
        if (!connected)
        {
            _logger.LogCritical("[BackgroundController] Failed to connect to pipe");
            throw new Exception("[BackgroundController] Failed to connect to pipe");
        }
    }

    public void StopConnection()
    {
        _pipeClient!.Disconnect();
        if (_startedByUs) StopExeApp();
        _startedByUs = false;
        _logger.LogInformation("[BackgroundController] Disconnected form background app");
    }

    public async Task SendData(byte[] data)
    {
        await _pipeClient!.SendData(data);
    }

    #region ForWindows
    private async Task<bool> StartForWindowsAsync()
    {
        try
        {
            if (IsWindowsServiceInstalled() && StartWindowsService()) return true;
            
            if (StartWindowsRegistry()) return true;
            
            return await StartExeApp();
        }
        catch (Exception ex)
        {
            _logger.LogError("[BackgroundController] Failed to start background app process: {ex}", ex.Message);
            return false;
        }
    }

    private bool IsWindowsServiceInstalled()
    {
#if WINDOWS
        try
        {
            using var controller = new ServiceController(SERVICE_NAME);
            var status = controller.Status;
            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
#else
        return false;
#endif
    }

    private bool StartWindowsService()
    {
#if WINDOWS
        try
        {
            using var controller = new ServiceController(SERVICE_NAME);
            if (controller.Status != ServiceControllerStatus.Running)
            {
                controller.Start();
                controller.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(30));
            }

            return true;
        }
        catch (Exception ex)
        {
            return false;
        }
#else
        return false;
#endif
    }

    private bool StartWindowsRegistry()
    {
#if WINDOWS
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", false);
            var value = key?.GetValue(SERVICE_NAME) as string;
            if (!string.IsNullOrEmpty(value))
            {
                Process.Start(value);
                return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            return false;
        }
#else
        return false;
#endif
    }
    #endregion

    #region As Executable
    private async Task<bool> StartExeApp()
    {
        try
        {
            var appPath = GetBackgroundAppPath();
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

            if (GetPlatform() == "win")
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
        catch (Exception ex)
        {
            _logger.LogError("[BackgroundController] Failed to start background app process");
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
            var processName = Path.GetFileNameWithoutExtension(EXE_NAME);
            var processes = Process.GetProcessesByName(processName);

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
    private string GetPlatform()
    {
        if (OperatingSystem.IsWindows()) return "win";
        if (OperatingSystem.IsLinux()) return "linux";
        if (OperatingSystem.IsMacOS()) return "macos";
        return "unknown";
    }

    private string GetBackgroundAppPath()
    {
        try
        {
            var currentDir = AppContext.BaseDirectory;

            if (GetPlatform() == "win")
            {
                var appPath = Path.Combine(currentDir, EXE_NAME + ".exe");
                if (File.Exists(appPath)) return appPath;
            }
            else
            {
                var appPath = Path.Combine(currentDir, EXE_NAME);
                if (File.Exists(appPath)) return appPath;
            }
            
            return string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError("[BackgroundController] Error finding background app path: {ex}", ex.Message);
            return string.Empty;
        }
    }
    #endregion
    
    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}