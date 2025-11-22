namespace PlanIt.Core.Services.Pipe;

public class PipeConfig
{
    public string PipeName { get; set; } = "MyPipe";
    public Action<byte[]>? OnDataReceived { get; set; } = null;
    public Action<bool>? ConnectionResult { get; set; } = null;
    public Action? OnConnectionBroke { get; set; } = null;
    public int MaxConnections { get; set; } = 1;
    public int BufferSize { get; set; } = 4096;
}