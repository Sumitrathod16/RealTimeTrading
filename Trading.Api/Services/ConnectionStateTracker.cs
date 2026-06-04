namespace Trading.Api.Services;

public sealed class ConnectionStateTracker
{
    private readonly object _lock = new();
    public string State { get; private set; } = "Disconnected";
    public string? LastError { get; private set; }
    private long _messageCount;
    public long MessageCount => Interlocked.Read(ref _messageCount);

    public void SetConnecting()
    {
        lock (_lock)
        {
            State = "Connecting";
            LastError = null;
        }
    }

    public void SetConnected()
    {
        lock (_lock)
        {
            State = "Connected";
            LastError = null;
        }
    }

    public void SetDisconnected(string? error = null)
    {
        lock (_lock)
        {
            State = "Disconnected";
            if (error != null) LastError = error;
        }
    }

    public void SetError(string error)
    {
        lock (_lock)
        {
            State = "Error";
            LastError = error;
        }
    }

    public void IncrementMessages() => Interlocked.Increment(ref _messageCount);

    public (string State, string? LastError, long MessageCount) Snapshot()
    {
        lock (_lock)
            return (State, LastError, MessageCount);
    }
}
