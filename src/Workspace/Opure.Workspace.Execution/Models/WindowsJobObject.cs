using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Opure.Workspace.Execution.Models;

public sealed class SafeJobObjectHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    public SafeJobObjectHandle() : base(true)
    {
    }

    protected override bool ReleaseHandle()
    {
        return WindowsJobObjectBindings.CloseHandle(handle);
    }
}

public sealed class WindowsJobObject : IDisposable
{
    private readonly SafeJobObjectHandle _handle;

    public WindowsJobObject()
    {
        _handle = WindowsJobObjectBindings.CreateJobObject(IntPtr.Zero, null);
        if (_handle.IsInvalid)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to create Windows Job Object.");
        }

        var extendedInfo = new WindowsJobObjectBindings.JOBOBJECT_EXTENDED_LIMIT_INFORMATION
        {
            BasicLimitInformation = new WindowsJobObjectBindings.JOBOBJECT_BASIC_LIMIT_INFORMATION
            {
                LimitFlags = WindowsJobObjectBindings.JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE
            }
        };

        int length = Marshal.SizeOf<WindowsJobObjectBindings.JOBOBJECT_EXTENDED_LIMIT_INFORMATION>();
        IntPtr extendedInfoPtr = Marshal.AllocHGlobal(length);
        try
        {
            Marshal.StructureToPtr(extendedInfo, extendedInfoPtr, false);

            if (!WindowsJobObjectBindings.SetInformationJobObject(
                _handle,
                WindowsJobObjectBindings.JobObjectExtendedLimitInformation,
                extendedInfoPtr,
                length))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to set Job Object information.");
            }
        }
        finally
        {
            Marshal.FreeHGlobal(extendedInfoPtr);
        }
    }

    public void AssignProcess(Process process)
    {
        ArgumentNullException.ThrowIfNull(process);
        
        if (!WindowsJobObjectBindings.AssignProcessToJobObject(_handle, process.Handle))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to assign process to Job Object.");
        }
    }

    public IntPtr DangerousGetHandle() => _handle.DangerousGetHandle();

    public void DangerousAddRef(ref bool success) => _handle.DangerousAddRef(ref success);

    public void DangerousRelease() => _handle.DangerousRelease();

    public void Dispose()
    {
        _handle.Dispose();
    }
}
