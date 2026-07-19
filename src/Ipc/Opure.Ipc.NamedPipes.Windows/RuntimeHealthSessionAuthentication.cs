using System.Globalization;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Grpc.Core;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Win32.SafeHandles;
using Opure.Ipc.Abstractions;

namespace Opure.Ipc.NamedPipes.Windows;

internal static partial class RuntimeHealthSessionAuthentication
{
    internal const string SessionIdHeader = "x-opure-session-id";
    internal const string ClientClassHeader = "x-opure-client-class";
    internal const string ClientProcessIdHeader = "x-opure-client-pid";
    internal const string RuntimeBootIdHeader = "x-opure-runtime-boot-id";
    internal const string IssuedUnixMillisecondsHeader = "x-opure-issued-unix-ms";
    internal const string NonceHeader = "x-opure-nonce";
    internal const string ClientProofHeader = "x-opure-client-proof";
    internal const string ServerProofHeader = "x-opure-server-proof";

    internal static Metadata CreateClientMetadata(
        RuntimeHealthEndpoint endpoint,
        RuntimeHealthSessionMaterial material,
        string method,
        int clientProcessId,
        DateTimeOffset now,
        out string nonce,
        out string clientProof,
        string? suppliedNonce = null)
    {
        ValidateMaterial(material);

        nonce = suppliedNonce ?? Encode(RandomNumberGenerator.GetBytes(16));

        if (nonce.Length != 22)
        {
            throw new ArgumentException(
                "The Runtime Health authentication nonce is invalid.",
                nameof(suppliedNonce));
        }
        string issued = now.ToUnixTimeMilliseconds().ToString(
            CultureInfo.InvariantCulture);
        clientProof = ComputeProof(
            material.SessionSecret,
            CreateClientCanonicalValue(
                method,
                material.SessionId,
                endpoint,
                "desktop",
                clientProcessId,
                issued,
                nonce));

        return
        [
            new Metadata.Entry(SessionIdHeader, material.SessionId),
            new Metadata.Entry(ClientClassHeader, "desktop"),
            new Metadata.Entry(
                ClientProcessIdHeader,
                clientProcessId.ToString(CultureInfo.InvariantCulture)),
            new Metadata.Entry(RuntimeBootIdHeader, endpoint.RuntimeBootId),
            new Metadata.Entry(IssuedUnixMillisecondsHeader, issued),
            new Metadata.Entry(NonceHeader, nonce),
            new Metadata.Entry(ClientProofHeader, clientProof)
        ];
    }

    internal static bool VerifyServerProof(
        RuntimeHealthEndpoint endpoint,
        RuntimeHealthSessionMaterial material,
        string method,
        string nonce,
        string clientProof,
        Metadata responseHeaders)
    {
        string? proof = GetSingleValue(responseHeaders, ServerProofHeader);

        if (!ProofPattern().IsMatch(proof ?? string.Empty))
        {
            return false;
        }

        string expected = ComputeProof(
            material.SessionSecret,
            CreateServerCanonicalValue(
                method,
                material.SessionId,
                endpoint,
                nonce,
                clientProof));

        return FixedTimeEquals(expected, proof!);
    }

    internal static string ComputeServerProof(
        RuntimeHealthEndpoint endpoint,
        RuntimeHealthSessionMaterial material,
        string method,
        string nonce,
        string clientProof)
    {
        return ComputeProof(
            material.SessionSecret,
            CreateServerCanonicalValue(
                method,
                material.SessionId,
                endpoint,
                nonce,
                clientProof));
    }

    internal static string CreateClientCanonicalValue(
        string method,
        string sessionId,
        RuntimeHealthEndpoint endpoint,
        string clientClass,
        int clientProcessId,
        string issuedUnixMilliseconds,
        string nonce)
    {
        return string.Join(
            '\n',
            "opure-ipc-auth-v1",
            method,
            sessionId,
            endpoint.RuntimeBootId,
            endpoint.PipeName,
            clientClass,
            clientProcessId.ToString(CultureInfo.InvariantCulture),
            issuedUnixMilliseconds,
            nonce);
    }

