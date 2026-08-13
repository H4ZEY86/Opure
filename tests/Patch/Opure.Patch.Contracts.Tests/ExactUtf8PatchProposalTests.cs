using System.Text;
using Opure.Patch.Contracts;
using Xunit;

namespace Opure.Patch.Contracts.Tests;

public sealed class ExactUtf8PatchProposalTests
{
    private const string HashA = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
    private const string HashB = "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";

    [Fact]
    public void Create_binds_exact_utf8_bytes_and_absent_source()
    {
        byte[] content = Encoding.UTF8.GetBytes("Hello, developer.\r\n");
        ExactUtf8PatchProposal proposal = Create(
            ExactUtf8PatchOperationKind.Create, null, null, content);

        Assert.Equal(ExactUtf8PatchProposal.CurrentContractRevision, proposal.ContractRevision);
        Assert.Equal(ExactUtf8PatchProposal.ContractSchema, ExactUtf8PatchProposal.Schema);
        Assert.Equal(content, proposal.ContentUtf8.ToArray());
        Assert.Equal(
            "16ca6563baf63c751fab7137c29395101f6131e568c6a910979addff713ac341",
            proposal.ResultingContentSha256);
        Assert.Null(proposal.ExpectedSourceSha256);
        content[0] = (byte)'X';
        Assert.Equal((byte)'H', proposal.ContentUtf8.Span[0]);
    }

    [Fact]
    public void Replace_requires_exact_source_hash_and_size()
    {
        ExactUtf8PatchProposal proposal = Create(
            ExactUtf8PatchOperationKind.Replace,
            HashB,
            42,
            Encoding.UTF8.GetBytes("replacement\n"));

        Assert.Equal(HashB, proposal.ExpectedSourceSha256);
        Assert.Equal(42, proposal.ExpectedSourceSizeBytes);
        Assert.Equal(PatchLineEndingIntent.PreserveExisting, proposal.LineEndingIntent);
    }

    [Theory]
    [InlineData(ExactUtf8PatchOperationKind.Create, HashB, 0L)]
    [InlineData(ExactUtf8PatchOperationKind.Replace, null, 0L)]
    [InlineData(ExactUtf8PatchOperationKind.Replace, HashB, null)]
    public void Operation_source_preconditions_cannot_be_ambiguous(
        ExactUtf8PatchOperationKind operation,
        string? sourceHash,
        long? sourceSize)
    {
        Assert.Throws<ArgumentException>(() => Create(
            operation, sourceHash, sourceSize, Encoding.UTF8.GetBytes("content")));
    }

    [Theory]
    [InlineData("../escape")]
    [InlineData("C:\\absolute")]
    [InlineData("path:stream")]
    [InlineData("contains/slash")]
    public void Target_is_an_opaque_path_reference_not_a_path(string targetReference)
    {
        Assert.Throws<ArgumentException>(() => Create(
            ExactUtf8PatchOperationKind.Create,
            null,
            null,
            Encoding.UTF8.GetBytes("content"),
            targetReference));
    }

    [Fact]
    public void Invalid_utf8_bom_nul_and_oversize_are_rejected()
    {
        Assert.Throws<ArgumentException>(() => Create(
            ExactUtf8PatchOperationKind.Create, null, null, [0xc3, 0x28]));
        Assert.Throws<ArgumentException>(() => Create(
            ExactUtf8PatchOperationKind.Create, null, null, [0xef, 0xbb, 0xbf, 0x61]));
        Assert.Throws<ArgumentException>(() => Create(
            ExactUtf8PatchOperationKind.Create, null, null, [0x61, 0x00, 0x62]));
        byte[] oversized = new byte[ExactUtf8PatchProposal.MaximumContentBytes + 1];
        Array.Fill(oversized, (byte)'a');
        Assert.Throws<ArgumentOutOfRangeException>(() => Create(
            ExactUtf8PatchOperationKind.Create, null, null, oversized));
    }

    [Fact]
    public void Unknown_creator_revision_and_hash_are_rejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Construct(
            contractRevision: 1,
            baseHash: HashA,
            creatorKind: (PatchCreatorKind)99));
        Assert.Throws<ArgumentOutOfRangeException>(() => Construct(
            contractRevision: 2,
            baseHash: HashA,
            creatorKind: PatchCreatorKind.Developer));
        Assert.Throws<ArgumentException>(() => Construct(
            contractRevision: 1,
            baseHash: "not-a-hash",
            creatorKind: PatchCreatorKind.Developer));
    }

    private static ExactUtf8PatchProposal Construct(
        int contractRevision,
        string baseHash,
        PatchCreatorKind creatorKind) =>
        new(
            "patch-001", contractRevision, "project-001", "root-001", 7, baseHash,
            "path-001", ExactUtf8PatchOperationKind.Create, null, null,
            PatchLineEndingIntent.ProjectConvention, creatorKind,
            "Create a deterministic text file.",
            new DateTimeOffset(2026, 8, 13, 12, 0, 0, TimeSpan.Zero),
            Encoding.UTF8.GetBytes("content"));

    private static ExactUtf8PatchProposal Create(
        ExactUtf8PatchOperationKind operation,
        string? expectedSourceSha256,
        long? expectedSourceSizeBytes,
        byte[] content,
        string targetPathReferenceId = "path-001") =>
        new(
            "patch-001", 1, "project-001", "root-001", 7, HashA,
            targetPathReferenceId, operation, expectedSourceSha256, expectedSourceSizeBytes,
            operation == ExactUtf8PatchOperationKind.Create
                ? PatchLineEndingIntent.ProjectConvention
                : PatchLineEndingIntent.PreserveExisting,
            PatchCreatorKind.Developer,
            "Create or replace one deterministic UTF-8 text file.",
            new DateTimeOffset(2026, 8, 13, 12, 0, 0, TimeSpan.Zero),
            content);
}
