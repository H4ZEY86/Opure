using System;
using System.IO;

namespace Opure.Workspace.Execution.Worker;

public static class Program
{
    public static int Main(string[] args)
    {
        if (args.Length != 1)
        {
            Console.Error.WriteLine("Usage: Opure.Workspace.Execution.Worker <target-absolute-path>");
            return 1;
        }

        string targetPath = args[0];
        if (!Path.IsPathRooted(targetPath))
        {
            Console.Error.WriteLine("Target path must be absolute.");
            return 2;
        }

        try
        {
            using Stream stdin = Console.OpenStandardInput();
            using MemoryStream memoryStream = new();
            stdin.CopyTo(memoryStream);

            byte[] content = memoryStream.ToArray();

            string directory = Path.GetDirectoryName(targetPath)!;
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string tempPath = targetPath + ".tmp." + Guid.NewGuid().ToString("N");
            File.WriteAllBytes(tempPath, content);
            File.Move(tempPath, targetPath, overwrite: true);

            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.ToString());
            return 3;
        }
    }
}
