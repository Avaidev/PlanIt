using System.IO.Pipes;
using Microsoft.Extensions.Logging;

namespace PlanIt.Core.Services.Pipe;

public class PipeClientController : IDisposable
{
    #region Initialization
    public PipeClientController(ILogger<PipeClientController> logger)
    {
        _logger = logger;
    }
    #endregion

    #region Attributes
    private readonly ILogger<PipeClientController> _logger;
    private NamedPipeClientStream? _pipeClient;
    private CancellationTokenSource _cancellationTokenSource;
    private bool _disposed = false;
    private int _bufferSize = 1024;
    
    private Action<bool>? ConnectionResult;
    private Action<byte[]>? OnDataReceived;
    private Action? ConnectionBroke;

    public int BufferSize
    {
        get => _bufferSize;
        set 
        {
            if (value >= 1) _bufferSize = value;
        }
    }

    #endregion
    
    public void AddReceivedCallback(Action<byte[]> callback) => OnDataReceived += callback;

    public void AddConnectedCallback(Action<bool> callback) => ConnectionResult += callback;

    public void AddConnectionBrokeCallback(Action callback) => ConnectionBroke += callback;

    public async Task<bool> Connect(string pipeName, int timeout = 5000)
    {
        _cancellationTokenSource = new CancellationTokenSource();
        CancellationToken cancellationToken = _cancellationTokenSource.Token;
        try
        {
            _pipeClient = new NamedPipeClientStream(
                ".",
                pipeName,
                PipeDirection.InOut,
                PipeOptions.Asynchronous);

            _logger.LogInformation("[PipeClient] Connecting to pipe server...");
            await _pipeClient.ConnectAsync(timeout, cancellationToken);
            _logger.LogInformation("[PipeClient] Connected to pipe server");

            ConnectionResult?.Invoke(true);
            _ = Task.Run(() => ListenForData(cancellationToken), cancellationToken);
            return true;
        }
        catch (TimeoutException)
        {
            _logger.LogInformation("[PipeClient] Timed out");
            ConnectionResult?.Invoke(false);
            return false;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError($"[PipeClient] Exception: {ex.Message}");
            ConnectionResult?.Invoke(false);
            return false;
        }
    }

    public void Disconnect()
    {
        _cancellationTokenSource.Cancel();
        _pipeClient!.Close();
        _logger.LogInformation("[PipeClient] Disconnecting from server");
    }

    private async Task ListenForData(CancellationToken cancellationToken)
    {
        if (_pipeClient is not { IsConnected: true }) return;
        _logger.LogInformation("[PipeClient] Listening for data...");
        byte[] buffer = new byte[_bufferSize];
        while (_pipeClient.IsConnected && !cancellationToken.IsCancellationRequested)
        {
            try
            {
                int readBytes = await _pipeClient.ReadAsync(buffer, 0, _bufferSize, cancellationToken);
                if (readBytes == 0) break;
                byte[] received = new byte[readBytes];
                Buffer.BlockCopy(buffer, 0, received, 0, readBytes);
                _logger.LogInformation("[PipeClient] Received {ReadBytes} bytes", readBytes);
                OnDataReceived?.Invoke(received);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("[PipeClient] Reading exception: {ExMessage}", ex.Message);
            }
        }
        _logger.LogInformation("[PipeClient] Connection broke");
        ConnectionBroke?.Invoke();
    }
    
    public async Task SendData(byte[] data)
    {
        if (_pipeClient is { IsConnected: true })
        {
            try
            {
                await _pipeClient.WriteAsync(data, 0, data.Length);
                await _pipeClient.FlushAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PipeClient] Send data error: {ex.Message}");
            }
        }
    }
    
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        try
        {
            Disconnect();
        }catch(OperationCanceledException){}
        _cancellationTokenSource.Dispose();
        _pipeClient?.Dispose();
    }
}