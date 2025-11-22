using System.IO.Pipes;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;

namespace PlanIt.Core.Services.Pipe;

public class TwoWayPipeServer : IDisposable
{
    #region Initialization
    public TwoWayPipeServer(ILogger<TwoWayPipeServer> logger, PipeConfig config)
    {
        _logger = logger;
        _config = config;
        _cancellationTokenSource = new CancellationTokenSource();
    }
    #endregion

    #region Attributes
    private readonly PipeConfig _config;
    private NamedPipeServerStream? _pipeServer;
    private readonly ILogger<TwoWayPipeServer> _logger;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private bool _isRunning = false;
    #endregion

    public void AddCallback(Action<byte[]> callback)
    {
        _config.OnDataReceived += callback;
    }
    
    public void StartServer()
    {
        if (_isRunning) return;
        _logger.LogInformation("[PipeServer] Staring pipe server at {time}", DateTimeOffset.Now);
        _isRunning = RunServer(_cancellationTokenSource.Token);
    }

    public void StopServer()
    {
        _logger.LogInformation("[PipeServer] Stopping pipe server at {time}", DateTimeOffset.Now);
        _cancellationTokenSource.Cancel();
        _isRunning = false;
        _pipeServer?.Close();
    }
    
    private bool RunServer(CancellationToken cancellationToken)
    {
        try
        {
            _pipeServer = new NamedPipeServerStream(
                _config.PipeName,
                PipeDirection.InOut,
                _config.MaxConnections,
                PipeTransmissionMode.Byte,
                PipeOptions.Asynchronous
            );

            _ = Task.Run(() => WaitingForConnection(cancellationToken), cancellationToken);
            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "[PipeServer] Exception");
            return false;
        }
    }

    private async Task WaitingForConnection(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("[PipeServer] Waiting for connection...");
            await _pipeServer!.WaitForConnectionAsync(cancellationToken);
            _logger.LogInformation("[PipeServer] Connected");
            
            await ListenForData(cancellationToken);
            _pipeServer.Disconnect();
            _logger.LogInformation("[PipeServer] Disconnected");
        }
    }

    private async Task ListenForData(CancellationToken cancellationToken)
    {
        if (_pipeServer is not { IsConnected: true }) return;
        byte[] buffer = new byte[_config.BufferSize];
        while (_pipeServer.IsConnected && !cancellationToken.IsCancellationRequested)
        {
            try
            {
                var readBytes = await _pipeServer.ReadAsync(buffer.AsMemory(0, _config.BufferSize), cancellationToken);
                if (readBytes == 0) break;
                byte[] received = new byte[readBytes];
                Buffer.BlockCopy(buffer, 0, received, 0, readBytes);
                _logger.LogDebug("[PipeServer] Received {ReadBytes} bytes]", readBytes);
                _config.OnDataReceived?.Invoke(received);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("[PipeServer] Reading exception: {ExMessage}", ex.Message);
            }
        }
        
    }
    
    public async Task SendData(byte[] data)
    {
        if (_pipeServer is { IsConnected: true })
        {
            try
            {
                await _pipeServer.WriteAsync(data, 0, data.Length);
                await _pipeServer.FlushAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PipeServer] Send data error: {ex.Message}");
            }
        }
    }

    public void Dispose()
    {
        StopServer();
        _cancellationTokenSource?.Dispose();
        _pipeServer?.Dispose();
    }
}