using System;

namespace Opure.Workspace.Execution.Models;

public static class WindowsJobObjectBindings
{
    // Stubs for Windows Job Object P/Invoke
    public static IntPtr CreateJobObject(IntPtr jobAttributes, string? name)
    {
        return IntPtr.Zero;
    }
}
