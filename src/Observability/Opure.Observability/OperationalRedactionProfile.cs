namespace Opure.Observability;

public enum OperationalRedactionFailureAction
{
    DropUnsafeFieldsAndEmitWarning = 0
}

/// <summary>
/// Defines a reviewed, versioned redaction policy. Callers cannot construct a
/// weaker profile.
/// </summary>
public sealed class OperationalRedactionProfile
{
    private static readonly string[] LocalProhibitedAttributeNameParts =
    [
        "authorization",
        "authenticationheader",
        "cookie",
        "credential",
        "exceptiondata",
        "password",
        "payload",
        "privatekey",
        "prompt",
        "requestbody",
        "responsebody",
        "secret",
        "sourcecontent",
        "token"
    ];

    private static readonly string[] LocalProhibitedValueParts =
    [
        "api key",
        "api-key",
        "api_key",
        "apikey",
        "authorization:",
        "bearer ",
        "basic ",
        "client secret",
        "client-secret",
        "client_secret",
        "connection string",
        "connectionstring",
        "credential=",
        "ghp_",
        "github_pat_",
        "password",
        "password=",
        "private key",
        "secret=",
        "secret canary",
        "secret-canary",
        "secret_canary",
        "sessionsecret",
        "token="
    ];

    private OperationalRedactionProfile(
        string profileId,
        string absolutePathReplacement,
        int maximumDecodedValueBytes,
        OperationalRedactionFailureAction failureAction)
    {
        ProfileId = profileId;
        AbsolutePathReplacement = absolutePathReplacement;
        MaximumDecodedValueBytes = maximumDecodedValueBytes;
        FailureAction = failureAction;
        ProhibitedAttributeNameParts = LocalProhibitedAttributeNameParts;
        ProhibitedValueParts = LocalProhibitedValueParts;
        PercentEncodedSecretDetectionEnabled = true;
        Base64EncodedSecretDetectionEnabled = true;
    }

    public static OperationalRedactionProfile LocalDiagnostics { get; } = new(
        "opure.local-diagnostics-redaction/1",
        "path.absolute",
        maximumDecodedValueBytes: 4096,
        OperationalRedactionFailureAction.DropUnsafeFieldsAndEmitWarning);

    public string ProfileId { get; }

    public string AbsolutePathReplacement { get; }

    public int MaximumDecodedValueBytes { get; }

    public OperationalRedactionFailureAction FailureAction { get; }

    public bool PercentEncodedSecretDetectionEnabled { get; }

    public bool Base64EncodedSecretDetectionEnabled { get; }

    internal IReadOnlyList<string> ProhibitedAttributeNameParts { get; }

    internal IReadOnlyList<string> ProhibitedValueParts { get; }
}
