using Opure.Observability.Contracts;

namespace Opure.Observability;

internal interface IOperationalLogRedactor
{
    OperationalLogEvent RedactForEnqueue(
        OperationalLogEvent logEvent,
        OperationalLogPolicy policy);
}

internal sealed class OperationalLogRedactor : IOperationalLogRedactor
{
    public OperationalLogEvent RedactForEnqueue(
        OperationalLogEvent logEvent,
        OperationalLogPolicy policy)
    {
        return OperationalLogSanitiser.SanitiseForEnqueue(
            logEvent,
            policy);
    }
}
