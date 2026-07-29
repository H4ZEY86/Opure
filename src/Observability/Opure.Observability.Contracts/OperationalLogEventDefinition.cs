using System.Collections.ObjectModel;

namespace Opure.Observability.Contracts;

public enum OperationalLogAttributeClassification
{
    Safe = 0,
    Pseudonymous = 1,
    Sensitive = 2,
    Secret = 3,
    Prohibited = 4
}
public sealed class OperationalLogAttributeDefinition
{
    public OperationalLogAttributeDefinition(
        string name,
        OperationalLogAttributeKind kind,
        OperationalLogAttributeClassification classification)
    {
        OperationalLogContract.ValidateAttributeName(name);

        if (!Enum.IsDefined(kind))
        {
            throw new ArgumentOutOfRangeException(nameof(kind));
        }

        if (!Enum.IsDefined(classification))
        {
            throw new ArgumentOutOfRangeException(nameof(classification));
        }

        if (classification is OperationalLogAttributeClassification.Secret or
            OperationalLogAttributeClassification.Prohibited)
        {
            throw new ArgumentException(
                "Secret and prohibited fields cannot be operational log attributes.",
                nameof(classification));
        }

        Name = name;
        Kind = kind;
        Classification = classification;
    }

    public string Name { get; }

    public OperationalLogAttributeKind Kind { get; }

    public OperationalLogAttributeClassification Classification { get; }
}

public sealed class OperationalLogEventDefinition
{
    private const int MaximumMessageCharacters = 256;

    private static readonly string[] ProhibitedMessageParts =
    [
        "api key",
        "authorization",
        "bearer ",
        "client secret",
        "connection string",
        "cookie",
        "credential",
        "namespace ",
        "password",
        "private key",
        "private ",
        "prompt",
        "protected ",
        "public ",
        "secret",
        "token",
        "using "
    ];

    private readonly IReadOnlyDictionary<string, OperationalLogAttributeDefinition>
        allowedAttributes;

    internal OperationalLogEventDefinition(
        string eventName,
        OperationalLogSeverity severity,
        string message)
        : this(eventName, severity, message, [])
    {
    }

    internal OperationalLogEventDefinition(
        string eventName,
        OperationalLogSeverity severity,
        string message,
        IEnumerable<OperationalLogAttributeDefinition> allowedAttributes)
    {
        OperationalLogContract.ValidateStableName(eventName, nameof(eventName));
        ValidateMessage(message);
        ArgumentNullException.ThrowIfNull(allowedAttributes);

        if (!Enum.IsDefined(severity))
        {
            throw new ArgumentOutOfRangeException(nameof(severity));
        }

        Dictionary<string, OperationalLogAttributeDefinition> definitions =
            new(StringComparer.Ordinal);

        foreach (OperationalLogAttributeDefinition definition in allowedAttributes)
        {
            ArgumentNullException.ThrowIfNull(definition);

            if (!definitions.TryAdd(definition.Name, definition))
            {
                throw new ArgumentException(
                    "An operational log event cannot define an attribute more than once.",
                    nameof(allowedAttributes));
            }
        }

        EventName = eventName;
        Severity = severity;
        Message = message;
        this.allowedAttributes =
            new ReadOnlyDictionary<string, OperationalLogAttributeDefinition>(
                definitions);
    }

    public string EventName { get; }

    public OperationalLogSeverity Severity { get; }

    public string Message { get; }

    public IReadOnlyDictionary<string, OperationalLogAttributeDefinition>
        AllowedAttributes => allowedAttributes;

    private static void ValidateMessage(string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        if (message.Length > MaximumMessageCharacters ||
            message[0] is < 'A' or > 'Z' ||
            message[^1] is not ('.' or '!' or '?') ||
            message.Any(static character =>
                !char.IsAsciiLetterOrDigit(character) &&
                character is not (' ' or '.' or ',' or '\'' or '(' or ')' or
                    '-' or '!' or '?')) ||
            ProhibitedMessageParts.Any(part =>
                message.Contains(part, StringComparison.OrdinalIgnoreCase)))
        {
            throw new ArgumentException(
                "An operational log message must be a bounded, reviewed safe sentence.",
                nameof(message));
        }
    }
}
