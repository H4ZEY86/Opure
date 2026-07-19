using Opure.Runtime.Contracts.Health.V1;

namespace Opure.Ipc.Abstractions;

/// <summary>
/// Identifies one boot-scoped Runtime Health endpoint without carrying credentials.
/// </summary>
public sealed record RuntimeHealthEndpoint(string PipeName, string RuntimeBootId);

/// <summary>
/// Contains ephemeral Bootstrap-issued material for one local IPC session.
/// </summary>
public sealed record RuntimeHealthSessionMaterial(
    string SessionId,
    string SessionSecret)
{
    public static RuntimeHealthSessionMaterial Create()
    {
        byte[] secretBytes = System.Security.Cryptography.RandomNumberGenerator
            .GetBytes(32);

        try
        {
            string secret = Convert.ToBase64String(secretBytes)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');

            return new RuntimeHealthSessionMaterial(
                Guid.NewGuid().ToString("N"),
                secret);
        }
        finally
        {
            System.Security.Cryptography.CryptographicOperations.ZeroMemory(
                secretBytes);
        }
    }
}

/// <summary>
/// Defines the server-side lifetime and expected client class for one session.
/// </summary>
public sealed record RuntimeHealthSessionPolicy(
    RuntimeHealthSessionMaterial Material,
    DateTimeOffset ExpiresAtUtc,
    string ExpectedClientClass = "desktop");

/// <summary>
/// Contains safe, bounded authentication evidence without session material.
/// </summary>
public sealed record RuntimeHealthAuthenticationEvent(
    bool Established,
    string ReasonCode,
    int? ClientProcessId);

/// <summary>
/// Provides transport-independent access to the Runtime Health contract.
/// </summary>
public interface IRuntimeHealthTransportClient : IAsyncDisposable
{
    Task<GetRuntimeHealthResponse> GetRuntimeHealthAsync(
        GetRuntimeHealthRequest request,
        TimeSpan deadline,
        CancellationToken cancellationToken);
}

/// <summary>
/// Handles one semantically valid Runtime Health request on behalf of a transport.
/// </summary>
public interface IRuntimeHealthRequestHandler
{
    Task<GetRuntimeHealthResponse> HandleAsync(
        GetRuntimeHealthRequest request,
        CancellationToken cancellationToken);
}

/// <summary>
/// Represents a running Runtime Health transport host.
/// </summary>
public interface IRuntimeHealthTransportHost : IAsyncDisposable
{
    RuntimeHealthEndpoint Endpoint { get; }
}

/// <summary>
/// Defines stable local transport failure codes presented above gRPC.
/// </summary>
public static class RuntimeHealthTransportErrorCodes
{
    public const string Cancelled = "HEALTH_TRANSPORT_CANCELLED";
    public const string DeadlineExceeded = "HEALTH_TRANSPORT_DEADLINE_EXCEEDED";
    public const string EndpointInvalid = "HEALTH_TRANSPORT_ENDPOINT_INVALID";
    public const string MessageTooLarge = "HEALTH_TRANSPORT_MESSAGE_TOO_LARGE";
    public const string RuntimeBootChanged = "HEALTH_TRANSPORT_BOOT_CHANGED";
    public const string ServerIdentityInvalid = "HEALTH_SESSION_SERVER_INVALID";
    public const string SessionDenied = "HEALTH_SESSION_DENIED";
    public const string Unavailable = "HEALTH_TRANSPORT_UNAVAILABLE";
}

/// <summary>
/// Describes a bounded, payload-free local transport failure.
/// </summary>
public sealed class RuntimeHealthTransportException : Exception
{
    public RuntimeHealthTransportException(
        string errorCode,
        string safeMessage,
        bool retryable,
        Exception? innerException = null)
        : base(safeMessage, innerException)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(errorCode);
        ErrorCode = errorCode;
        Retryable = retryable;
    }

    public string ErrorCode { get; }

    public bool Retryable { get; }
}

/// <summary>
/// Defines bounded transport defaults shared by named-pipe adapters.
/// </summary>
public static class RuntimeHealthTransportPolicy
{
    public const int MaximumPendingStreamMessages = 32;
    public static readonly TimeSpan AuthenticationClockSkew = TimeSpan.FromSeconds(30);
    public static readonly TimeSpan SessionLifetime = TimeSpan.FromHours(12);
    public static readonly TimeSpan ConnectionTimeout =
        TimeSpan.FromMilliseconds(250);
}
