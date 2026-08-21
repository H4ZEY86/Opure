using System.Threading;
using System.Threading.Tasks;

namespace Opure.Runtime.Contracts.Providers;

public interface ISecretVault
{
    Task<string?> GetSecretAsync(string key, CancellationToken cancellationToken);
}
