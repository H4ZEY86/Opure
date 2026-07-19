using System.Security.Cryptography;

namespace Opure.Bootstrap.Windows;

internal sealed record BootstrapSession(
    string SessionId,
    string SessionSecret)
{
    internal static BootstrapSession Create()
    {
        byte[] secretBytes = RandomNumberGenerator.GetBytes(32);

        string secret = Convert
            .ToBase64String(secretBytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

        return new BootstrapSession(
            Guid.NewGuid().ToString("N"),
            secret);
    }
}
