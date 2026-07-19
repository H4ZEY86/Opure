namespace Opure.Runtime;

public sealed class RuntimeShutdownSignal : IDisposable
{
    private readonly CancellationTokenSource cancellation = new();
    private readonly TaskCompletionSource<string> completion = new(
        TaskCreationOptions.RunContinuationsAsynchronously);

    public CancellationToken Token => cancellation.Token;

    public bool Request(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        if (!completion.TrySetResult(reason))
        {
            return false;
        }

        cancellation.Cancel();
        return true;
    }

    public Task<string> WaitAsync()
    {
        return completion.Task;
    }

    public void Dispose()
    {
        cancellation.Dispose();
    }
}
