using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Opure.Workspace.Containment;

[Flags]
public enum ContainmentJobLimitFlags : uint
{
    None = 0,
    ProcessTime = 0x00000002,
    JobTime = 0x00000004,
    ActiveProcess = 0x00000008,
    Affinity = 0x00000010,
    ProcessMemory = 0x00000100,
    JobMemory = 0x00000200,
    KillOnJobClose = 0x00002000
}

public sealed class WindowsContainmentJob : IDisposable
{
    private readonly SafeJobHandle _handle;

    public WindowsContainmentJob(
        long memoryLimitBytes,
        uint activeProcessLimit = 1,
        long cpuTimeLimit100Ns = 0)
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException("Containment Job Object requires Windows.");
        }

        _handle = NativeMethods.CreateJobObject(IntPtr.Zero, null);
        if (_handle.IsInvalid)
        {
            throw new Win32Exception(Marshal.GetLastPInvokeError(), "Could not create Job Object.");
        }

        ContainmentJobLimitFlags flags = ContainmentJobLimitFlags.KillOnJobClose 
                                         | ContainmentJobLimitFlags.ActiveProcess
                                         | ContainmentJobLimitFlags.JobMemory
                                         | ContainmentJobLimitFlags.ProcessMemory;

        if (cpuTimeLimit100Ns > 0)
        {
            flags |= ContainmentJobLimitFlags.JobTime;
        }

        JobObjectExtendedLimitInformation info = new()
        {
            BasicLimitInformation = new JobObjectBasicLimitInformation
            {
                LimitFlags = flags,
                ActiveProcessLimit = activeProcessLimit,
                PerJobUserTimeLimit = cpuTimeLimit100Ns
            },
            ProcessMemoryLimit = (UIntPtr)memoryLimitBytes,
            JobMemoryLimit = (UIntPtr)memoryLimitBytes
        };

        int length = Marshal.SizeOf<JobObjectExtendedLimitInformation>();
        if (!NativeMethods.SetInformationJobObject(_handle, JobObjectInformationClass.ExtendedLimitInformation, ref info, (uint)length))
        {
            int error = Marshal.GetLastPInvokeError();
            _handle.Dispose();
            throw new Win32Exception(error, "Could not configure job object limits.");
        }
    }

    public void AssignProcess(Process process)
    {
        ArgumentNullException.ThrowIfNull(process);

        if (!NativeMethods.AssignProcessToJobObject(_handle, process.SafeHandle))
        {
            throw new Win32Exception(Marshal.GetLastPInvokeError(), "Could not assign process to Job Object.");
        }
    }

    public void Dispose()
    {
        _handle.Dispose();
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct JobObjectBasicLimitInformation
    {
        internal long PerProcessUserTimeLimit;
        internal long PerJobUserTimeLimit;
        internal ContainmentJobLimitFlags LimitFlags;
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
        public SafeJobHandle() : base(true) { }
        protected override bool ReleaseHandle() => NativeMethods.CloseHandle(handle);
    }

    private static class NativeMethods
    {
        [DllImport("kernel32.dll", EntryPoint = "CreateJobObjectW", ExactSpelling = true, SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern SafeJobHandle CreateJobObject(IntPtr jobAttributes, string? name);

        [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool SetInformationJobObject(SafeJobHandle job, JobObjectInformationClass infoClass, ref JobObjectExtendedLimitInformation info, uint infoLength);

        [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool AssignProcessToJobObject(SafeJobHandle job, SafeProcessHandle process);

        [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool CloseHandle(IntPtr handle);
    }
}
