using System.Diagnostics.CodeAnalysis;
using Opure.Ipc.Abstractions;
using Opure.Ipc.NamedPipes.Windows;
using Opure.Recovery.Protocol;
using Opure.Recovery.Protocol.Point.V1;
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
            WriteUsage();
            return 1;
        }

        string command = args[0].ToLowerInvariant();
        return command switch
        {
            "health" => await HandleHealthAsync(),
            "recovery" => await HandleRecoveryAsync(args[1..]),
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

    private static void WriteUsage()
    {
        Console.WriteLine("Opure CLI");
        Console.WriteLine("Commands: health, version, recovery create|list|show");
        Console.WriteLine("  opure recovery create [--channel Development|Preview|Stable]");
        Console.WriteLine("  opure recovery list [--channel Development|Preview|Stable]");
        Console.WriteLine("  opure recovery show --id <guid> [--channel Development|Preview|Stable]");
    }

    private static async Task<int> HandleRecoveryAsync(string[] args)
    {
        if (args.Length == 0)
        {
            Console.Error.WriteLine("A recovery subcommand is required: create, list or show.");
            return 1;
        }

        if (!TryParseRecoveryArguments(
                args[1..],
                out string channel,
                out Guid? recoveryPointId,
                out string? argumentError))
        {
            Console.Error.WriteLine(argumentError);
            return 1;
        }

        if (!TryGetRuntimeEndpoint(
                out RuntimeHealthEndpoint? endpoint,
                out RuntimeHealthSessionMaterial? sessionMaterial))
        {
            Console.Error.WriteLine("Error: Could not locate a running Opure Runtime in this environment.");
            Console.Error.WriteLine("Run this command in the bounded Opure session created by Bootstrap.");
            return 1;
        }

        try
        {
            await using NamedPipeRecoveryPointClient client = new(endpoint, sessionMaterial);
            return args[0].ToLowerInvariant() switch
            {
                "create" => await CreateRecoveryPointAsync(client, channel),
                "list" => await ListRecoveryPointsAsync(client, channel),
                "show" => await ShowRecoveryPointAsync(client, channel, recoveryPointId),
                _ => HandleUnknownRecoveryCommand(args[0])
            };
        }
        catch (OperationCanceledException)
        {
            Console.Error.WriteLine("The recovery request was cancelled.");
            return 1;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"Recovery request failed: {exception.Message}");
            return 1;
        }
    }

    private static async Task<int> CreateRecoveryPointAsync(
        NamedPipeRecoveryPointClient client,
        string channel)
    {
        CreateRecoveryPointResponseMessage response = await client.CreateRecoveryPointAsync(
            new CreateRecoveryPointRequestMessage
            {
                ContractRevision = RecoveryPointContractPolicy.CurrentRevision,
                ReleaseChannel = channel
            },
            CancellationToken.None);

        if (!response.IsSuccess)
        {
            Console.Error.WriteLine($"Recovery point creation failed: {response.ErrorMessage}");
            return 1;
        }

        Console.WriteLine($"Created local recovery point {response.RecoveryPointId}.");
        Console.WriteLine("Scope: Same-device recovery only");
        return 0;
    }

    private static async Task<int> ListRecoveryPointsAsync(
        NamedPipeRecoveryPointClient client,
        string channel)
    {
        ListRecoveryPointsResponseMessage response = await QueryRecoveryPointsAsync(client, channel);
        if (response.Points.Count == 0)
        {
            Console.WriteLine("No local recovery points found.");
            return 0;
        }

        Console.WriteLine("ID                                   Created (local)             Structural verification");
        foreach (RecoveryPointSummaryMessage point in response.Points)
        {
            DateTimeOffset created = DateTimeOffset.FromUnixTimeMilliseconds(
                point.CreatedAtUnixTimeMilliseconds).ToLocalTime();
            Console.WriteLine($"{point.RecoveryPointId,-36} {created:yyyy-MM-dd HH:mm:ss zzz} {point.VerificationState}");
        }

        Console.WriteLine("Scope: Same-device recovery only");
        return 0;
    }

    private static async Task<int> ShowRecoveryPointAsync(
        NamedPipeRecoveryPointClient client,
        string channel,
        Guid? recoveryPointId)
    {
        if (recoveryPointId is null)
        {
            Console.Error.WriteLine("The show command requires --id <guid>.");
            return 1;
        }

        ListRecoveryPointsResponseMessage response = await QueryRecoveryPointsAsync(client, channel);
        RecoveryPointSummaryMessage? point = response.Points.FirstOrDefault(candidate =>
            Guid.TryParse(candidate.RecoveryPointId, out Guid candidateId) &&
            candidateId == recoveryPointId.Value);
        if (point is null)
        {
            Console.Error.WriteLine($"Recovery point {recoveryPointId.Value:D} was not found.");
            return 1;
        }

        DateTimeOffset created = DateTimeOffset.FromUnixTimeMilliseconds(
            point.CreatedAtUnixTimeMilliseconds).ToLocalTime();
        Console.WriteLine($"Recovery point ID: {recoveryPointId.Value:D}");
        Console.WriteLine($"Created: {created:O}");
        Console.WriteLine($"Channel: {channel}");
        Console.WriteLine($"Product version: {point.ProductVersion}");
        Console.WriteLine($"Required owners: {point.OwnerCount}");
        Console.WriteLine($"Schema versions: {string.Join(", ", point.SupportedSchemaVersions)}");
        Console.WriteLine($"Checkpoint hashes: {point.CheckpointHashes.Count}");
        Console.WriteLine($"Structural verification: {point.VerificationState}");
        Console.WriteLine("Creation and verification receipts:");
        foreach (RecoveryPointReceiptMessage receipt in point.Receipts)
        {
            DateTimeOffset receiptTimestamp = DateTimeOffset.FromUnixTimeMilliseconds(
                receipt.TimestampUnixTimeMilliseconds).ToLocalTime();
            Console.WriteLine($"  {receipt.EventType} at {receiptTimestamp:O}: {receipt.StatusMessage}");
        }

        Console.WriteLine("Scope: Same-device recovery only");
        return 0;
    }

    private static Task<ListRecoveryPointsResponseMessage> QueryRecoveryPointsAsync(
        NamedPipeRecoveryPointClient client,
        string channel) =>
        client.ListRecoveryPointsAsync(
            new ListRecoveryPointsRequestMessage
            {
                ContractRevision = RecoveryPointContractPolicy.CurrentRevision,
                ReleaseChannel = channel
            },
            CancellationToken.None);

    private static int HandleUnknownRecoveryCommand(string command)
    {
        Console.Error.WriteLine($"Unknown recovery command: {command}");
        return 1;
    }

    private static bool TryParseRecoveryArguments(
        string[] args,
        out string channel,
        out Guid? recoveryPointId,
        [NotNullWhen(false)] out string? error)
    {
        channel = "Development";
        recoveryPointId = null;
        error = null;

        for (int index = 0; index < args.Length; index += 2)
        {
            if (index + 1 >= args.Length)
            {
                error = $"Missing value for {args[index]}.";
                return false;
            }

            string value = args[index + 1];
            switch (args[index])
            {
                case "--channel":
                    if (value is not ("Development" or "Preview" or "Stable"))
                    {
                        error = "Channel must be Development, Preview or Stable.";
                        return false;
                    }

                    channel = value;
                    break;
                case "--id":
                    if (!Guid.TryParse(value, out Guid parsedId))
                    {
                        error = "Recovery point ID must be a GUID.";
                        return false;
                    }

                    recoveryPointId = parsedId;
                    break;
                default:
                    error = $"Unknown recovery option: {args[index]}";
                    return false;
            }
        }

        return true;
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
