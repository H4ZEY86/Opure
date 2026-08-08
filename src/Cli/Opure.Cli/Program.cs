using System.Diagnostics.CodeAnalysis;
using Grpc.Net.Client;
using Opure.Ipc.Abstractions;
using Opure.Ipc.NamedPipes.Windows;
using Opure.Runtime.Contracts;
using Opure.Runtime.Contracts.Health.V1;

namespace Opure.Cli;

internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Opure CLI");
            Console.WriteLine("Commands: health, version");
            return 1;
        }

        string command = args[0].ToLowerInvariant();
        return command switch
        {
            "health" => await HandleHealthAsync(),
            "version" => HandleVersion(),
            _ => HandleUnknownCommand(command)
        };
    }

    private static int HandleVersion()
    {
        Console.WriteLine("Opure CLI v0.1.0-preview.0");
        return 0;
    }

    private static int HandleUnknownCommand(string command)
    {
        Console.Error.WriteLine($"Unknown command: {command}");
        return 1;
    }

    private static async Task<int> HandleHealthAsync()
    {
        if (!TryGetRuntimeEndpoint(out RuntimeHealthEndpoint? endpoint, out RuntimeHealthSessionMaterial? sessionMaterial))
        {
            Console.Error.WriteLine("Error: Could not locate a running Opure Runtime in this environment.");
            Console.Error.WriteLine("Ensure the CLI is running within a valid Opure Session.");
            return 1;
        }

        try
        {
            await using NamedPipeRuntimeHealthClient client = new(endpoint, sessionMaterial);
            GetRuntimeHealthRequest request = new()
            {
                MinimumContractRevision = RuntimeHealthContractPolicy.CurrentRevision,
                MaximumContractRevision = RuntimeHealthContractPolicy.CurrentRevision,
                QueryId = Guid.NewGuid().ToString("N")
            };

            using CancellationTokenSource cts = new(RuntimeHealthContractPolicy.DefaultDeadline);
            GetRuntimeHealthResponse response = await client.GetRuntimeHealthAsync(
                request,
                RuntimeHealthContractPolicy.DefaultDeadline,
                cts.Token);

            if (response.OutcomeCase == GetRuntimeHealthResponse.OutcomeOneofCase.Health)
            {
                var health = response.Health;
                Console.WriteLine($"Runtime Status: {health.OverallHealth}");
                Console.WriteLine($"Readiness: {health.Readiness}");
                Console.WriteLine($"Mode: {health.RuntimeMode}");
                Console.WriteLine($"Product Version: {health.ProductVersion}");
                Console.WriteLine($"Boot ID: {health.RuntimeBootId}");
                Console.WriteLine($"Services: {health.Services.Count}");
                foreach (var service in health.Services)
                {
                    Console.WriteLine($"  - {service.ServiceId}: {service.State}");
                }
                return 0;
            }
            else if (response.OutcomeCase == GetRuntimeHealthResponse.OutcomeOneofCase.Error)
            {
                Console.Error.WriteLine($"Runtime returned an error: {response.Error.Code} - {response.Error.SafeMessage}");
                return 1;
            }

            Console.Error.WriteLine("Runtime returned an unknown outcome.");
            return 1;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to connect to Runtime: {ex.Message}");
            return 1;
        }
    }

    private static bool TryGetRuntimeEndpoint(
        [NotNullWhen(true)] out RuntimeHealthEndpoint? endpoint,
        [NotNullWhen(true)] out RuntimeHealthSessionMaterial? sessionMaterial)
    {
        string? pipeName = Environment.GetEnvironmentVariable("OPURE_RUNTIME_PIPE_NAME");
        string? bootId = Environment.GetEnvironmentVariable("OPURE_RUNTIME_BOOT_ID");
        string? sessionId = Environment.GetEnvironmentVariable("OPURE_BOOTSTRAP_SESSION_ID");
        string? sessionSecret = Environment.GetEnvironmentVariable("OPURE_BOOTSTRAP_SESSION_SECRET");

        if (string.IsNullOrWhiteSpace(pipeName) || string.IsNullOrWhiteSpace(bootId))
        {
            endpoint = null;
            sessionMaterial = null;
            return false;
        }

        endpoint = new RuntimeHealthEndpoint(pipeName, bootId);
        
        if (string.IsNullOrWhiteSpace(sessionId) || string.IsNullOrWhiteSpace(sessionSecret))
        {
            sessionMaterial = null;
            return false;
        }
        
        sessionMaterial = new RuntimeHealthSessionMaterial(sessionId, sessionSecret);
        return true;
    }
}

