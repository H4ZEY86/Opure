using System;
using System.Collections.Generic;
using Opure.Runtime.Contracts.Models;

namespace Opure.Runtime.Models;

public sealed class OllamaCommandBuilder : IModelCommandBuilder
{
    public ModelProcessConfiguration Build(string modelPath, ModelRequest request)
    {
        ArgumentNullException.ThrowIfNull(modelPath);
        ArgumentNullException.ThrowIfNull(request);

        // Assuming manifest.ModelPath points to the model name in ollama, or we just run 'ollama run <name>'
        // However, Opure verifies the binary. So manifest.ModelPath might actually be 'ollama.exe'
        // and we need to pass the model name. For now, let's assume `manifest.ModelPath` is the ollama executable.
        // And the model name is passed via parameters, or manifest has a property for it.
        // But manifest.ModelPath is just a string. Let's assume we run `ollama.exe run <ModelName>`

        var args = new List<string> { "run" };
        
        if (request.Parameters != null && request.Parameters.TryGetValue("ModelName", out var modelNameObj) && modelNameObj is string modelName)
        {
            args.Add(modelName);
        }
        else
        {
            args.Add("default_model");
        }

        // Add secure arguments for context, threads, and ngl if provided
        if (request.Parameters != null)
        {
            if (request.Parameters.TryGetValue("ContextWindow", out var ctxObj) && ctxObj is int ctx)
            {
                args.Add("-c");
                args.Add(ctx.ToString());
            }

            if (request.Parameters.TryGetValue("ThreadCount", out var threadsObj) && threadsObj is int threads)
            {
                args.Add("-t");
                args.Add(threads.ToString());
            }

            if (request.Parameters.TryGetValue("GpuLayers", out var nglObj) && nglObj is int ngl)
            {
                args.Add("-ngl");
                args.Add(ngl.ToString());
            }
        }

        return new ModelProcessConfiguration
        {
            ExecutablePath = modelPath,
            Arguments = args
        };
    }
}
