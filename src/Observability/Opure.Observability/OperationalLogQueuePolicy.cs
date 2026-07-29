namespace Opure.Observability;

public sealed class OperationalLogQueuePolicy
{
    private static readonly TimeSpan MinimumTimeout =
        TimeSpan.FromMilliseconds(10);
    private static readonly TimeSpan MaximumTimeout =
        TimeSpan.FromMinutes(1);

    public OperationalLogQueuePolicy(
        int capacity = 1024,
        TimeSpan? completionTimeout = null,
        TimeSpan? sinkDisposalTimeout = null)
    {
        if (capacity is < 1 or > 65_536)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity));
        }

        Capacity = capacity;
        CompletionTimeout = ValidateTimeout(
            completionTimeout ?? TimeSpan.FromSeconds(5),
            nameof(completionTimeout));
        SinkDisposalTimeout = ValidateTimeout(
            sinkDisposalTimeout ?? TimeSpan.FromSeconds(5),
            nameof(sinkDisposalTimeout));
    }

    public int Capacity { get; }

    public TimeSpan CompletionTimeout { get; }

    public TimeSpan SinkDisposalTimeout { get; }

    private static TimeSpan ValidateTimeout(
        TimeSpan value,
        string parameterName)
    {
        if (value < MinimumTimeout || value > MaximumTimeout)
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }

        return value;
    }
}
