namespace Opure.Bootstrap.Windows;

internal enum BootstrapSupervisorMode
{
    Normal = 0,
    Recovering = 1,
    SafeMode = 2
}

internal enum BootstrapProcessHealth
{
    Starting = 0,
    Ready = 1,
    Stopping = 2,
    Stopped = 3,
    Crashed = 4,
    Quarantined = 5,
    Unavailable = 6
}

internal enum BootstrapProcessExitKind
{
    CleanExit = 0,
    Crash = 1,
    PolicyStop = 2,
    Quarantine = 3
}

internal sealed record BootstrapSupervisedProcessIdentity(
    BootstrapProcessClass ProcessClass,
    string InstanceId,
    int ProcessId,
    DateTimeOffset StartTimeUtc,
    string ExecutableSha256,
    string ProductVersion,
    string? BootId)
{
    internal bool MatchesOperatingSystemProcess(
        int processId,
        DateTimeOffset startTimeUtc)
    {
        return ProcessId == processId && StartTimeUtc == startTimeUtc;
    }

    internal BootstrapSupervisedProcessIdentity WithBootId(string bootId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(bootId);
        return this with { BootId = bootId };
    }

    internal static BootstrapSupervisedProcessIdentity Create(
        BootstrapProcessClass processClass,
        IBootstrapOwnedProcess process,
        BootstrapBinaryIdentity binaryIdentity)
    {
        ArgumentNullException.ThrowIfNull(process);
        ArgumentNullException.ThrowIfNull(binaryIdentity);

        return new BootstrapSupervisedProcessIdentity(
            processClass,
            Guid.NewGuid().ToString("N"),
            process.ProcessId,
            process.StartTimeUtc,
            binaryIdentity.ExecutableSha256,
            binaryIdentity.ProductVersion,
            BootId: null);
    }
}

internal sealed record BootstrapRestartPolicy(
    int MaximumAttempts,
    TimeSpan Window,
    TimeSpan InitialBackoff,
    TimeSpan MaximumBackoff)
{
    internal static BootstrapRestartPolicy Default { get; } = new(
        MaximumAttempts: 3,
        Window: TimeSpan.FromSeconds(30),
        InitialBackoff: TimeSpan.FromMilliseconds(100),
        MaximumBackoff: TimeSpan.FromSeconds(2));

    internal void Validate()
    {
        if (MaximumAttempts < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(MaximumAttempts),
                MaximumAttempts,
                "Maximum restart attempts cannot be negative.");
        }

        if (Window <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(Window),
                Window,
                "The restart window must be positive.");
        }

        if (InitialBackoff < TimeSpan.Zero || MaximumBackoff < InitialBackoff)
        {
            throw new ArgumentOutOfRangeException(
                nameof(InitialBackoff),
                InitialBackoff,
                "Restart backoff bounds are invalid.");
        }
    }
}

internal sealed class BootstrapRestartBudget
{
    private readonly BootstrapRestartPolicy policy;
    private readonly Queue<DateTimeOffset> attempts = new();

    internal BootstrapRestartBudget(BootstrapRestartPolicy policy)
    {
        this.policy = policy ?? throw new ArgumentNullException(nameof(policy));
        policy.Validate();
    }

    internal int TotalAttempts { get; private set; }

    internal bool TryReserve(
        DateTimeOffset now,
        out int attempt,
        out TimeSpan backoff)
    {
        while (attempts.TryPeek(out DateTimeOffset oldest) &&
               now - oldest >= policy.Window)
        {
            attempts.Dequeue();
        }

        if (attempts.Count >= policy.MaximumAttempts)
        {
            attempt = TotalAttempts;
            backoff = TimeSpan.Zero;
            return false;
        }

        attempts.Enqueue(now);
        TotalAttempts++;
        attempt = TotalAttempts;

        double multiplier = Math.Pow(2, attempts.Count - 1);
        double requestedMilliseconds =
            policy.InitialBackoff.TotalMilliseconds * multiplier;

        backoff = TimeSpan.FromMilliseconds(
            Math.Min(
                requestedMilliseconds,
                policy.MaximumBackoff.TotalMilliseconds));

        return true;
    }
}
