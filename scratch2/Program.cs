using System;
using System.Diagnostics;
using System.IO;

class Program
{
    static void Main()
    {
        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.DoNotVerify);
        string dataRoot = Path.Combine(localAppData, ""Opure"", ""Test"");
        
        if (Directory.Exists(dataRoot))
        {
            try { Directory.Delete(dataRoot, true); } catch { }
        }
        Directory.CreateDirectory(dataRoot);
        
        var startInfo = new ProcessStartInfo
        {
            FileName = @""C:\Opure\artifacts\bin\Opure.Bootstrap.Windows\Release\Opure.Bootstrap.Windows.exe"",
            Arguments = ""--channel Test --layout Development"",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        startInfo.Environment[""OPURE_BOOTSTRAP_TEST_MODE""] = ""1"";
        
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
