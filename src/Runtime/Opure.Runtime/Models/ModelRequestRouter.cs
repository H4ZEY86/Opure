using System;
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

    public async IAsyncEnumerable<StreamPayload> RouteRequestAsync(
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
        var requestJson = JsonSerializer.Serialize(request, ModelContractsJsonContext.Default.ModelRequest);
        try
        {
            // Write out the JSON request payload
            await stdin.WriteLineAsync(requestJson.AsMemory(), cancellationToken).ConfigureAwait(false);
            await stdin.FlushAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (System.IO.IOException)
        {
            // Process may have exited before reading all input, which closes the pipe.
            // We ignore this and let the read loop capture any stdout output or exit.
        }

        while (!cancellationToken.IsCancellationRequested)
        {
            var line = await stdout.ReadLineAsync(cancellationToken).ConfigureAwait(false);
            if (line == null)
            {
                break;
            }

            StreamPayload? payload = null;
            if (line.TrimStart().StartsWith('{') && line.TrimEnd().EndsWith('}'))
            {
                try
                {
                    payload = JsonSerializer.Deserialize(line, ModelContractsJsonContext.Default.StreamPayload);
                }
                catch (JsonException)
                {
                    // Fallback to text if JSON is malformed
                }
            }

            if (payload != null)
            {
                yield return payload;
            }
            else
            {
                yield return new StreamPayload(false, line + Environment.NewLine);
            }
        }

        // Enforce tight cancellation propagation
        cancellationToken.ThrowIfCancellationRequested();
    }
}
