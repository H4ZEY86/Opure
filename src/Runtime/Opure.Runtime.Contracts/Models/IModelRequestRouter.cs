using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Opure.Runtime.Contracts.Models;

public interface IModelRequestRouter
{
    IAsyncEnumerable<string> RouteRequestAsync(
        ModelHostSession session,
        ModelRequest request,
        CancellationToken cancellationToken);
}
