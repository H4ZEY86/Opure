using System.Collections.Generic;
using System.Threading;
using Opure.TrustEvidence.Contracts;

namespace Opure.Configuration.Tests;

public sealed class TestEvidenceIngestionPort : ITrustEvidenceOwnerIngestionPort
{
    private readonly List<EvidenceIngestionRequest> requests = [];

    public string BoundOwnerServiceId => "opure.configuration";
    public IReadOnlyList<EvidenceIngestionRequest> Requests => requests;

    public EvidenceIngestionReceipt Ingest(EvidenceIngestionRequest request, CancellationToken cancellationToken = default)
    {
        requests.Add(request);
        return new EvidenceIngestionReceipt(
            Guid.NewGuid().ToString("N"),
            EvidenceIngestionDisposition.Applied,
            BoundOwnerServiceId,
            request.MessageId,
            request.Record.EvidenceId,
            request.DeclaredRecordSha256,
            "1",
            true,
            false,
            true,
            EvidenceIngestionCodes.Applied,
            "Ingested in test");
    }
}
