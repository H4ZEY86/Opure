using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Opure.Packaging.Windows;

public static class Program
{
    public static int Main(string[] args)
    {
        try
        {
            if (args.Length < 6)
            {
                Console.WriteLine("Usage: Opure.Packaging.Windows <output-dir> <publish-dir> <version> <channel> <cert-path> <cert-password>");
                return 1;
            }

            var outputDir = Path.GetFullPath(args[0]);
            var publishDir = Path.GetFullPath(args[1]);
            var version = args[2];
            var channel = args[3];
            var certPath = args[4];
            var certPassword = args[5];

            var packageName = channel == "Development" ? "Opure.Dev" : (channel == "Preview" ? "Opure.Preview" : "Opure");
            var displayName = channel == "Stable" ? "Opure" : $"Opure {channel}";

            Console.WriteLine($"Composing package {packageName} v{version} from {publishDir} to {outputDir}...");

            var stagingDir = Path.Combine(outputDir, "Staging");
            if (Directory.Exists(stagingDir))
            {
                Directory.Delete(stagingDir, true);
            }
            Directory.CreateDirectory(stagingDir);

            // Copy published outputs
            Console.WriteLine("Copying published outputs...");
            CopyDirectory(publishDir, stagingDir);

            // Copy assets
            var templateDir = Path.Combine(AppContext.BaseDirectory, "Templates");
            var assetsDest = Path.Combine(stagingDir, "Assets");
            Directory.CreateDirectory(assetsDest);
            CopyDirectory(Path.Combine(templateDir, "Assets"), assetsDest);

            // Compose manifest
            Console.WriteLine("Composing AppxManifest.xml...");
            var manifestTemplatePath = Path.Combine(templateDir, "AppxManifest.template.xml");
            var manifestContent = File.ReadAllText(manifestTemplatePath);
            manifestContent = manifestContent
                .Replace("{{PackageName}}", packageName)
                .Replace("{{Publisher}}", "CN=Opure Development")
                .Replace("{{Version}}", version)
                .Replace("{{DisplayName}}", displayName);
            
            File.WriteAllText(Path.Combine(stagingDir, "AppxManifest.xml"), manifestContent);

            // Find MakeAppx and SignTool
            var kitsDir = @"C:\Program Files (x86)\Windows Kits\10\bin";
            if (!Directory.Exists(kitsDir))
            {
                throw new DirectoryNotFoundException("Windows Kits not found. Make sure Windows SDK is installed.");
            }

            var versionDirs = Directory.GetDirectories(kitsDir).OrderByDescending(d => d).ToList();
            var makeAppxPath = versionDirs.Select(d => Path.Combine(d, "x64", "makeappx.exe")).FirstOrDefault(File.Exists);
            var signToolPath = versionDirs.Select(d => Path.Combine(d, "x64", "signtool.exe")).FirstOrDefault(File.Exists);

            if (makeAppxPath == null || signToolPath == null)
            {
                throw new FileNotFoundException("MakeAppx.exe or SignTool.exe not found in Windows Kits.");
            }

            // Pack
            var msixFile = Path.Combine(outputDir, $"{packageName}-{version}-win-x64.msix");
            if (File.Exists(msixFile))
            {
                File.Delete(msixFile);
            }

            Console.WriteLine($"Running MakeAppx: {msixFile}");
            var makeAppxArgs = $"pack /d \"{stagingDir}\" /p \"{msixFile}\" /o";
            RunProcess(makeAppxPath, makeAppxArgs);

            // Sign
            Console.WriteLine($"Running SignTool: {msixFile}");
            var signToolArgs = $"sign /fd SHA256 /a /f \"{certPath}\" /p \"{certPassword}\" \"{msixFile}\"";
            RunProcess(signToolPath, signToolArgs);

            Console.WriteLine("Package created successfully!");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ERROR: {ex.Message}");
            return 1;
        }
    }

    private static void CopyDirectory(string sourceDir, string destinationDir)
    {
        var dir = new DirectoryInfo(sourceDir);
        if (!dir.Exists)
            throw new DirectoryNotFoundException($"Source directory not found: {dir.FullName}");

        var dirs = dir.GetDirectories();
        Directory.CreateDirectory(destinationDir);

        foreach (var file in dir.GetFiles())
        {
            var targetFilePath = Path.Combine(destinationDir, file.Name);
            file.CopyTo(targetFilePath, true);
        }

        foreach (var subDir in dirs)
        {
            var newDestinationDir = Path.Combine(destinationDir, subDir.Name);
            CopyDirectory(subDir.FullName, newDestinationDir);
        }
    }

    private static void RunProcess(string executable, string arguments)
    {
        var process = Process.Start(new ProcessStartInfo
        {
            FileName = executable,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        });

        if (process == null)
            throw new InvalidOperationException("Failed to start process.");

        process.OutputDataReceived += (s, e) => { if (e.Data != null) Console.WriteLine(e.Data); };
        process.ErrorDataReceived += (s, e) => { if (e.Data != null) Console.Error.WriteLine(e.Data); };

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            throw new Exception($"{executable} failed with exit code {process.ExitCode}");
        }
    }
}
