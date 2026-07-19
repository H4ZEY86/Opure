using Opure.Runtime.Contracts;

namespace Opure.Runtime;

public static class RuntimeProductIdentity
{
    public static RuntimeBootSnapshot CreateBootSnapshot()
    {
        return new RuntimeBootSnapshot(
            RuntimeBootIdentity.Create(),
            Environment.ProcessId,
            ThisAssembly.AssemblyInformationalVersion,
            RuntimeContractVersion.Current);
    }
}
