using System.Threading;
using System.Threading.Tasks;

namespace Opure.Runtime.Contracts.Plugins;

public interface IPluginHost
{
    Task StartAsync(PluginPackageRecord package, CapabilityLease lease, CancellationToken ct);
    Task StopAsync();
    Task<string> SendCommandAsync(string payload, CancellationToken ct);
}
