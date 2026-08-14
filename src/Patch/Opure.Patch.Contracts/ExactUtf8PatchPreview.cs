using System.Security.Cryptography;
using System.Text;

namespace Opure.Patch.Contracts;

public enum PatchEffectIntentClass
{
    Unknown = 0,
    Refactoring = 1,
    Feature = 2,
    BugFix = 3,
    Documentation = 4,
    Configuration = 5,
    Deletion = 6
}

public sealed class ExactUtf8PatchPreview
{
    public const string ContractSchema = "opure.patch.exact-utf8.preview/1";
    public const int CurrentContractRevision = 1;
    private const int Sha256HexLength = 64;
    private static readonly UTF8Encoding StrictUtf8WithoutBom =
        new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    public ExactUtf8PatchPreview(
        string patchId,
        int contractRevision,
        string proposalSha256,
        string targetPathReferenceId,
        ExactUtf8PatchOperationKind operationKind,
        string? beforeHashSha256,
        string afterHashSha256,
        PatchLineEndingIntent sourceLineEnding,
        PatchLineEndingIntent resultingLineEnding,
        bool hasHiddenOrBidiControls,
        bool isTruncated,
        PatchEffectIntentClass effectIntentClass)
    {
        ValidateOpaqueIdentifier(patchId, nameof(patchId));
        ArgumentOutOfRangeException.ThrowIfNotEqual(
            contractRevision,
            CurrentContractRevision,
            nameof(contractRevision));
        ValidateSha256(proposalSha256, nameof(proposalSha256));
        ValidateOpaqueIdentifier(targetPathReferenceId, nameof(targetPathReferenceId));
        
        if (!Enum.IsDefined(operationKind))
        {
            throw new ArgumentOutOfRangeException(nameof(operationKind));
        }

        if (operationKind == ExactUtf8PatchOperationKind.Create)
        {
            if (beforeHashSha256 is not null)
            {
                throw new ArgumentException(
                    "Create operation previews cannot have a before hash.",
                    nameof(beforeHashSha256));
            }
        }
        else
        {
            if (beforeHashSha256 is null)
            {
                throw new ArgumentException(
                    "Replace operation previews require a before hash.",
                    nameof(beforeHashSha256));
            }
            ValidateSha256(beforeHashSha256, nameof(beforeHashSha256));
        }

        ValidateSha256(afterHashSha256, nameof(afterHashSha256));

        if (!Enum.IsDefined(sourceLineEnding))
        {
            throw new ArgumentOutOfRangeException(nameof(sourceLineEnding));
        }
        if (!Enum.IsDefined(resultingLineEnding))
        {
            throw new ArgumentOutOfRangeException(nameof(resultingLineEnding));
        }
        if (!Enum.IsDefined(effectIntentClass))
        {
            throw new ArgumentOutOfRangeException(nameof(effectIntentClass));
        }

        PatchId = patchId;
        ContractRevision = contractRevision;
        ProposalSha256 = proposalSha256;
        TargetPathReferenceId = targetPathReferenceId;
        OperationKind = operationKind;
        BeforeHashSha256 = beforeHashSha256;
        AfterHashSha256 = afterHashSha256;
        SourceLineEnding = sourceLineEnding;
        ResultingLineEnding = resultingLineEnding;
        HasHiddenOrBidiControls = hasHiddenOrBidiControls;
        IsTruncated = isTruncated;
        EffectIntentClass = effectIntentClass;
        PreviewDigestSha256 = ComputePreviewDigestSha256();
    }

    public static string Schema => ContractSchema;
    public string PatchId { get; }
    public int ContractRevision { get; }
    public string ProposalSha256 { get; }
    public string TargetPathReferenceId { get; }
    public ExactUtf8PatchOperationKind OperationKind { get; }
    public string? BeforeHashSha256 { get; }
    public string AfterHashSha256 { get; }
    public PatchLineEndingIntent SourceLineEnding { get; }
    public PatchLineEndingIntent ResultingLineEnding { get; }
    public bool HasHiddenOrBidiControls { get; }
    public bool IsTruncated { get; }
    public PatchEffectIntentClass EffectIntentClass { get; }
    public string PreviewDigestSha256 { get; }

    private string ComputePreviewDigestSha256()
    {
        using IncrementalHash hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        Append(hash, ContractSchema);
        Append(hash, ContractRevision.ToString(System.Globalization.CultureInfo.InvariantCulture));
        Append(hash, PatchId);
        Append(hash, ProposalSha256);
        Append(hash, TargetPathReferenceId);
        Append(hash, OperationKind.ToString());
        Append(hash, BeforeHashSha256 ?? string.Empty);
        Append(hash, AfterHashSha256);
        Append(hash, SourceLineEnding.ToString());
        Append(hash, ResultingLineEnding.ToString());
        Append(hash, HasHiddenOrBidiControls.ToString());
        Append(hash, IsTruncated.ToString());
        Append(hash, EffectIntentClass.ToString());
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
}
