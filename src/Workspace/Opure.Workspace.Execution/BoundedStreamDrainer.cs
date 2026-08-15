using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Opure.Workspace.Contracts;

namespace Opure.Workspace.Execution;

public sealed class BoundedStreamDrainer
{
    private const int MaxBytes = 1024 * 1024; // 1 MB limit

    public static async Task<CommandOutputBuffer> DrainAsync(Stream stream, CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(stream, new UTF8Encoding(false, false), true, 4096, leaveOpen: true);
        
        var builder = new StringBuilder();
        long totalBytesRead = 0;
        bool truncated = false;
        
        var buffer = new char[4096];

        try
        {
            while (true)
            {
                int read = await reader.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
                if (read == 0)
                {
                    break;
                }

                int byteCount = Encoding.UTF8.GetByteCount(buffer, 0, read);
                totalBytesRead += byteCount;

                if (!truncated)
                {
                    if (totalBytesRead > MaxBytes)
                    {
                        truncated = true;
                        // Approximate how much we can still add (rough char equivalent, as we only need hard truncation)
                        int capacityLeft = MaxBytes - (int)(totalBytesRead - byteCount);
                        if (capacityLeft > 0)
                        {
                            // Convert bytes left to chars left roughly, or just omit the last partial chunk for safety
                            // We'll just omit the last read that broke the limit to ensure we are strictly under.
                        }
                    }
                    else
                    {
                        builder.Append(buffer, 0, read);
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            // We swallow cancellation here to preserve what we read so far
        }
        catch (Exception)
        {
            // Any stream error also truncates safely
            truncated = true;
        }

        string scrubbed = StreamRedactionPipeline.Scrub(builder.ToString(), out bool redactionApplied, out bool encodingFaults);

        return new CommandOutputBuffer(
            scrubbed,
            new CommandOutputMetadata(
                truncated,
                totalBytesRead,
                redactionApplied,
                encodingFaults));
    }
}