    internal static string? GetSingleValue(Metadata metadata, string name)
    {
        string[] values = metadata
            .Where(entry => string.Equals(entry.Key, name, StringComparison.Ordinal))
            .Select(entry => entry.Value)
            .ToArray();

        return values.Length == 1 ? values[0] : null;
    }

    internal static bool FixedTimeEquals(string expected, string actual)
    {
        byte[] expectedBytes = Encoding.ASCII.GetBytes(expected);
        byte[] actualBytes = Encoding.ASCII.GetBytes(actual);

        try
        {
            return CryptographicOperations.FixedTimeEquals(
                expectedBytes,
                actualBytes);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(expectedBytes);
            CryptographicOperations.ZeroMemory(actualBytes);
        }
    }

    internal static bool TryGetActualClientProcessId(
        ServerCallContext context,
        out int clientProcessId)
    {
        IConnectionNamedPipeFeature? feature = context
            .GetHttpContext()
            .Features
            .Get<IConnectionNamedPipeFeature>();

        if (feature is null ||
            !GetNamedPipeClientProcessId(
                feature.NamedPipe.SafePipeHandle,
                out uint processId) ||
            processId is 0 or > int.MaxValue)
        {
            clientProcessId = 0;
            return false;
        }

        clientProcessId = (int)processId;
        return true;
    }

    internal static void ValidateMaterial(RuntimeHealthSessionMaterial material)
    {
        ArgumentNullException.ThrowIfNull(material);

        if (!SessionIdPattern().IsMatch(material.SessionId) ||
            !SessionSecretPattern().IsMatch(material.SessionSecret))
        {
            throw new ArgumentException(
                "The Runtime Health session material is invalid.",
                nameof(material));
        }
    }

    private static string CreateServerCanonicalValue(
        string method,
        string sessionId,
        RuntimeHealthEndpoint endpoint,
        string nonce,
        string clientProof)
    {
        return string.Join(
            '\n',
            "opure-ipc-server-v1",
            method,
            sessionId,
            endpoint.RuntimeBootId,
            endpoint.PipeName,
            nonce,
            clientProof);
    }

    internal static string ComputeProof(string encodedSecret, string canonicalValue)
    {
        byte[] key = Decode(encodedSecret);
        byte[] value = Encoding.UTF8.GetBytes(canonicalValue);
        byte[]? proof = null;

        try
        {
            proof = HMACSHA256.HashData(key, value);
            return Encode(proof);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
            CryptographicOperations.ZeroMemory(value);

            if (proof is not null)
            {
                CryptographicOperations.ZeroMemory(proof);
            }
        }
    }

    private static string Encode(ReadOnlySpan<byte> value)
    {
        return Convert.ToBase64String(value)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static byte[] Decode(string value)
    {
        string padded = value.Replace('-', '+').Replace('_', '/');
        padded += new string('=', (4 - padded.Length % 4) % 4);
        return Convert.FromBase64String(padded);
    }

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetNamedPipeClientProcessId(
        SafePipeHandle pipe,
        out uint clientProcessId);

    [GeneratedRegex("^[0-9a-f]{32}$", RegexOptions.CultureInvariant)]
    private static partial Regex SessionIdPattern();

    [GeneratedRegex("^[A-Za-z0-9_-]{43}$", RegexOptions.CultureInvariant)]
    private static partial Regex SessionSecretPattern();

    [GeneratedRegex("^[A-Za-z0-9_-]{43}$", RegexOptions.CultureInvariant)]
    private static partial Regex ProofPattern();
}

internal sealed class RuntimeHealthSessionAuthenticator
{
    private const int MaximumReplayEntries = 4096;
    private readonly RuntimeHealthEndpoint endpoint;
    private readonly RuntimeHealthSessionPolicy policy;
    private readonly TimeProvider timeProvider;
    private readonly Func<RuntimeHealthAuthenticationEvent, ValueTask>? eventSink;
    private readonly Lock stateLock = new();
    private readonly Dictionary<string, long> usedNonces = new(
        StringComparer.Ordinal);
    private int? boundClientProcessId;

