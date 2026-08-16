using System;
using System.Collections.Generic;
using System.Threading;

namespace Opure.Runtime.Contracts.Models;

public interface IModelHostRunner
{
    IAsyncEnumerable<StreamPayload> RunModelAsync(
        string workspaceId,
        string manifestHash,
        ModelRequest request,
        CancellationToken cancellationToken = default);
}
