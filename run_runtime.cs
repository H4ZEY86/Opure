using System;
using System.Diagnostics;

class Program
{
    static void Main()
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = @""C:\Opure\artifacts\bin\Opure.Runtime\Release\Opure.Runtime.exe"",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        startInfo.Environment[""OPURE_BOOTSTRAP_MANAGED""] = ""1"";
        startInfo.Environment[""OPURE_CHANNEL""] = ""Test"";
        startInfo.Environment[""OPURE_DATA_ROOT""] = @""C:\Opure\scratch\TestRoot"";
        startInfo.Environment[""OPURE_BOOTSTRAP_SESSION_ID""] = ""00000000000000000000000000000000"";
        startInfo.Environment[""OPURE_BOOTSTRAP_SESSION_SECRET""] = ""aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"";
        startInfo.Environment[""OPURE_BOOTSTRAP_PARENT_PID""] = Process.GetCurrentProcess().Id.ToString();
        startInfo.Environment[""OPURE_BOOTSTRAP_PARENT_START_UTC""] = DateTimeOffset.UtcNow.ToString(""O"");
        
        var proc = Process.Start(startInfo);
        proc.WaitForExit(5000);
        
        Console.WriteLine(""Exited: "" + proc.HasExited);
        if (proc.HasExited) {
            Console.WriteLine(""ExitCode: "" + proc.ExitCode);
            Console.WriteLine(""StdOut: "" + proc.StandardOutput.ReadToEnd());
            Console.WriteLine(""StdErr: "" + proc.StandardError.ReadToEnd());
        }
    }
}
