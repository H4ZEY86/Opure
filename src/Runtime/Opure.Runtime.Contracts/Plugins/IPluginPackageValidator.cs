using System.Threading;
using System.Threading.Tasks;

namespace Opure.Runtime.Contracts.Plugins;

/// <summary>
/// Validates an incoming plugin package and forces it into a Pending quarantine state.
/// </summary>
public interface IPluginPackageValidator
{
    Task<PluginPackageRecord> ValidateAndQuarantineAsync(string archivePath, CancellationToken ct);
}
