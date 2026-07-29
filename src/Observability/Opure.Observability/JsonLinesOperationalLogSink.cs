using Opure.Observability.Contracts;

namespace Opure.Observability;

public sealed class JsonLinesOperationalLogSink : IOperationalLogSink
{
    private readonly OwnedOperationalLogDirectory ownedDirectory;
    private readonly OperationalLogPolicy policy;
    private readonly TimeProvider timeProvider;
    private readonly SemaphoreSlim writeGate = new(1, 1);
    private readonly object healthGate = new();
    private readonly List<RetentionCandidate> retentionSurvivors = [];
    private FileStream? activeStream;
    private IEnumerator<string>? retentionCandidates;
    private long rotationSequence;
    private long totalFailureCount;
    private int consecutiveFailureCount;
    private long partialLineRecoveryCount;
    private DateTime retentionCutoffUtc;
    private string? lastSignalCode;
    private DateTimeOffset? lastSignalTimestampUtc;
    private bool retentionCleanupRequested;
    private bool activeFileRequiresRecovery;
    private bool initialised;
    private bool disposed;

    public JsonLinesOperationalLogSink(
        string channelDataRoot,
        string serviceId,
        OperationalLogPolicy? policy = null,
        TimeProvider? timeProvider = null)
    {
        ownedDirectory = new OwnedOperationalLogDirectory(
            channelDataRoot,
            serviceId);
        this.policy = policy ?? new OperationalLogPolicy();
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async ValueTask<OperationalLogWriteResult> WriteAsync(
        OperationalLogEvent logEvent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(logEvent);

        if (cancellationToken.IsCancellationRequested)
        {
            return OperationalLogWriteResult.Cancelled;
        }

        try
        {
            await writeGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (
            cancellationToken.IsCancellationRequested)
        {
            return OperationalLogWriteResult.Cancelled;
        }

        try
        {
            if (disposed)
            {
                RecordFailure("LOG_SINK_DISPOSED");
                return new OperationalLogWriteResult(
                    OperationalLogWriteState.Failed,
                    "LOG_SINK_DISPOSED");
            }

            SanitisedOperationalLogEvent sanitised =
                OperationalLogSanitiser.Sanitise(logEvent, policy);
            byte[] line = CreateBoundedLine(sanitised);

            if (line.Length > policy.MaximumEventBytes)
            {
                RecordFailure("LOG_EVENT_TOO_LARGE");
                return new OperationalLogWriteResult(
                    OperationalLogWriteState.Rejected,
                    "LOG_EVENT_TOO_LARGE");
            }

            EnsureInitialised();
            RecoverActiveFileIfRequired();
            await RotateIfRequiredAsync(line.Length).ConfigureAwait(false);
            ContinueRetentionCleanup();
            await EnsureActiveStreamAsync().ConfigureAwait(false);
            activeFileRequiresRecovery = true;
            await activeStream!.WriteAsync(line, cancellationToken)
                .ConfigureAwait(false);
            await activeStream.FlushAsync(cancellationToken).ConfigureAwait(false);
            activeFileRequiresRecovery = false;
            RecordSuccess();
            return OperationalLogWriteResult.Written;
        }
        catch (OperationCanceledException) when (
            cancellationToken.IsCancellationRequested)
        {
            await CloseActiveStreamAsync().ConfigureAwait(false);
            return OperationalLogWriteResult.Cancelled;
        }
        catch (Exception)
        {
            await CloseActiveStreamAsync().ConfigureAwait(false);
            RecordFailure("LOG_SINK_WRITE_FAILED");
            return new OperationalLogWriteResult(
                OperationalLogWriteState.Failed,
                "LOG_SINK_WRITE_FAILED");
        }
        finally
        {
            writeGate.Release();
        }
    }

    public OperationalLogHealthSnapshot GetHealthSnapshot()
    {
        lock (healthGate)
        {
            return new OperationalLogHealthSnapshot(
                consecutiveFailureCount == 0
                    ? OperationalLogHealthState.Healthy
                    : OperationalLogHealthState.Degraded,
                totalFailureCount,
                consecutiveFailureCount,
                partialLineRecoveryCount,
                lastSignalCode,
                lastSignalTimestampUtc);
        }
    }

    public async ValueTask DisposeAsync()
    {
        await writeGate.WaitAsync().ConfigureAwait(false);

        try
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            await CloseActiveStreamAsync().ConfigureAwait(false);
            ResetRetentionCleanupPass(requestRetry: false);
            ownedDirectory.Dispose();
        }
        finally
        {
            writeGate.Release();
        }
    }

    private byte[] CreateBoundedLine(SanitisedOperationalLogEvent logEvent)
    {
        byte[] line = OperationalLogJsonSerialiser.Serialise(logEvent);

        if (line.Length <= policy.MaximumEventBytes)
        {
            return line;
        }

        SanitisedOperationalLogEvent minimal = logEvent with
        {
            Attributes = [],
            Message = OperationalLogSanitiser.NormaliseAndBound(
                logEvent.Message,
                Math.Min(logEvent.Message.Length, 128))
        };
        line = OperationalLogJsonSerialiser.Serialise(minimal);

        while (line.Length > policy.MaximumEventBytes &&
               minimal.Message.Length > 0)
        {
            minimal = minimal with
            {
                Message = minimal.Message[..(minimal.Message.Length / 2)]
            };
            line = OperationalLogJsonSerialiser.Serialise(minimal);
        }

        return line;
    }

    private void EnsureInitialised()
    {
        if (initialised)
        {
            return;
        }

        ownedDirectory.EnsureCreatedWithoutReparsePoints();
        RecoverPartialActiveFile();
        RequestRetentionCleanup();
        initialised = true;
    }

    private void RecoverActiveFileIfRequired()
    {
        if (!activeFileRequiresRecovery)
        {
            return;
        }

        RecoverPartialActiveFile();
        activeFileRequiresRecovery = false;
    }

    private void RecoverPartialActiveFile()
    {
        long length;
        int finalByte;

        using (FileStream? stream = ownedDirectory.TryOpenActiveFileForRead())
        {
            if (stream is null || stream.Length == 0)
            {
                return;
            }

            length = stream.Length;
            _ = stream.Seek(-1, SeekOrigin.End);
            finalByte = stream.ReadByte();
        }

        if (finalByte == '\n')
        {
            if (length >= policy.MaximumActiveFileBytes)
            {
                _ = RotateActiveFile(partial: false);
            }

            return;
        }

        if (RotateActiveFile(partial: true))
        {
            RecordPartialLineRecovery();
        }
    }

    private async Task RotateIfRequiredAsync(int nextLineLength)
    {
        if (activeStream is null)
        {
            using FileStream? stream =
                ownedDirectory.TryOpenActiveFileForRead();

            if (stream is null)
            {
                return;
            }

            long length = stream.Length;

            if (length == 0 ||
                length + nextLineLength <= policy.MaximumActiveFileBytes)
            {
                return;
            }
        }
        else if (activeStream.Length == 0 ||
                 activeStream.Length + nextLineLength <=
                    policy.MaximumActiveFileBytes)
        {
            return;
        }

        await CloseActiveStreamAsync().ConfigureAwait(false);
        _ = RotateActiveFile(partial: false);
    }

    private bool RotateActiveFile(bool partial)
    {
        string activePath = ownedDirectory.ActiveFilePath;
        FileStream? mutationStream = null;

        if (OperatingSystem.IsWindows())
        {
            mutationStream = ownedDirectory.TryOpenActiveFileForMutation();

            if (mutationStream is null)
            {
                return false;
            }
        }
        else
        {
            using FileStream? stream =
                ownedDirectory.TryOpenActiveFileForRead();

            if (stream is null)
            {
                return false;
            }
        }

        try
        {
            string extension = partial ? ".partial" : ".jsonl";

            for (int attempt = 0; attempt < 1000; attempt++)
            {
                long sequence = checked(++rotationSequence);
                string timestamp = timeProvider.GetUtcNow()
                    .ToString("yyyyMMdd'T'HHmmssfffffff'Z'", null);
                string destination = Path.Combine(
                    ownedDirectory.FullPath,
                    $"segment-{timestamp}-{sequence:D8}{extension}");

                if (mutationStream is not null)
                {
                    if (ownedDirectory.TryRenameActiveFile(
                            mutationStream,
                            destination))
                    {
                        RequestRetentionCleanup();
                        return true;
                    }
                }
                else if (!File.Exists(destination))
                {
                    File.Move(activePath, destination);
                    RequestRetentionCleanup();
                    return true;
                }
            }

            throw new IOException(
                "A collision-free operational log segment name could not be allocated.");
        }
        finally
        {
            mutationStream?.Dispose();
        }
    }

    private async Task EnsureActiveStreamAsync()
    {
        if (activeStream is not null)
        {
            return;
        }

        activeStream = ownedDirectory.OpenActiveFileForAppend();
        await Task.CompletedTask.ConfigureAwait(false);
    }

    private void RequestRetentionCleanup()
    {
        retentionCleanupRequested = true;
    }

    private void ContinueRetentionCleanup()
    {
        if (retentionCandidates is null)
        {
            if (!retentionCleanupRequested)
            {
                return;
            }

            retentionCleanupRequested = false;
            retentionCutoffUtc = (
                timeProvider.GetUtcNow() - policy.MaximumRetainedAge)
                .UtcDateTime;
            retentionSurvivors.Clear();
            retentionCandidates = Directory
                .EnumerateFiles(ownedDirectory.FullPath, "segment-*")
                .GetEnumerator();
        }

        try
        {
            for (int inspected = 0;
                 inspected < policy.MaximumCleanupFileCount;
                 inspected++)
            {
                if (!retentionCandidates.MoveNext())
                {
                    ResetRetentionCleanupPass(requestRetry: false);
                    return;
                }

                EvaluateRetentionCandidate(retentionCandidates.Current);
            }
        }
        catch
        {
            ResetRetentionCleanupPass(requestRetry: true);
            throw;
        }
    }

    private void EvaluateRetentionCandidate(string path)
    {
        if (!ownedDirectory.IsOwnedFile(path) ||
            OwnedOperationalLogDirectory.IsReparsePoint(path))
        {
            return;
        }

        FileInfo file = new(path);
        RetentionCandidate candidate = new(
            file.FullName,
            file.Name,
            file.LastWriteTimeUtc);

        if (candidate.LastWriteTimeUtc < retentionCutoffUtc)
        {
            ownedDirectory.DeleteOwnedSegmentIfPresent(candidate.FullName);
            return;
        }

        if (retentionSurvivors.Count < policy.MaximumRetainedFileCount)
        {
            retentionSurvivors.Add(candidate);
            return;
        }

        int oldestIndex = 0;

        for (int index = 1; index < retentionSurvivors.Count; index++)
        {
            if (CompareRetentionOrder(
                    retentionSurvivors[index],
                    retentionSurvivors[oldestIndex]) < 0)
            {
                oldestIndex = index;
            }
        }

        RetentionCandidate oldest = retentionSurvivors[oldestIndex];

        if (CompareRetentionOrder(candidate, oldest) > 0)
        {
            ownedDirectory.DeleteOwnedSegmentIfPresent(oldest.FullName);
            retentionSurvivors[oldestIndex] = candidate;
        }
        else
        {
            ownedDirectory.DeleteOwnedSegmentIfPresent(candidate.FullName);
        }
    }

    private void ResetRetentionCleanupPass(bool requestRetry)
    {
        retentionCandidates?.Dispose();
        retentionCandidates = null;
        retentionSurvivors.Clear();
        retentionCleanupRequested |= requestRetry;
    }

    private static int CompareRetentionOrder(
        RetentionCandidate left,
        RetentionCandidate right)
    {
        int timestampComparison = left.LastWriteTimeUtc.CompareTo(
            right.LastWriteTimeUtc);

        return timestampComparison != 0
            ? timestampComparison
            : StringComparer.Ordinal.Compare(left.Name, right.Name);
    }

    private async Task CloseActiveStreamAsync()
    {
        FileStream? stream = activeStream;
        activeStream = null;

        if (stream is null)
        {
            return;
        }

        try
        {
            await stream.FlushAsync().ConfigureAwait(false);
        }
        catch (Exception)
        {
            // A safe health signal is recorded by the caller.
        }

        try
        {
            await stream.DisposeAsync().ConfigureAwait(false);
        }
        catch (Exception)
        {
            // A logging cleanup failure must not escape into domain code.
        }
    }

    private void RecordSuccess()
    {
        lock (healthGate)
        {
            consecutiveFailureCount = 0;
        }
    }

    private void RecordFailure(string signalCode)
    {
        lock (healthGate)
        {
            totalFailureCount = totalFailureCount == long.MaxValue
                ? long.MaxValue
                : totalFailureCount + 1;
            consecutiveFailureCount = consecutiveFailureCount == int.MaxValue
                ? int.MaxValue
                : consecutiveFailureCount + 1;
            lastSignalCode = signalCode;
            lastSignalTimestampUtc = timeProvider.GetUtcNow();
        }
    }

    private void RecordPartialLineRecovery()
    {
        lock (healthGate)
        {
            partialLineRecoveryCount = partialLineRecoveryCount == long.MaxValue
                ? long.MaxValue
                : partialLineRecoveryCount + 1;
            lastSignalCode = "LOG_PARTIAL_LINE_RECOVERED";
            lastSignalTimestampUtc = timeProvider.GetUtcNow();
        }
    }

    private sealed record RetentionCandidate(
        string FullName,
        string Name,
        DateTime LastWriteTimeUtc);
}
