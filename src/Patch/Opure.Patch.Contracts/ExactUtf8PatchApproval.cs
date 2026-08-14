using System.Security.Cryptography;
using System.Text;

namespace Opure.Patch.Contracts;

public sealed class ExactUtf8PatchApproval
{
    public const string ContractSchema = "opure.patch.exact-utf8.approval/1";
    public const int CurrentContractRevision = 1;
    private const int Sha256HexLength = 64;
    private static readonly UTF8Encoding StrictUtf8WithoutBom =
        new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    public ExactUtf8PatchApproval(
        string approvalId,
        int contractRevision,
        string patchId,
        string proposalSha256,
        string previewDigestSha256,
        string approverIdentity,
        DateTimeOffset approvedAtUtc)
    {
        ValidateOpaqueIdentifier(approvalId, nameof(approvalId));
        ArgumentOutOfRangeException.ThrowIfNotEqual(
            contractRevision,
            CurrentContractRevision,
            nameof(contractRevision));
        ValidateOpaqueIdentifier(patchId, nameof(patchId));
        ValidateSha256(proposalSha256, nameof(proposalSha256));
        ValidateSha256(previewDigestSha256, nameof(previewDigestSha256));
        ValidateApproverIdentity(approverIdentity);
        ArgumentOutOfRangeException.ThrowIfEqual(approvedAtUtc, default, nameof(approvedAtUtc));

        ApprovalId = approvalId;
        ContractRevision = contractRevision;
        PatchId = patchId;
        ProposalSha256 = proposalSha256;
        PreviewDigestSha256 = previewDigestSha256;
        ApproverIdentity = approverIdentity;
        ApprovedAtUtc = approvedAtUtc.ToUniversalTime();
        ApprovalSha256 = ComputeApprovalSha256();
    }

    public static string Schema => ContractSchema;
    public string ApprovalId { get; }
    public int ContractRevision { get; }
    public string PatchId { get; }
    public string ProposalSha256 { get; }
    public string PreviewDigestSha256 { get; }
    public string ApproverIdentity { get; }
    public DateTimeOffset ApprovedAtUtc { get; }
    public string ApprovalSha256 { get; }

    private string ComputeApprovalSha256()
    {
        using IncrementalHash hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        Append(hash, ContractSchema);
        Append(hash, ContractRevision.ToString(System.Globalization.CultureInfo.InvariantCulture));
        Append(hash, ApprovalId);
        Append(hash, PatchId);
        Append(hash, ProposalSha256);
        Append(hash, PreviewDigestSha256);
        Append(hash, ApproverIdentity);
        Append(hash, ApprovedAtUtc.ToString("O", System.Globalization.CultureInfo.InvariantCulture));
        return Convert.ToHexStringLower(hash.GetHashAndReset());
    }

    private static void Append(IncrementalHash hash, string value)
    {
        byte[] bytes = StrictUtf8WithoutBom.GetBytes(value);
        Span<byte> length = stackalloc byte[4];
        System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(length, bytes.Length);
        hash.AppendData(length);
        hash.AppendData(bytes);
        CryptographicOperations.ZeroMemory(bytes);
    }

    private static void ValidateOpaqueIdentifier(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        if (value.Length > 128 || value.Any(character =>
                !char.IsAsciiLetterOrDigit(character) && character is not '-' and not '_' and not '.'))
        {
            throw new ArgumentException(
                "The value must be a bounded opaque identifier.", parameterName);
        }
    }

    private static void ValidateSha256(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        if (value.Length != Sha256HexLength || value.Any(character => !char.IsAsciiHexDigit(character)))
        {
            throw new ArgumentException(
                "The value must be a 64-character hexadecimal SHA-256.", parameterName);
        }
    }

    private static void ValidateApproverIdentity(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(value));
        if (value.Length > 256 || value.Any(char.IsControl))
        {
            throw new ArgumentException(
                "The approver identity must be bounded printable text.", nameof(value));
        }
    }
}
