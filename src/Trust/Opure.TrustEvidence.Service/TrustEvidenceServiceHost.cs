using Opure.TrustEvidence.Contracts;
using Opure.TrustEvidence.Sqlite;

namespace Opure.TrustEvidence.Service;

/// <summary>
/// Owns the Trust Evidence database and creates owner-bound ingestion ports.
/// Runtime composes the binding; owner identity is not accepted from an
/// outbox payload or ordinary caller input.
/// </summary>
public sealed class TrustEvidenceServiceHost : IDisposable
{
    private readonly TrustEvidenceDatabase database;
    private readonly TrustEvidenceIngestionPipeline ingestion;
    private readonly EvidenceTypeCatalogue evidenceTypes;
    private readonly TimeProvider timeProvider;
    private bool disposed;

    private TrustEvidenceServiceHost(
        TrustEvidenceDatabase database,
        EvidenceTypeCatalogue evidenceTypes,
        TimeProvider timeProvider)
    {
        this.database = database;
        this.evidenceTypes = evidenceTypes;
        this.timeProvider = timeProvider;
        ingestion = database.CreateIngestionPipeline(
            evidenceTypes,
            timeProvider);
        QueryService = database.CreateQueryService(
            evidenceTypes,
            timeProvider);
    }

    public TrustEvidenceQueryService QueryService { get; }

    public static TrustEvidenceServiceHost Start(
        string channelDataRoot,
        TimeProvider? timeProvider = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(channelDataRoot);
        TimeProvider clock = timeProvider ?? TimeProvider.System;
        TrustEvidenceDatabase database = TrustEvidenceDatabase.Open(
            channelDataRoot,
            cancellationToken);

        try
        {
            return new TrustEvidenceServiceHost(
                database,
                FoundationEvidenceTypeCatalogue.Current,
                clock);
        }
        catch
        {
            database.Dispose();
            throw;
        }
    }

    public ITrustEvidenceOwnerIngestionPort BindOwner(
        string ownerServiceId)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerServiceId);

        if (!evidenceTypes.Definitions.Any(definition =>
                string.Equals(
                    definition.OwnerServiceId,
                    ownerServiceId,
                    StringComparison.Ordinal)))
        {
            throw new ArgumentException(
                "The requested owner has no registered Evidence Type.",
                nameof(ownerServiceId));
        }

        return new BoundOwnerIngestionPort(
            ownerServiceId,
            ingestion,
            timeProvider);
    }

    public TrustEvidenceDatabaseHealth InspectHealth(
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        return database.InspectHealth(cancellationToken);
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        database.Dispose();
    }

    private sealed class BoundOwnerIngestionPort :
        ITrustEvidenceOwnerIngestionPort
    {
        private static readonly TimeSpan SessionLifetime =
            TimeSpan.FromMinutes(1);

        private readonly TrustEvidenceIngestionPipeline ingestion;
        private readonly TimeProvider timeProvider;

        internal BoundOwnerIngestionPort(
            string ownerServiceId,
            TrustEvidenceIngestionPipeline ingestion,
            TimeProvider timeProvider)
        {
            BoundOwnerServiceId = ownerServiceId;
            this.ingestion = ingestion;
            this.timeProvider = timeProvider;
        }

        public string BoundOwnerServiceId { get; }

        public EvidenceIngestionReceipt Ingest(
            EvidenceIngestionRequest request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            DateTimeOffset now = timeProvider.GetUtcNow().ToUniversalTime();
            EvidenceOwnerSessionContext session = new(
                Guid.NewGuid().ToString("N"),
                BoundOwnerServiceId,
                EvidenceOwnerSessionAuthenticationState.Authenticated,
                now,
                now.Add(SessionLifetime));

            if (Environment.GetEnvironmentVariable("OPURE_TEST_CRASH_POINT") == "TrustEvidenceIngestion")
            {
                Environment.Exit(71);
            }

            return ingestion.Ingest(
                session,
                request,
                cancellationToken);
        }
    }
}
