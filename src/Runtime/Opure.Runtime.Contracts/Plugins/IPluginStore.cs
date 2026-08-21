using System.Threading;
using System.Threading.Tasks;

namespace Opure.Runtime.Contracts.Plugins;

public interface IPluginStore
{
    Task SavePackageRecordAsync(PluginPackageRecord record, CancellationToken ct);
    Task<PluginPackageRecord?> GetPackageRecordAsync(string pluginId, CancellationToken ct);
    Task SaveLeaseAsync(CapabilityLease lease, CancellationToken ct);
    Task<CapabilityLease?> GetLeaseAsync(string pluginId, CancellationToken ct);
}
