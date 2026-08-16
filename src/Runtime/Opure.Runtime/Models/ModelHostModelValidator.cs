using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Opure.Runtime.Contracts.Models;
using Opure.Workspace.Contracts.Models;

namespace Opure.Runtime.Models;

public class ModelHostModelValidator : IModelHostModelValidator
{
    private readonly IModelManifestStore _manifestStore;

    public ModelHostModelValidator(IModelManifestStore manifestStore)
    {
        _manifestStore = manifestStore;
    }

    public async Task ValidateAsync(string modelPath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(modelPath))
        {
            throw new FileNotFoundException($"Model file not found: {modelPath}", modelPath);
        }

        var manifest = await _manifestStore.GetManifestAsync(modelPath, cancellationToken);
        if (manifest == null)
        {
            throw new InvalidDataException($"No manifest found for model at {modelPath}");
        }

        // The hash verification engine integration goes here
        await Task.CompletedTask;
    }
}
