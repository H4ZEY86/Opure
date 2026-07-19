using Opure.Ipc.Abstractions;
using Opure.Runtime.Contracts;
using Opure.Runtime.Contracts.Health.V1;

namespace Opure.Runtime;

internal sealed class RuntimeHealthRequestHandler(
    RuntimeBootSnapshot bootSnapshot) : IRuntimeHealthRequestHandler
{
    public Task<GetRuntimeHealthResponse> HandleAsync(
        GetRuntimeHealthRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        if (RuntimeHealthContractPolicy.NegotiateRevision(
                request.MinimumContractRevision,
                request.MaximumContractRevision) == 0)
        {
            return Task.FromResult(
                RuntimeHealthContractPolicy.CreateIncompatibleRevisionResponse());
        }

        GetRuntimeHealthResponse response = new()
        {
            ContractRevision = RuntimeHealthContractPolicy.CurrentRevision,
            Health = new RuntimeHealthProjection
            {
                ProductVersion = bootSnapshot.ProductVersion,
                RuntimeBootId = bootSnapshot.BootId,
                RuntimeMode = RuntimeMode.Normal,
                Readiness = RuntimeReadiness.Ready,
                OverallHealth = RuntimeHealthState.Healthy,
                GeneratedUnixTimeMilliseconds =
                    DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            }
        };

        response.Health.Services.Add(new ServiceHealthSummary
        {
            ServiceId = "runtime.health",
            State = ServiceHealthState.Ready,
            RequiredForReadiness = true,
            SafeDetail = "Runtime Health is ready."
        });

        return Task.FromResult(response);
    }
}
