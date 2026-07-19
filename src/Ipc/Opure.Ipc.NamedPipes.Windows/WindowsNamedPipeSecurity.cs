using System.IO.Pipes;
using System.Runtime.Versioning;
using System.Security.AccessControl;
using System.Security.Principal;

namespace Opure.Ipc.NamedPipes.Windows;

internal static class WindowsNamedPipeSecurity
{
    [SupportedOSPlatform("windows")]
    internal static PipeSecurity CreateCurrentUserOnly()
    {
        SecurityIdentifier currentUser =
            WindowsIdentity.GetCurrent().User ??
            throw new InvalidOperationException(
                "The current Windows user identity is unavailable.");
        SecurityIdentifier localSystem = new(
            WellKnownSidType.LocalSystemSid,
            domainSid: null);
        PipeSecurity security = new();
        security.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);
        security.SetOwner(currentUser);
        security.AddAccessRule(new PipeAccessRule(
            currentUser,
            PipeAccessRights.FullControl,
            AccessControlType.Allow));
        security.AddAccessRule(new PipeAccessRule(
            localSystem,
            PipeAccessRights.FullControl,
            AccessControlType.Allow));
        return security;
    }
}
