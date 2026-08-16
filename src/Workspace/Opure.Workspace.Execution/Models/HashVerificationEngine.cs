using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace Opure.Workspace.Execution.Models;

public static class HashVerificationEngine
{
    public static async Task<byte[]> ComputeSha256Async(string filePath, CancellationToken cancellationToken = default)
    {
        using var stream = File.OpenRead(filePath);
        using var sha256 = SHA256.Create();
        return await sha256.ComputeHashAsync(stream, cancellationToken);
    }
}
