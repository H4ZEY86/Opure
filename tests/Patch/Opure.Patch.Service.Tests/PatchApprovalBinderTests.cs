using System;
using System.Security.Cryptography;
using System.Text;
using Opure.Patch.Contracts;
using Xunit;

namespace Opure.Patch.Service.Tests;

public sealed class PatchApprovalBinderTests
{
    private static readonly UTF8Encoding Utf8 = new(false, true);

    private static (ExactUtf8PatchProposal proposal, ExactUtf8PatchPreview preview) CreateValidPair()
    {
        var proposalContent = Utf8.GetBytes("Hello World\n");
        var proposalSha256 = Convert.ToHexStringLower(SHA256.HashData(proposalContent));
        
        var proposal = new ExactUtf8PatchProposal(
            "patch-123",
            1,
            "proj-1",
            "root-1",
            42,
            new string('0', 64),
            "file1.txt",
            ExactUtf8PatchOperationKind.Create,
            null,
            null,
            PatchLineEndingIntent.Lf,
            PatchCreatorKind.Developer,
            "feat: new file",
            DateTimeOffset.UtcNow,
            proposalContent);
            
        var preview = new ExactUtf8PatchPreview(
            "patch-123",
            1,
            proposal.ProposalSha256,
            "file1.txt",
            ExactUtf8PatchOperationKind.Create,
            null,
            proposal.ResultingContentSha256,
            PatchLineEndingIntent.PreserveExisting,
            PatchLineEndingIntent.Lf,
            false,
            false,
            PatchEffectIntentClass.Feature);

        return (proposal, preview);
    }

    [Fact]
    public void VerifyApproval_WithValidMatch_DoesNotThrow()
    {
        // Arrange
        var (proposal, preview) = CreateValidPair();
        
        var approval = new ExactUtf8PatchApproval(
            "app-1",
            1,
            "patch-123",
            proposal.ProposalSha256,
            preview.PreviewDigestSha256,
            "developer-id",
            DateTimeOffset.UtcNow);

        // Act & Assert
        // Should not throw
        PatchApprovalBinder.VerifyApproval(approval, preview, proposal, "developer-id");
    }

    [Fact]
    public void VerifyApproval_WithMismatchedPatchId_ThrowsException()
    {
        // Arrange
        var (proposal, preview) = CreateValidPair();
        
        var approval = new ExactUtf8PatchApproval(
            "app-1",
            1,
            "patch-FORGED", // Mismatch!
            proposal.ProposalSha256,
            preview.PreviewDigestSha256,
            "developer-id",
            DateTimeOffset.UtcNow);

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => 
            PatchApprovalBinder.VerifyApproval(approval, preview, proposal, "developer-id"));
        Assert.Contains("Patch ID mismatch across lifecycle bounds", ex.Message);
    }

    [Fact]
    public void VerifyApproval_WithMismatchedProposalSha256_ThrowsException()
    {
        // Arrange
        var (proposal, preview) = CreateValidPair();
        
        var approval = new ExactUtf8PatchApproval(
            "app-1",
            1,
            "patch-123",
            new string('f', 64), // Mismatch!
            preview.PreviewDigestSha256,
            "developer-id",
            DateTimeOffset.UtcNow);

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => 
            PatchApprovalBinder.VerifyApproval(approval, preview, proposal, "developer-id"));
        Assert.Contains("Cryptographic mismatch for Proposal SHA-256", ex.Message);
    }

    [Fact]
    public void VerifyApproval_WithMismatchedPreviewDigestSha256_ThrowsException()
    {
        // Arrange
        var (proposal, preview) = CreateValidPair();
        
        var approval = new ExactUtf8PatchApproval(
            "app-1",
            1,
            "patch-123",
            proposal.ProposalSha256,
            new string('e', 64), // Mismatch!
            "developer-id",
            DateTimeOffset.UtcNow);

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => 
            PatchApprovalBinder.VerifyApproval(approval, preview, proposal, "developer-id"));
        Assert.Contains("Cryptographic mismatch for Preview Digest SHA-256", ex.Message);
    }

    [Fact]
    public void VerifyApproval_WithUnauthorizedIdentity_ThrowsException()
    {
        // Arrange
        var (proposal, preview) = CreateValidPair();
        
        var approval = new ExactUtf8PatchApproval(
            "app-1",
            1,
            "patch-123",
            proposal.ProposalSha256,
            preview.PreviewDigestSha256,
            "malicious-actor", // Forged!
            DateTimeOffset.UtcNow);

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => 
            PatchApprovalBinder.VerifyApproval(approval, preview, proposal, "developer-id"));
        Assert.Contains("Unauthorized approver identity", ex.Message);
    }
}
