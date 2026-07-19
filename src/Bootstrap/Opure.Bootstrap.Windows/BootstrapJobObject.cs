using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Opure.Bootstrap.Windows;

[Flags]
internal enum BootstrapJobLimitFlags : uint
{
    None = 0,
    BreakawayOk = 0x00000800,
    SilentBreakawayOk = 0x00001000,
    KillOnJobClose = 0x00002000
}

internal interface IBootstrapProcessContainment : IDisposable
{
    BootstrapJobLimitFlags LimitFlags { get; }

    void Assign(Process process);
}

internal sealed class WindowsBootstrapJobObject : IBootstrapProcessContainment
{
    private readonly SafeJobHandle handle;

    internal WindowsBootstrapJobObject()
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException(
                "Bootstrap Job Object containment requires Windows.");
        }

        handle = NativeMethods.CreateJobObject(
            IntPtr.Zero,
            name: null);

        if (handle.IsInvalid)
        {
            throw CreateWin32Exception("Bootstrap could not create its Job Object.");
        }

        JobObjectExtendedLimitInformation information = new()
        {
            BasicLimitInformation = new JobObjectBasicLimitInformation
            {
                LimitFlags = LimitFlags
            }
        };

        int informationLength = Marshal.SizeOf<JobObjectExtendedLimitInformation>();

        if (!NativeMethods.SetInformationJobObject(
                handle,
                JobObjectInformationClass.ExtendedLimitInformation,
                ref information,
                (uint)informationLength))
        {
            handle.Dispose();
            throw CreateWin32Exception(
                "Bootstrap could not configure kill-on-close process containment.");
        }
    }

    public BootstrapJobLimitFlags LimitFlags =>
        BootstrapJobLimitFlags.KillOnJobClose;

    public void Assign(Process process)
    {
        ArgumentNullException.ThrowIfNull(process);

        if (!NativeMethods.AssignProcessToJobObject(
                handle,
                process.SafeHandle))
        {
            throw CreateWin32Exception(
                "Bootstrap could not assign a child to its Job Object.");
        }
    }

    public void Dispose()
    {
        handle.Dispose();
    }

    private static Win32Exception CreateWin32Exception(string message)
    {
        return new Win32Exception(
            Marshal.GetLastPInvokeError(),
            message);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct JobObjectBasicLimitInformation
    {
        internal long PerProcessUserTimeLimit;
        internal long PerJobUserTimeLimit;
        internal BootstrapJobLimitFlags LimitFlags;
        internal UIntPtr MinimumWorkingSetSize;
        internal UIntPtr MaximumWorkingSetSize;
        internal uint ActiveProcessLimit;
        internal UIntPtr Affinity;
        internal uint PriorityClass;
        internal uint SchedulingClass;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct IoCounters
    {
        internal ulong ReadOperationCount;
        internal ulong WriteOperationCount;
        internal ulong OtherOperationCount;
        internal ulong ReadTransferCount;
        internal ulong WriteTransferCount;
        internal ulong OtherTransferCount;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct JobObjectExtendedLimitInformation
    {
        internal JobObjectBasicLimitInformation BasicLimitInformation;
        internal IoCounters IoInfo;
        internal UIntPtr ProcessMemoryLimit;
        internal UIntPtr JobMemoryLimit;
        internal UIntPtr PeakProcessMemoryUsed;
        internal UIntPtr PeakJobMemoryUsed;
    }

    private enum JobObjectInformationClass
    {
        ExtendedLimitInformation = 9
    }

    private sealed class SafeJobHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        private SafeJobHandle()
            : base(ownsHandle: true)
        {
        }

        protected override bool ReleaseHandle()
        {
            return NativeMethods.CloseHandle(handle);
        }
    }

    private static class NativeMethods
    {
        [DllImport(
            "kernel32.dll",
            EntryPoint = "CreateJobObjectW",
            ExactSpelling = true,
            SetLastError = true,
            CharSet = CharSet.Unicode)]
        internal static extern SafeJobHandle CreateJobObject(
            IntPtr jobAttributes,
            string? name);

        [DllImport(
            "kernel32.dll",
            ExactSpelling = true,
            SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool SetInformationJobObject(
            SafeJobHandle job,
            JobObjectInformationClass informationClass,
            ref JobObjectExtendedLimitInformation information,
            uint informationLength);

        [DllImport(
            "kernel32.dll",
            ExactSpelling = true,
            SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool AssignProcessToJobObject(
            SafeJobHandle job,
            SafeProcessHandle process);

        [DllImport(
            "kernel32.dll",
            ExactSpelling = true,
            SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool CloseHandle(IntPtr handle);
    }
}
