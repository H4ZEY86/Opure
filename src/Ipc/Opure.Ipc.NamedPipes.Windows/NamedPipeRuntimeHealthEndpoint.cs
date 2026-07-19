using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Opure.Ipc.Abstractions;

namespace Opure.Ipc.NamedPipes.Windows;

public static partial class NamedPipeRuntimeHealthEndpoint
{
    public static RuntimeHealthEndpoint Create(
        string channel,
        string runtimeBootId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(channel);
        ArgumentException.ThrowIfNullOrWhiteSpace(runtimeBootId);

        string normalisedChannel = channel.ToLowerInvariant();

        if (!ChannelPattern().IsMatch(normalisedChannel) ||
            !OpaqueIdentifierPattern().IsMatch(runtimeBootId))
        {
            throw new ArgumentException(
                "The Runtime Health endpoint identity is invalid.",
                nameof(channel));
        }

        string endpointIdentity = Convert.ToHexString(
            RandomNumberGenerator.GetBytes(16)).ToLowerInvariant();

        return new RuntimeHealthEndpoint(
            $"opure-{normalisedChannel}-{endpointIdentity}",
            runtimeBootId);
    }

    public static bool IsValid(RuntimeHealthEndpoint endpoint)
    {
        ArgumentNullException.ThrowIfNull(endpoint);

        return PipeNamePattern().IsMatch(endpoint.PipeName) &&
            OpaqueIdentifierPattern().IsMatch(endpoint.RuntimeBootId);
    }

    [GeneratedRegex("^[a-z][a-z0-9]{2,31}$", RegexOptions.CultureInvariant)]
    private static partial Regex ChannelPattern();

    [GeneratedRegex("^[0-9a-f]{32}$", RegexOptions.CultureInvariant)]
    private static partial Regex OpaqueIdentifierPattern();

    [GeneratedRegex(
        "^opure-[a-z][a-z0-9]{2,31}-[0-9a-f]{32}$",
        RegexOptions.CultureInvariant)]
    private static partial Regex PipeNamePattern();
}
