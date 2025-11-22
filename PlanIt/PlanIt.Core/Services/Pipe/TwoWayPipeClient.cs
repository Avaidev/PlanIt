using System.IO.Pipes;
using Microsoft.Extensions.Logging;

namespace PlanIt.Core.Services.Pipe;

public class TwoWayPipeClient : IDisposable
{
    #region Initialization
    public TwoWayPipeClient(ILogger<TwoWayPipeClient> logger, PipeConfig config)
    {
        _logger = logger;
        _config = config;
        _cancellationTokenSource =  new CancellationTokenSource();
    }
    #endregion

    #region Attributes
    private NamedPipeClientStream? _pipeClient;
    private readonly PipeConfig _config;
    private readonly ILogger<TwoWayPipeClient> _logger;
    private readonly CancellationTokenSource _cancellationTokenSource;
    public bool IsConnected => _pipeClient?.IsConnected ?? false;
    #endregion
    
    public void AddCallback(Action<byte[]> callback)
    {
        _config.OnDataReceived += callback;
    }

    public void AddConnectionBrokeCallback(Action callback)
    {
        _config.OnConnectionBroke += callback;
    }

    public async Task<bool> Connect(int timeout = 5000)
    {
        CancellationToken cancellationToken = _cancellationTokenSource.Token;
        try
        {
            _pipeClient = new NamedPipeClientStream(
                ".",
                _config.PipeName,
                PipeDirection.InOut,
                PipeOptions.Asynchronous);

            _logger.LogInformation("[PipeClient] Connecting to pipe server...");
            await _pipeClient.ConnectAsync(timeout, cancellationToken);
            _logger.LogInformation("[PipeClient] Connected to pipe server");

            _config.ConnectionResult?.Invoke(true);
            _ = Task.Run(() => ListenForData(cancellationToken), cancellationToken);
            return true;
        }
        catch (TimeoutException)
        {
            _logger.LogInformation("[PipeClient] Timed out");
            _config.ConnectionResult?.Invoke(false);
            return false;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError($"[PipeClient] Exception: {ex.Message}");
            _config.ConnectionResult?.Invoke(false);
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
        byte[] buffer = new byte[_config.BufferSize];
        while (_pipeClient.IsConnected && !cancellationToken.IsCancellationRequested)
        {
            try
            {
                int readBytes = await _pipeClient.ReadAsync(buffer, 0, _config.BufferSize, cancellationToken);
                if (readBytes == 0) break;
                byte[] received = new byte[readBytes];
                Buffer.BlockCopy(buffer, 0, received, 0, readBytes);
                _logger.LogDebug("[PipeClient] Received {ReadBytes} bytes]", readBytes);
                _config.OnDataReceived?.Invoke(received);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("[PipeClient] Reading exception: {ExMessage}", ex.Message);
            }
        }
        _config.OnConnectionBroke?.Invoke();
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
        Disconnect();
        _cancellationTokenSource.Dispose();
        _pipeClient?.Dispose();
    }
}