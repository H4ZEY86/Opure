using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using Opure.Runtime.Contracts.Models;

namespace Opure.Runtime.Models;

public class ModelRequestRouter : IModelRequestRouter
{
    private readonly JsonSerializerOptions _jsonOptions;

    public ModelRequestRouter()
    {
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    public async IAsyncEnumerable<string> RouteRequestAsync(
        ModelHostSession session,
        ModelRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (session.Process == null || session.Process.HasExited)
        {
            throw new InvalidOperationException("Model host session process is not running or invalid.");
        }

        var process = session.Process;
        var stdin = process.StandardInput;
        var stdout = process.StandardOutput;

        // Serialize and write request using UTF8 JSON
        var requestJson = JsonSerializer.Serialize(request, _jsonOptions);
        
        // Write out the JSON request payload
        await stdin.WriteLineAsync(requestJson.AsMemory(), cancellationToken).ConfigureAwait(false);
        await stdin.FlushAsync(cancellationToken).ConfigureAwait(false);

        // Zero-allocation streaming buffer read
        char[] buffer = ArrayPool<char>.Shared.Rent(4096);
        try
        {
            Memory<char> memory = buffer;

            while (!cancellationToken.IsCancellationRequested)
            {
                int bytesRead = await stdout.ReadAsync(memory, cancellationToken).ConfigureAwait(false);
                if (bytesRead == 0)
                {
                    break;
                }

                // Yield the buffered chunk
                yield return new string(buffer, 0, bytesRead);
            }
        }
        finally
        {
            ArrayPool<char>.Shared.Return(buffer);
        }

        // Enforce tight cancellation propagation
        cancellationToken.ThrowIfCancellationRequested();
    }
}