    internal RuntimeHealthSessionAuthenticator(
        RuntimeHealthEndpoint endpoint,
        RuntimeHealthSessionPolicy policy,
        TimeProvider timeProvider,
        Func<RuntimeHealthAuthenticationEvent, ValueTask>? eventSink)
    {
        RuntimeHealthSessionAuthentication.ValidateMaterial(policy.Material);
        this.endpoint = endpoint;
        this.policy = policy;
        this.timeProvider = timeProvider;
        this.eventSink = eventSink;
    }

    internal async ValueTask<RuntimeHealthAuthenticationResult> AuthenticateAsync(
        ServerCallContext context)
    {
        Metadata headers = context.RequestHeaders;
        string? sessionId = RuntimeHealthSessionAuthentication.GetSingleValue(
            headers,
            RuntimeHealthSessionAuthentication.SessionIdHeader);
        string? clientClass = RuntimeHealthSessionAuthentication.GetSingleValue(
            headers,
            RuntimeHealthSessionAuthentication.ClientClassHeader);
        string? clientProcessIdValue = RuntimeHealthSessionAuthentication.GetSingleValue(
            headers,
            RuntimeHealthSessionAuthentication.ClientProcessIdHeader);
        string? bootId = RuntimeHealthSessionAuthentication.GetSingleValue(
            headers,
            RuntimeHealthSessionAuthentication.RuntimeBootIdHeader);
        string? issuedValue = RuntimeHealthSessionAuthentication.GetSingleValue(
            headers,
            RuntimeHealthSessionAuthentication.IssuedUnixMillisecondsHeader);
        string? nonce = RuntimeHealthSessionAuthentication.GetSingleValue(
            headers,
            RuntimeHealthSessionAuthentication.NonceHeader);
        string? proof = RuntimeHealthSessionAuthentication.GetSingleValue(
            headers,
            RuntimeHealthSessionAuthentication.ClientProofHeader);

        if (sessionId is null || clientClass is null ||
            clientProcessIdValue is null || bootId is null ||
            issuedValue is null || nonce is null || proof is null)
        {
            return await DenyAsync("IPC_AUTH_MISSING", null).ConfigureAwait(false);
        }

        if (!int.TryParse(
                clientProcessIdValue,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out int claimedProcessId) ||
            claimedProcessId <= 0 ||
            !long.TryParse(
                issuedValue,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out long issuedMilliseconds) ||
            nonce.Length != 22 ||
            proof.Length != 43)
        {
            return await DenyAsync("IPC_AUTH_MALFORMED", null).ConfigureAwait(false);
        }

        if (!RuntimeHealthSessionAuthentication.TryGetActualClientProcessId(
                context,
                out int actualProcessId) ||
            actualProcessId != claimedProcessId)
        {
            return await DenyAsync("IPC_AUTH_CLIENT_PID", actualProcessId)
                .ConfigureAwait(false);
        }

        DateTimeOffset now = timeProvider.GetUtcNow();
        DateTimeOffset issued;

        try
        {
            issued = DateTimeOffset.FromUnixTimeMilliseconds(issuedMilliseconds);
        }
        catch (ArgumentOutOfRangeException)
        {
            return await DenyAsync("IPC_AUTH_MALFORMED", actualProcessId)
                .ConfigureAwait(false);
        }

        if (now >= policy.ExpiresAtUtc ||
            issued < now - RuntimeHealthTransportPolicy.AuthenticationClockSkew ||
            issued > now + RuntimeHealthTransportPolicy.AuthenticationClockSkew)
        {
            return await DenyAsync("IPC_AUTH_EXPIRED", actualProcessId)
                .ConfigureAwait(false);
        }

        if (!string.Equals(sessionId, policy.Material.SessionId, StringComparison.Ordinal) ||
            !string.Equals(clientClass, policy.ExpectedClientClass, StringComparison.Ordinal) ||
            !string.Equals(bootId, endpoint.RuntimeBootId, StringComparison.Ordinal))
        {
            return await DenyAsync("IPC_AUTH_CONTEXT", actualProcessId)
                .ConfigureAwait(false);
        }

        string canonical = RuntimeHealthSessionAuthentication.CreateClientCanonicalValue(
            context.Method,
            sessionId,
            endpoint,
            clientClass,
            claimedProcessId,
            issuedValue,
            nonce);
        string expectedProof = ComputeExpectedClientProof(canonical);

        if (!RuntimeHealthSessionAuthentication.FixedTimeEquals(expectedProof, proof))
        {
            return await DenyAsync("IPC_AUTH_PROOF", actualProcessId)
                .ConfigureAwait(false);
        }

        if (!TryAcceptClientAndNonce(
                actualProcessId,
                nonce,
                now.ToUnixTimeMilliseconds(),
                out string? denialReason,
                out bool sessionEstablished))
        {
            return await DenyAsync(denialReason, actualProcessId)
                .ConfigureAwait(false);
        }

        if (sessionEstablished && eventSink is not null)
        {
            await eventSink(new RuntimeHealthAuthenticationEvent(
                Established: true,
                "IPC_SESSION_ESTABLISHED",
                actualProcessId)).ConfigureAwait(false);
        }

        string serverProof = RuntimeHealthSessionAuthentication.ComputeServerProof(
            endpoint,
            policy.Material,
            context.Method,
            nonce,
            proof);

        return RuntimeHealthAuthenticationResult.Success(serverProof);
    }

