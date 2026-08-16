using System.Collections.Generic;
using System.Threading;

namespace Opure.Runtime.Contracts.Models;

public interface IRemoteModelClient
{
    IAsyncEnumerable<StreamPayload> RunRemoteModelAsync(
        RemoteProviderConfiguration config,
        ModelRequest request,
        CancellationToken cancellationToken = default);
}
