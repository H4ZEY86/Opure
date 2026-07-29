namespace Opure.Observability.Contracts;

public sealed class OperationalLogContext
{
    public OperationalLogContext(
        string serviceId,
        string serviceVersion,
        string runtimeBootId)
    {
        OperationalLogContract.ValidateStableName(serviceId, nameof(serviceId));
        OperationalLogContract.ValidateServiceVersion(serviceVersion);
        OperationalLogContract.ValidateRuntimeBootId(runtimeBootId);

        ServiceId = serviceId;
        ServiceVersion = serviceVersion;
        RuntimeBootId = runtimeBootId;
    }

    public string ServiceId { get; }

    public string ServiceVersion { get; }

    public string RuntimeBootId { get; }
}
