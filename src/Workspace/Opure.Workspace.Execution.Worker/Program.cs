using System;
using System.IO;

using System.Runtime.Versioning;

namespace Opure.Workspace.Execution.Worker;

[SupportedOSPlatform("windows")]
public static class Program
{
    public static int Main(string[] args)
    {
        if (args.Length != 3)
        {
            Console.Error.WriteLine("Usage: Opure.Workspace.Execution.Worker <target-absolute-path> <payload-absolute-path> <backup-absolute-path>");
            return 1;
        }

        string targetPath = args[0];
        string payloadPath = args[1];
        string backupPath = args[2];

        if (!Path.IsPathRooted(targetPath) || !Path.IsPathRooted(payloadPath) || !Path.IsPathRooted(backupPath))
        {
            Console.Error.WriteLine("Paths must be absolute.");
            return 2;
        }

        try
        {
            using Stream stdin = Console.OpenStandardInput();
            using MemoryStream memoryStream = new();
            stdin.CopyTo(memoryStream);

            byte[] content = memoryStream.ToArray();

            // 1. Write the payload to the secure staging path first
            File.WriteAllBytes(payloadPath, content);
            
            // Verify the write (length check as basic verification)
            var writtenInfo = new FileInfo(payloadPath);
            if (writtenInfo.Length != content.Length)
            {
                Console.Error.WriteLine("Payload write verification failed.");
                return 5;
            }

            // 2. Execute the atomic swap
            AtomicFileReplacer.Replace(targetPath, payloadPath, backupPath);

            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.ToString());
            return 3;
        }
    }
}
