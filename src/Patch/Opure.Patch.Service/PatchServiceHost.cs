using Opure.Patch.Contracts;
using Opure.Patch.Sqlite;
using Opure.TrustEvidence.Contracts;
using System.Runtime.Versioning;

namespace Opure.Patch.Service;

[SupportedOSPlatform("windows")]
public sealed class PatchServiceHost : IDisposable
{
    private readonly PatchDatabase database;
    private readonly PatchTrustReceiptDispatchService trustReceiptDispatcher;
    private bool disposed;

    private PatchServiceHost(
        PatchDatabase database,
        ITrustEvidenceOwnerIngestionPort trustEvidenceIngestion)
    {
        this.database = database;
        trustReceiptDispatcher = new PatchTrustReceiptDispatchService(
            database,
            trustEvidenceIngestion);
        StateStore = database.CreateStateStore();
    }

    public IPatchStateStore StateStore { get; }

    public static PatchServiceHost Start(
        string channelDataRoot,
        ITrustEvidenceOwnerIngestionPort trustEvidenceIngestion,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(channelDataRoot);
        ArgumentNullException.ThrowIfNull(trustEvidenceIngestion);
        PatchDatabase database = PatchDatabase.Open(
            channelDataRoot,
            cancellationToken);
        try
        {
            return new PatchServiceHost(database, trustEvidenceIngestion);
        }
        catch
        {
            database.Dispose();
            throw;
        }
    }

    public PatchTrustReceiptDispatchReport DispatchPendingTrustReceipts(
        CancellationToken cancellationToken = default) =>
        trustReceiptDispatcher.DispatchPending(
            cancellationToken: cancellationToken);

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        database.Dispose();
    }
}
