using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Opure.Ipc.Abstractions;

namespace Opure.Desktop.GatewayClient;

public static partial class RuntimeHealthSessionEnvironment
{
    public static RuntimeHealthSessionMaterial? ReadCurrent()
    {
        Dictionary<string, string?> values = new(StringComparer.Ordinal)
        {
            ["OPURE_BOOTSTRAP_SESSION_ID"] = Environment.GetEnvironmentVariable(
                "OPURE_BOOTSTRAP_SESSION_ID"),
            ["OPURE_BOOTSTRAP_SESSION_SECRET"] = Environment.GetEnvironmentVariable(
                "OPURE_BOOTSTRAP_SESSION_SECRET")
        };

        if (TryCreate(values, out RuntimeHealthSessionMaterial? material))
        {
            return material;
        }

        string channel = Environment.GetEnvironmentVariable("OPURE_CHANNEL") ?? "Development";
        string dataRoot = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string lockfilePath = Path.Combine(dataRoot, "Opure", channel, "runtime.lock");

        if (System.IO.File.Exists(lockfilePath))
        {
            try
            {
                using var stream = new System.IO.FileStream(
                    lockfilePath,
                    System.IO.FileMode.Open,
                    System.IO.FileAccess.Read,
                    System.IO.FileShare.ReadWrite | System.IO.FileShare.Delete);
                
                using var document = System.Text.Json.JsonDocument.Parse(stream);
                if (document.RootElement.TryGetProperty("sessionId", out var sessionId) &&
                    document.RootElement.TryGetProperty("sessionSecret", out var sessionSecret))
                {
                    values["OPURE_BOOTSTRAP_SESSION_ID"] = sessionId.GetString();
                    values["OPURE_BOOTSTRAP_SESSION_SECRET"] = sessionSecret.GetString();

                    if (TryCreate(values, out material))
                    {
                        return material;
                    }
                }
            }
            catch
            {
                // Ignore parsing errors and return null
            }
        }

        return null;
    }

    public static bool TryCreate(
        IReadOnlyDictionary<string, string?> values,
        [NotNullWhen(true)] out RuntimeHealthSessionMaterial? material)
    {
        ArgumentNullException.ThrowIfNull(values);
        values.TryGetValue("OPURE_BOOTSTRAP_SESSION_ID", out string? sessionId);
        values.TryGetValue(
            "OPURE_BOOTSTRAP_SESSION_SECRET",
            out string? sessionSecret);

        if (!SessionIdPattern().IsMatch(sessionId ?? string.Empty) ||
            !SessionSecretPattern().IsMatch(sessionSecret ?? string.Empty))
        {
            material = null;
            return false;
        }

        material = new RuntimeHealthSessionMaterial(sessionId!, sessionSecret!);
        return true;
    }

    [GeneratedRegex("^[0-9a-f]{32}$", RegexOptions.CultureInvariant)]
    private static partial Regex SessionIdPattern();

    [GeneratedRegex("^[A-Za-z0-9_-]{43}$", RegexOptions.CultureInvariant)]
    private static partial Regex SessionSecretPattern();
}
