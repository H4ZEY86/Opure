namespace Opure.Observability;

/// <summary>
/// Controls local trace creation without changing any product authority.
/// </summary>
public sealed record OperationalTracePolicy(bool Enabled, string ReleaseChannel)
{
    public static OperationalTracePolicy ForReleaseChannel(string releaseChannel)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(releaseChannel);

        return releaseChannel switch
        {
            "Development" or "Test" => new OperationalTracePolicy(true, releaseChannel),
            "Preview" or "Stable" =>
                new OperationalTracePolicy(false, releaseChannel),
            _ => throw new ArgumentOutOfRangeException(
                nameof(releaseChannel),
                "The trace release channel is unsupported.")
        };
    }
}
