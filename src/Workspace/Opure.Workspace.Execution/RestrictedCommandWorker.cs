using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Opure.Workspace.Contracts;
using Opure.Workspace.Containment;

namespace Opure.Workspace.Execution;

public interface IRestrictedCommandWorker
{
    Task<CommandExecutionResult> ExecuteAsync(ToolTemplate template, string workingDirectory, CancellationToken cancellationToken);
}

public class RestrictedCommandWorker : IRestrictedCommandWorker
{
    private readonly IExecutableResolver _resolver;

    public RestrictedCommandWorker(IExecutableResolver resolver)
    {
        _resolver = resolver;
    }

    public async Task<CommandExecutionResult> ExecuteAsync(ToolTemplate template, string workingDirectory, CancellationToken cancellationToken)
    {
        ToolTemplateValidator.Validate(template);

        string absolutePath = _resolver.Resolve(template.ExecutableName);

        var startInfo = new ProcessStartInfo
        {
            FileName = absolutePath,
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = template.InputOutputPolicy.SupportsStdin
        };

        foreach (var arg in template.Arguments)
        {
            startInfo.ArgumentList.Add(arg);
        }

        startInfo.EnvironmentVariables.Clear();
        foreach (var allowedVar in template.EnvironmentPolicy.AllowedVariables)
        {
            var value = Environment.GetEnvironmentVariable(allowedVar);
            if (value != null)
            {
                startInfo.EnvironmentVariables[allowedVar] = value;
            }
        }

        using var jobObject = new WindowsJobObject(template.ResourceClass);
        using var process = new Process { StartInfo = startInfo };

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(template.TimeoutMilliseconds);

        process.Start();

        try
        {
            jobObject.AddProcess(process);
        }
        catch
        {
            process.Kill(true);
            throw;
        }

        var outTask = BoundedStreamDrainer.DrainAsync(process.StandardOutput.BaseStream, cts.Token);
        var errTask = BoundedStreamDrainer.DrainAsync(process.StandardError.BaseStream, cts.Token);

        try
        {
            await Task.WhenAll(process.WaitForExitAsync(cts.Token), outTask, errTask);
        }
        catch (OperationCanceledException)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                throw new OperationCanceledException("Execution cancelled by user.");
            }
            throw new TimeoutException($"Command exceeded timeout of {template.TimeoutMilliseconds}ms.");
        }

        // Even if the process exits, we must ensure we got the buffers.
        var stdout = await outTask;
        var stderr = await errTask;

        return new CommandExecutionResult(process.ExitCode, stdout, stderr);
    }
}
