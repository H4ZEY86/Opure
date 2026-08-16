using System;
using System.Threading;
using System.Threading.Tasks;

namespace Opure.Runtime.Contracts.Models;

public interface IModelHostProcessLauncher : IDisposable
{
    Task<ModelHostSession> LaunchAsync(
        string modelPath,
        string? prompt = null,
        CancellationToken cancellationToken = default);

    Task SendPromptAsync(ModelHostSession session, string prompt, CancellationToken cancellationToken = default);

    Task<ModelHostResponse?> ReadResponseAsync(ModelHostSession session, CancellationToken cancellationToken = default);
}
