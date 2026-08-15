using System.Text;
using Opure.Patch.Contracts;
using Opure.Persistence.Sqlite;
using Opure.Patch.Sqlite;
using Opure.TrustEvidence.Contracts;
using Opure.TrustEvidence.Service;
using Xunit;

namespace Opure.Patch.Service.Tests;

public sealed class PatchTrustReceiptDispatchServiceTests : IDisposable
{
    private const string ProjectId = "project-000000001";
    private const string BaseHash = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
    private static readonly DateTimeOffset TestInstant =
        new(2026, 8, 13, 12, 0, 0, TimeSpan.Zero);

    private readonly string testRoot = Path.Combine(
        Path.GetTempPath(),
        "Opure.PatchTrustReceiptDispatchService.Tests",
        Guid.NewGuid().ToString("N"));

    public PatchTrustReceiptDispatchServiceTests()
    {
        Directory.CreateDirectory(testRoot);
    }

    [Fact]
    public void PendingReceiptResumesAfterRestartAndDuplicateIsIdempotent()
    {
        ManualTimeProvider timeProvider = new(TestInstant);
        string databasePath;
        ExactUtf8PatchProposal proposal = CreateProposal("patch-0000000001", "content one");

        using (PatchDatabase database = PatchDatabase.Open(
                   ChannelRoot,
                   TestContext.Current.CancellationToken))
        {
            _ = database.CreateStateStore(timeProvider).Register(
                proposal,
                "command-register-001",
                TestContext.Current.CancellationToken);
            
            databasePath = database.Descriptor.DatabasePath;
            PatchTrustReceiptDispatchService unavailable = new(
                database,
                new UnavailableIngestionPort(),
                timeProvider);

            SqliteOutboxBacklogHealth preDispatch = unavailable.ReadBacklog(TestContext.Current.CancellationToken);
            Assert.Equal(1, preDispatch.PendingCount); // Debug!

            PatchTrustReceiptDispatchReport failed = unavailable.DispatchPending(
                cancellationToken: TestContext.Current.CancellationToken);

            Assert.Equal(1, failed.RetryScheduled);
            Assert.Equal(1, failed.Backlog.UndeliveredCount);
        }

        timeProvider.Advance(TimeSpan.FromSeconds(1));
        using TrustEvidenceServiceHost trustHost = TrustEvidenceServiceHost.Start(
            ChannelRoot,
            timeProvider,
            TestContext.Current.CancellationToken);
        RecordingIngestionPort ingestion = new(
            trustHost.BindOwner(PatchDatabase.OwnerServiceId));
        using PatchDatabase reopened = PatchDatabase.Open(
            ChannelRoot,
            TestContext.Current.CancellationToken);
        Assert.Equal(databasePath, reopened.Descriptor.DatabasePath);
        PatchTrustReceiptDispatchService resumed = new(
            reopened,
            ingestion,
            timeProvider);

        PatchTrustReceiptDispatchReport delivered = resumed.DispatchPending(
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(1, delivered.Delivered);
        Assert.Equal(0, delivered.Backlog.UndeliveredCount);
        EvidenceIngestionRequest request = Assert.IsType<EvidenceIngestionRequest>(
            ingestion.Request);
        EvidenceIngestionReceipt duplicate = ingestion.Replay(request);
        Assert.Equal(EvidenceIngestionDisposition.Duplicate, duplicate.Disposition);
        Assert.False(duplicate.DomainEffectApplied);
        Assert.Equal(request.Record.EvidenceId, duplicate.EvidenceId);
        Assert.Equal(request.Record.RecordSha256, duplicate.RecordSha256);
    }

    public void Dispose()
    {
        if (Directory.Exists(testRoot))
        {
            Directory.Delete(testRoot, recursive: true);
        }

        GC.SuppressFinalize(this);
    }

    private string ChannelRoot => Path.Combine(testRoot, "channel");

    private static ExactUtf8PatchProposal CreateProposal(string patchId, string content) =>
        new(
            patchId,
            ExactUtf8PatchProposal.CurrentContractRevision,
            ProjectId,
            "root-000000000001",
            1,
            BaseHash,
            "path-001",
            ExactUtf8PatchOperationKind.Create,
            null,
            null,
            PatchLineEndingIntent.ProjectConvention,
            PatchCreatorKind.Developer,
            "Create one exact UTF-8 file.",
            new DateTimeOffset(2026, 8, 13, 12, 0, 0, TimeSpan.Zero),
            Encoding.UTF8.GetBytes(content));

    private sealed class RecordingIngestionPort(
        ITrustEvidenceOwnerIngestionPort inner) : ITrustEvidenceOwnerIngestionPort
    {
        public string BoundOwnerServiceId => inner.BoundOwnerServiceId;

        public EvidenceIngestionRequest? Request { get; private set; }

        public EvidenceIngestionReceipt Ingest(
            EvidenceIngestionRequest request,
            CancellationToken cancellationToken = default)
        {
            Request = request;
            return inner.Ingest(request, cancellationToken);
        }

        public EvidenceIngestionReceipt Replay(EvidenceIngestionRequest request)
        {
            return inner.Ingest(request, TestContext.Current.CancellationToken);
        }
    }

    private sealed class UnavailableIngestionPort : ITrustEvidenceOwnerIngestionPort
    {
        public string BoundOwnerServiceId => PatchDatabase.OwnerServiceId;

        public EvidenceIngestionReceipt Ingest(
            EvidenceIngestionRequest request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            cancellationToken.ThrowIfCancellationRequested();
            throw new InvalidOperationException("The test Trust service is unavailable.");
        }
    }

    private sealed class ManualTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        private DateTimeOffset current = utcNow;

        public override DateTimeOffset GetUtcNow()
        {
            return current;
        }

        internal void Advance(TimeSpan amount)
        {
            current = current.Add(amount);
        }
    }
}
