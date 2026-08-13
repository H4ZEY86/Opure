using System.Security.Cryptography;
using System.Text;

namespace Opure.Patch.Contracts;

public enum ExactUtf8PatchOperationKind
{
    Create = 0,
    Replace = 1
}

public enum PatchLineEndingIntent
{
    PreserveExisting = 0,
    ProjectConvention = 1,
    Lf = 2,
    CrLf = 3
}

public enum PatchCreatorKind
{
    Developer = 0,
    DeterministicService = 1
}

public sealed class ExactUtf8PatchProposal
{
    public const string ContractSchema = "opure.patch.exact-utf8/1";
    public const int CurrentContractRevision = 1;
    public const int MaximumContentBytes = 4 * 1024 * 1024;
    private const int Sha256HexLength = 64;
    private static readonly UTF8Encoding StrictUtf8WithoutBom =
        new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    private readonly byte[] contentUtf8;

    public ExactUtf8PatchProposal(
        string patchId,
        int contractRevision,
        string projectId,
        string rootReferenceId,
        long baseWorkspaceGeneration,
        string baseWorkspaceGenerationSha256,
        string targetPathReferenceId,
        ExactUtf8PatchOperationKind operationKind,
        string? expectedSourceSha256,
        long? expectedSourceSizeBytes,
        PatchLineEndingIntent lineEndingIntent,
        PatchCreatorKind creatorKind,
        string intentSummary,
        DateTimeOffset createdAtUtc,
        ReadOnlySpan<byte> contentUtf8)
    {
        ValidateOpaqueIdentifier(patchId, nameof(patchId));
        ArgumentOutOfRangeException.ThrowIfNotEqual(
            contractRevision,
            CurrentContractRevision,
            nameof(contractRevision));
        ValidateOpaqueIdentifier(projectId, nameof(projectId));
        ValidateOpaqueIdentifier(rootReferenceId, nameof(rootReferenceId));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(
            baseWorkspaceGeneration,
            nameof(baseWorkspaceGeneration));
        ValidateSha256(baseWorkspaceGenerationSha256, nameof(baseWorkspaceGenerationSha256));
        ValidateOpaqueIdentifier(targetPathReferenceId, nameof(targetPathReferenceId));

        if (!Enum.IsDefined(operationKind))
        {
            throw new ArgumentOutOfRangeException(nameof(operationKind));
        }
        if (!Enum.IsDefined(lineEndingIntent))
        {
            throw new ArgumentOutOfRangeException(nameof(lineEndingIntent));
        }
        if (!Enum.IsDefined(creatorKind) || creatorKind is not PatchCreatorKind.Developer and
            not PatchCreatorKind.DeterministicService)
        {
            throw new ArgumentOutOfRangeException(
                nameof(creatorKind),
                creatorKind,
                "CM-001 permits only developer or deterministic-service creators.");
        }

        ValidateSourcePrecondition(operationKind, expectedSourceSha256, expectedSourceSizeBytes);
        ValidateIntentSummary(intentSummary);
        ArgumentOutOfRangeException.ThrowIfEqual(createdAtUtc, default, nameof(createdAtUtc));
        this.contentUtf8 = ValidateAndCopyContent(contentUtf8);

        PatchId = patchId;
        ContractRevision = contractRevision;
        ProjectId = projectId;
        RootReferenceId = rootReferenceId;
        BaseWorkspaceGeneration = baseWorkspaceGeneration;
        BaseWorkspaceGenerationSha256 = baseWorkspaceGenerationSha256;
        TargetPathReferenceId = targetPathReferenceId;
        OperationKind = operationKind;
        ExpectedSourceSha256 = expectedSourceSha256;
        ExpectedSourceSizeBytes = expectedSourceSizeBytes;
        LineEndingIntent = lineEndingIntent;
        CreatorKind = creatorKind;
        IntentSummary = intentSummary;
        CreatedAtUtc = createdAtUtc.ToUniversalTime();
        ResultingContentSha256 = Convert.ToHexStringLower(SHA256.HashData(this.contentUtf8));
    }

    public static string Schema => ContractSchema;
    public string PatchId { get; }
    public int ContractRevision { get; }
    public string ProjectId { get; }
    public string RootReferenceId { get; }
    public long BaseWorkspaceGeneration { get; }
    public string BaseWorkspaceGenerationSha256 { get; }
    public string TargetPathReferenceId { get; }
    public ExactUtf8PatchOperationKind OperationKind { get; }
    public string? ExpectedSourceSha256 { get; }
    public long? ExpectedSourceSizeBytes { get; }
    public PatchLineEndingIntent LineEndingIntent { get; }
    public PatchCreatorKind CreatorKind { get; }
    public string IntentSummary { get; }
    public DateTimeOffset CreatedAtUtc { get; }
    public string ResultingContentSha256 { get; }
    public int ContentByteCount => contentUtf8.Length;
    public ReadOnlyMemory<byte> ContentUtf8 => contentUtf8;

    private static byte[] ValidateAndCopyContent(ReadOnlySpan<byte> content)
    {
        if (content.Length > MaximumContentBytes)
        {
            throw new ArgumentOutOfRangeException(
                nameof(content), content.Length, "Exact UTF-8 patch content exceeds the byte limit.");
        }
        if (content.Length >= 3 &&
            content[0] == 0xef &&
            content[1] == 0xbb &&
            content[2] == 0xbf)
        {
            throw new ArgumentException(
                "Exact UTF-8 patch content must not include a byte-order mark.", nameof(content));
        }
        if (content.Contains((byte)0))
        {
            throw new ArgumentException(
                "Exact UTF-8 text content must not contain NUL bytes.", nameof(content));
        }

        try
        {
            _ = StrictUtf8WithoutBom.GetCharCount(content);
        }
        catch (DecoderFallbackException exception)
        {
            throw new ArgumentException(
                "Exact UTF-8 patch content contains an invalid byte sequence.",
                nameof(content), exception);
        }
        return content.ToArray();
    }

    private static void ValidateSourcePrecondition(
        ExactUtf8PatchOperationKind operationKind,
        string? expectedSourceSha256,
        long? expectedSourceSizeBytes)
    {
        if (operationKind == ExactUtf8PatchOperationKind.Create)
        {
            if (expectedSourceSha256 is not null || expectedSourceSizeBytes is not null)
            {
                throw new ArgumentException(
                    "Create proposals cannot carry a source hash or size.",
                    nameof(expectedSourceSha256));
            }
            return;
        }
        if (expectedSourceSha256 is null || expectedSourceSizeBytes is null)
        {
            throw new ArgumentException(
                "Replace proposals require an exact source SHA-256 and size.",
                nameof(expectedSourceSha256));
        }
        ValidateSha256(expectedSourceSha256, nameof(expectedSourceSha256));
        ArgumentOutOfRangeException.ThrowIfNegative(
            expectedSourceSizeBytes.Value, nameof(expectedSourceSizeBytes));
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

    private static void ValidateIntentSummary(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(value));
        if (value.Length > 256 || value.Any(char.IsControl))
        {
            throw new ArgumentException(
                "The intent summary must be bounded printable text.", nameof(value));
        }
    }
}
