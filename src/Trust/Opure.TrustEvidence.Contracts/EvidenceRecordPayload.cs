using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Opure.TrustEvidence.Contracts;

public sealed class EvidenceRecordPayload
{
    public const int MaximumInlinePayloadBytes = 64 * 1024;
    public const int MaximumReferencedPayloadBytes = 256 * 1024 * 1024;

    private EvidenceRecordPayload(
        EvidencePayloadLocation location,
        EvidenceDataClassification classification,
        int payloadSizeBytes,
        string payloadSha256,
        string? inlineCanonicalJson,
        string? reference)
    {
        Location = location;
        Classification = classification;
        PayloadSizeBytes = payloadSizeBytes;
        PayloadSha256 = payloadSha256;
        InlineCanonicalJson = inlineCanonicalJson;
        Reference = reference;
    }

    public EvidencePayloadLocation Location { get; }

    public EvidenceDataClassification Classification { get; }

    public int PayloadSizeBytes { get; }

    public string PayloadSha256 { get; }

    public string? InlineCanonicalJson { get; }

    public string? Reference { get; }

    public static EvidenceRecordPayload CreateInline(
        string json,
        EvidenceDataClassification classification)
    {
        ValidateClassification(classification);
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        int originalSize = Encoding.UTF8.GetByteCount(json);

        if (originalSize > MaximumInlinePayloadBytes)
        {
            throw new ArgumentOutOfRangeException(
                nameof(json),
                originalSize,
                "An inline Trust Evidence payload cannot exceed 64 KiB.");
        }

        string canonicalJson;

        try
        {
            canonicalJson = EvidenceJsonCanonicaliser.Canonicalise(json);
        }
        catch (JsonException exception)
        {
            throw new ArgumentException(
                "An inline Trust Evidence payload must be valid bounded JSON.",
                nameof(json),
                exception);
        }

        byte[] canonicalBytes = Encoding.UTF8.GetBytes(canonicalJson);

        if (canonicalBytes.Length > MaximumInlinePayloadBytes)
        {
            throw new ArgumentOutOfRangeException(
                nameof(json),
                canonicalBytes.Length,
                "A canonical inline Trust Evidence payload cannot exceed 64 KiB.");
        }

        return new EvidenceRecordPayload(
            EvidencePayloadLocation.Inline,
            classification,
            canonicalBytes.Length,
            Convert.ToHexStringLower(SHA256.HashData(canonicalBytes)),
            canonicalJson,
            reference: null);
    }

    public static EvidenceRecordPayload CreateOwnerReference(
        string ownerPayloadReference,
        string payloadSha256,
        int payloadSizeBytes,
        EvidenceDataClassification classification)
    {
        ValidateClassification(classification);
        EvidenceRecordContract.ValidateOpaqueIdentifier(
            ownerPayloadReference,
            nameof(ownerPayloadReference));
        ValidateReferencedPayload(
            payloadSha256,
            payloadSizeBytes,
            nameof(payloadSizeBytes));

        return new EvidenceRecordPayload(
            EvidencePayloadLocation.OwnerReference,
            classification,
            payloadSizeBytes,
            payloadSha256,
            inlineCanonicalJson: null,
            ownerPayloadReference);
    }

    public static EvidenceRecordPayload CreateContentAddressedReference(
        string payloadSha256,
        int payloadSizeBytes,
        EvidenceDataClassification classification)
    {
        ValidateClassification(classification);
        ValidateReferencedPayload(
            payloadSha256,
            payloadSizeBytes,
            nameof(payloadSizeBytes));

        return new EvidenceRecordPayload(
            EvidencePayloadLocation.TrustEvidenceContentAddressedStore,
            classification,
            payloadSizeBytes,
            payloadSha256,
            inlineCanonicalJson: null,
            string.Concat("sha256:", payloadSha256));
    }

    private static void ValidateClassification(
        EvidenceDataClassification classification)
    {
        if (!Enum.IsDefined(classification))
        {
            throw new ArgumentOutOfRangeException(nameof(classification));
        }

        if (classification is EvidenceDataClassification.Secret or
            EvidenceDataClassification.Prohibited)
        {
            throw new ArgumentException(
                "Secret and prohibited payloads cannot become Trust Evidence records.",
                nameof(classification));
        }
    }

    private static void ValidateReferencedPayload(
        string payloadSha256,
        int payloadSizeBytes,
        string sizeParameterName)
    {
        EvidenceTypeContract.ValidateSha256(
            payloadSha256,
            nameof(payloadSha256));

        if (payloadSizeBytes is < 1 or > MaximumReferencedPayloadBytes)
        {
            throw new ArgumentOutOfRangeException(
                sizeParameterName,
                payloadSizeBytes,
                "A referenced Trust Evidence payload must be between 1 byte and 256 MiB.");
        }
    }
}
