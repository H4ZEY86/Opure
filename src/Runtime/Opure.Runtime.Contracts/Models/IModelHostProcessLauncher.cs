using System;
using System.Threading;
using System.Threading.Tasks;

namespace Opure.Runtime.Contracts.Models;

public interface IModelHostProcessLauncher : IDisposable
{
    Task<ModelHostSession> LaunchAsync(
        ModelProcessConfiguration configuration,
        CancellationToken cancellationToken = default);
}

