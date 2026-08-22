using System.Threading;
using System.Threading.Tasks;

namespace Opure.Runtime.Contracts.Configuration;

/// <summary>
/// Provides typed access to the channel-scoped Opure configuration store.
/// The store is owned by the Runtime; the Desktop must never read or write
/// the backing file directly.
/// </summary>
public interface IOpureConfigStore
{
    /// <summary>Returns a boolean setting, or <paramref name="defaultValue"/> if absent.</summary>
    bool GetBool(string key, bool defaultValue = false);

    /// <summary>Persists a boolean setting, flushing to disk before returning.</summary>
    Task SetBoolAsync(string key, bool value, CancellationToken ct);
}

/// <summary>Well-known configuration keys used across the product.</summary>
public static class OpureConfigKeys
{
    public const string IsProActivated = "IsProActivated";
}
