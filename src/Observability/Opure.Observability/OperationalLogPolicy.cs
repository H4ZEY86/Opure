namespace Opure.Observability;

public sealed class OperationalLogPolicy
{
    public OperationalLogPolicy(
        long maximumActiveFileBytes = 8 * 1024 * 1024,
        int maximumRetainedFileCount = 16,
        TimeSpan? maximumRetainedAge = null,
        int maximumMessageCharacters = 2048,
        int maximumAttributeCount = 24,
        int maximumAttributeNameCharacters = 64,
        int maximumAttributeValueCharacters = 512,
        int maximumEventBytes = 16 * 1024,
        int maximumCleanupFileCount = 256,
        OperationalRedactionProfile? redactionProfile = null)
    {
        MaximumActiveFileBytes = ValidateRange(
            maximumActiveFileBytes,
            256,
            1024L * 1024 * 1024,
            nameof(maximumActiveFileBytes));
        MaximumRetainedFileCount = ValidateRange(
            maximumRetainedFileCount,
            1,
            1024,
            nameof(maximumRetainedFileCount));
        MaximumRetainedAge = maximumRetainedAge ?? TimeSpan.FromDays(14);
        MaximumMessageCharacters = ValidateRange(
            maximumMessageCharacters,
            1,
            16 * 1024,
            nameof(maximumMessageCharacters));
        MaximumAttributeCount = ValidateRange(
            maximumAttributeCount,
            0,
            128,
            nameof(maximumAttributeCount));
        MaximumAttributeNameCharacters = ValidateRange(
            maximumAttributeNameCharacters,
            1,
            128,
            nameof(maximumAttributeNameCharacters));
        MaximumAttributeValueCharacters = ValidateRange(
            maximumAttributeValueCharacters,
            1,
            4096,
            nameof(maximumAttributeValueCharacters));
        MaximumEventBytes = ValidateRange(
            maximumEventBytes,
            256,
            1024 * 1024,
            nameof(maximumEventBytes));
        MaximumCleanupFileCount = ValidateRange(
            maximumCleanupFileCount,
            MaximumRetainedFileCount,
            4096,
            nameof(maximumCleanupFileCount));
        RedactionProfile =
            redactionProfile ?? OperationalRedactionProfile.LocalDiagnostics;

        if (MaximumRetainedAge <= TimeSpan.Zero ||
            MaximumRetainedAge > TimeSpan.FromDays(366))
        {
            throw new ArgumentOutOfRangeException(
                nameof(maximumRetainedAge),
                "Operational log retention must be greater than zero and no more than 366 days.");
        }

        if (MaximumEventBytes > MaximumActiveFileBytes)
        {
            throw new ArgumentException(
                "The maximum event size cannot exceed the active-file size.",
                nameof(maximumEventBytes));
        }
    }

    public long MaximumActiveFileBytes { get; }

    public int MaximumRetainedFileCount { get; }

    public TimeSpan MaximumRetainedAge { get; }

    public int MaximumMessageCharacters { get; }

    public int MaximumAttributeCount { get; }

    public int MaximumAttributeNameCharacters { get; }

    public int MaximumAttributeValueCharacters { get; }

    public int MaximumEventBytes { get; }

    public int MaximumCleanupFileCount { get; }

    public OperationalRedactionProfile RedactionProfile { get; }

    private static int ValidateRange(
        int value,
        int minimum,
        int maximum,
        string parameterName)
    {
        if (value < minimum || value > maximum)
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }

        return value;
    }

    private static long ValidateRange(
        long value,
        long minimum,
        long maximum,
        string parameterName)
    {
        if (value < minimum || value > maximum)
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }

        return value;
    }
}
