using System.IO.Pipes;
using Microsoft.Extensions.Logging;

namespace PlanIt.Core.Services.Pipe;

public class PipeServerController : IDisposable
{
    #region Initialization
    public PipeServerController(ILogger<PipeServerController> logger)
    {
        _logger = logger;
        _pipes = [null];
        _lock = new();
        _writerLock = new();
    }
    #endregion
    
    #region Attributes
    private readonly object _lock;
    private readonly object _writerLock;
    private readonly ILogger<PipeServerController> _logger;
    private List<NamedPipeServerStream?> _pipes;
    private CancellationTokenSource _cancellationTokenSource;
    private bool _isRunning = false;
    private bool _disposed = false;
    private int _bufferSize = 1024;

    private Action<bool>? ConnectionResult;
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

    public void AddConnectedCallback(Action<bool> callback) => ConnectionResult += callback;
    public void AddConnectionBrokeCallback(Action callback) => ConnectionBroke += callback;

    public void StartServer()
    {
        if (_isRunning) return;
        _cancellationTokenSource = new CancellationTokenSource();
        _logger.LogInformation("[PipeServer] Staring pipe server at {time}", DateTimeOffset.Now);
        _logger.LogInformation("[PipeServer] Waiting for registering clients...");
        _isRunning = true;
    }
    
    public byte RegisterClient(string pipeName, Action<byte[]> onReceived, Action<bool>? onConnection = null, Action? onBroke = null) => RegisterClient(_cancellationTokenSource.Token, pipeName, onReceived, onConnection, onBroke);
    private byte RegisterClient(CancellationToken cancellationToken, string pipeName, Action<byte[]> onReceived, Action<bool>? onConnection = null, Action? onBroke = null)
    {
        if (!_isRunning)
        {
            _logger.LogError("[PipeServer] Start server first!");
            return 0;
        }
        
        try
        {
            var pipeStream = new NamedPipeServerStream(
                pipeName,
                PipeDirection.InOut,
                1,
                PipeTransmissionMode.Byte,
                PipeOptions.Asynchronous
            );
            
            _ = Task.Run(() => WaitingForConnection(pipeStream, cancellationToken, onReceived, onConnection, onBroke));

            lock(_lock)
            {
                _pipes.Add(pipeStream);
                return (byte)(_pipes.Count-1);
            }
        }
        catch (OperationCanceledException)
        {
            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError("[PipeServer] Error registering new client: {ex}", ex.Message);
            return 0;
        }
    }
    
    private async Task WaitingForConnection(NamedPipeServerStream pipeStream, CancellationToken cancellationToken, Action<byte[]> onReceived, Action<bool>? onConnection = null, Action? onBroke = null)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("[PipeServer] Waiting for connection...");
            await pipeStream!.WaitForConnectionAsync(cancellationToken);
            _logger.LogInformation("[PipeServer] Connected");
            if (onConnection == null) ConnectionResult?.Invoke(true);
            else onConnection.Invoke(true); 
            
            await ListenForData(pipeStream, onReceived, cancellationToken);
            pipeStream.Disconnect();
            if (onBroke == null) ConnectionBroke?.Invoke();
            else onBroke.Invoke();
            _logger.LogInformation("[PipeServer] Disconnected");
        }
    } 

    private async Task ListenForData(NamedPipeServerStream pipeStream, Action<byte[]> onReceived, CancellationToken cancellationToken)
    {
        if (!pipeStream.IsConnected) return;
        byte[] buffer = new byte[_bufferSize];
        while (pipeStream.IsConnected && !cancellationToken.IsCancellationRequested)
        {
            try
            {
                var readBytes = await pipeStream.ReadAsync(buffer.AsMemory(0, _bufferSize), cancellationToken);
                if (readBytes == 0) break;
                byte[] received = new byte[readBytes];
                Buffer.BlockCopy(buffer, 0, received, 0, readBytes);
                _logger.LogInformation("[PipeServer] Received {ReadBytes} bytes", readBytes);
                onReceived?.Invoke(received);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("[PipeServer] Reading exception: {ex}", ex.Message);
            }
        }
    }

    public void SendData(byte id, byte[] data)
    {
        if (id == 0)
        {
            _logger.LogInformation("[PipeServer] Cant send data from server to server");
            return;
        }
        NamedPipeServerStream pipeStream;
        lock (_lock) pipeStream = _pipes[id]!;
        if (!pipeStream.IsConnected) return;
        _ = Task.Run(() =>
        {
            lock (_writerLock)
            {
                try
                {
                    pipeStream.WriteAsync(data, 0, data.Length).GetAwaiter().GetResult();
                    pipeStream.FlushAsync().GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    _logger.LogError("[PipeServer] Send data error: {ex}", ex.Message);
                }
            }
        });
    }
    
    public void StopAll()
    {
        _logger.LogInformation("[PipeServer] Stopping all pipes at {time}", DateTimeOffset.Now);
        _cancellationTokenSource.Cancel();
        _isRunning = false;
        foreach (var pipeStream in _pipes)
        {
            pipeStream?.Close();
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        try
        {
            StopAll();
        }
        catch (OperationCanceledException) {}
        _cancellationTokenSource.Dispose();
        foreach (var pipeStream in _pipes)
        {
            pipeStream?.Dispose();
        }
    }
}