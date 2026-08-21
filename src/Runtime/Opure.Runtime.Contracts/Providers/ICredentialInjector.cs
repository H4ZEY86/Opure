using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Opure.Runtime.Contracts.Providers;

public interface ICredentialInjector
{
    Task InjectAsync(HttpRequestMessage request, DataSharingPlan plan, CancellationToken cancellationToken);
}
