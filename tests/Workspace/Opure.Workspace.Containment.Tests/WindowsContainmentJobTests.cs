using System.Diagnostics;
using Xunit;

namespace Opure.Workspace.Containment.Tests;

public class WindowsContainmentJobTests
{
    [Fact]
    public void ContainmentJob_Prevents_Child_Process_Creation()
    {
        if (!OperatingSystem.IsWindows())
        {
            return; // Not applicable
        }

        // 100MB limit, 1 active process limit prevents spawning child processes
        using WindowsContainmentJob job = new(memoryLimitBytes: 1024 * 1024 * 100, activeProcessLimit: 1);

        ProcessStartInfo psi = new()
        {
            FileName = "cmd.exe",
            // 1. Wait for AssignProcess to happen (ping -n 2).
            // 2. Try to spawn another process. If blocked by job limits, it fails (errorlevel != 0) and we exit 42.
            // 3. If it succeeds (boundary failure), we exit 0.
            Arguments = "/c \"ping 127.0.0.1 -n 2 > nul & cmd.exe /c echo Child > nul && exit 0 || exit 42\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using Process process = new() { StartInfo = psi };
        process.Start();

        // Assign before the sleep finishes, effectively trapping the pwsh process in the job object
        job.AssignProcess(process);

        process.WaitForExit();

        Assert.Equal(42, process.ExitCode);
    }
}