    private bool TryAcceptClientAndNonce(
        int actualProcessId,
        string nonce,
        long nowUnixMilliseconds,
        [NotNullWhen(false)] out string? denialReason,
        out bool sessionEstablished)
    {
        lock (stateLock)
        {
            if (boundClientProcessId is int boundProcessId &&
                boundProcessId != actualProcessId)
            {
                denialReason = "IPC_AUTH_CLIENT_BOUND";
                sessionEstablished = false;
                return false;
            }

            long oldestAccepted = nowUnixMilliseconds - (long)
                RuntimeHealthTransportPolicy.AuthenticationClockSkew.TotalMilliseconds;

            if (usedNonces.Count >= MaximumReplayEntries)
            {
                string[] expiredNonces = usedNonces
                    .Where(pair => pair.Value < oldestAccepted)
                    .Select(pair => pair.Key)
                    .ToArray();

                foreach (string expiredNonce in expiredNonces)
                {
                    usedNonces.Remove(expiredNonce);
                }
            }

            if (usedNonces.Count >= MaximumReplayEntries)
            {
                denialReason = "IPC_AUTH_REPLAY_CAPACITY";
                sessionEstablished = false;
                return false;
            }

            if (!usedNonces.TryAdd(nonce, nowUnixMilliseconds))
            {
                denialReason = "IPC_AUTH_REPLAY";
                sessionEstablished = false;
                return false;
            }

            sessionEstablished = boundClientProcessId is null;
            boundClientProcessId ??= actualProcessId;
            denialReason = null;
            return true;
        }
    }

    private string ComputeExpectedClientProof(string canonical)
    {
        return RuntimeHealthSessionAuthentication.ComputeProof(
            policy.Material.SessionSecret,
            canonical);
    }

    private async ValueTask<RuntimeHealthAuthenticationResult> DenyAsync(
        string reasonCode,
        int? clientProcessId)
    {
        if (eventSink is not null)
        {
            await eventSink(new RuntimeHealthAuthenticationEvent(
                Established: false,
                reasonCode,
                clientProcessId)).ConfigureAwait(false);
        }

        return RuntimeHealthAuthenticationResult.Denied(reasonCode);
    }

}

internal sealed record RuntimeHealthAuthenticationResult(
    bool IsAuthenticated,
    string ReasonCode,
    string ServerProof)
{
    internal static RuntimeHealthAuthenticationResult Success(string serverProof) =>
        new(true, string.Empty, serverProof);

    internal static RuntimeHealthAuthenticationResult Denied(string reasonCode) =>
        new(false, reasonCode, string.Empty);
}
