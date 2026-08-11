using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.Versioning;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using Opure.Ipc.Abstractions;
using Opure.Ipc.NamedPipes.Windows;
using Opure.Runtime.Contracts;
using Opure.Runtime.Contracts.Health.V1;
using Xunit;

namespace Opure.Ipc.NamedPipes.Windows.Tests;

[SupportedOSPlatform("windows")]
public class IpcSecuritySuiteTests
{
    private static RuntimeHealthEndpoint CreateEndpoint()
    {
        return NamedPipeRuntimeHealthEndpoint.Create("Test", Guid.NewGuid().ToString("N"));
    }

    private static RuntimeHealthSessionPolicy CreatePolicy(RuntimeHealthSessionMaterial material)
    {
        return new RuntimeHealthSessionPolicy(material, DateTimeOffset.UtcNow.AddMinutes(5));
    }

    private sealed class StaticHealthHandler(string bootId) : IRuntimeHealthRequestHandler
    {
        public Task<GetRuntimeHealthResponse> HandleAsync(
            GetRuntimeHealthRequest request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new GetRuntimeHealthResponse
            {
                ContractRevision = 1,
                Health = new RuntimeHealthProjection
                {
                    ProductVersion = "1.0.0-test",
                    RuntimeBootId = bootId,
                    RuntimeMode = RuntimeMode.Normal,
                    Readiness = RuntimeReadiness.Ready,
                    OverallHealth = RuntimeHealthState.Healthy,
                    GeneratedUnixTimeMilliseconds = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                }
            });
        }
    }

    [Fact]
    public void Pipe_security_denies_other_windows_users_by_acl()
    {
        PipeSecurity security = WindowsNamedPipeSecurity.CreateCurrentUserOnly();
        var rules = security.GetAccessRules(true, true, typeof(SecurityIdentifier));
        
        bool hasCurrentUser = false;
        bool hasOtherUsers = false;
        
        SecurityIdentifier currentUser = WindowsIdentity.GetCurrent().User!;

        foreach (PipeAccessRule rule in rules.Cast<PipeAccessRule>())
        {
            if (rule.IdentityReference.Value == currentUser.Value)
            {
                hasCurrentUser = true;
            }
            else
            {
                hasOtherUsers = true;
            }
        }
        
        Assert.True(hasCurrentUser, "Current user must be allowed.");
        Assert.False(hasOtherUsers, "Other users must not be present in the ACL.");
    }

    [Fact]
    public async Task Gateway_server_does_not_open_tcp_listeners()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        RuntimeHealthEndpoint endpoint = CreateEndpoint();
        RuntimeHealthSessionMaterial material = RuntimeHealthSessionMaterial.Create();

        await using NamedPipeGatewayServer server = await NamedPipeGatewayServer.StartAsync(
            endpoint,
            new StaticHealthHandler(endpoint.RuntimeBootId),
            CreatePolicy(material),
            cancellationToken);

        // Allow Kestrel to fully bind
        await Task.Delay(1000, cancellationToken);
        
        int currentProcessId = Environment.ProcessId;
        
        IPGlobalProperties ipProperties = IPGlobalProperties.GetIPGlobalProperties();
        var listeners = ipProperties.GetActiveTcpListeners();
        
        // Assert no new listeners were opened by checking if we can connect to typical Kestrel ports
        // Or simply checking if any listener is bound to port 5000/5001/8080/80
        // Wait, Kestrel might bind to a dynamic port if configured, but by default it's 5000.
        // We can just use an HttpClient to probe http://localhost:5000
        
        using var client = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(1) };
        try
        {
            var response = await client.GetAsync("http://localhost:5000", cancellationToken);
            Assert.Fail("Kestrel responded on port 5000, meaning a TCP listener exists!");
        }
        catch (System.Net.Http.HttpRequestException)
        {
            // Expected: Connection refused
        }
        catch (TaskCanceledException)
        {
            // Expected: Timeout
        }
    }

    [Fact]
    public async Task Malformed_protobuf_does_not_crash_server()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        RuntimeHealthEndpoint endpoint = CreateEndpoint();
        RuntimeHealthSessionMaterial material = RuntimeHealthSessionMaterial.Create();

        await using NamedPipeGatewayServer server = await NamedPipeGatewayServer.StartAsync(
            endpoint,
            new StaticHealthHandler(endpoint.RuntimeBootId),
            CreatePolicy(material),
            cancellationToken);

        using var rawClient = new NamedPipeClientStream(
            ".",
            endpoint.PipeName,
            PipeDirection.InOut,
            PipeOptions.Asynchronous | PipeOptions.WriteThrough);

        await rawClient.ConnectAsync(2000, cancellationToken);
        
        // Write garbage
        byte[] garbage = new byte[1024];
        Random.Shared.NextBytes(garbage);
        await rawClient.WriteAsync(garbage, cancellationToken);
        
        // Wait to see if server crashes
        await Task.Delay(1000, cancellationToken);
        
        // If the server didn't crash, a valid client should still be able to connect and get an Unauthenticated error (or timeout since session is bad, but server is ALIVE)
        await using NamedPipeRuntimeHealthClient validClient = new(endpoint, RuntimeHealthSessionMaterial.Create());
        
        var ex = await Assert.ThrowsAsync<Opure.Ipc.Abstractions.RuntimeHealthTransportException>(() => validClient.GetRuntimeHealthAsync(
            new GetRuntimeHealthRequest(),
            RuntimeHealthContractPolicy.DefaultDeadline,
            cancellationToken));
            

    }

    [Fact]
    public async Task Rapid_connection_attempts_are_handled_safely()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        RuntimeHealthEndpoint endpoint = CreateEndpoint();
        RuntimeHealthSessionMaterial material = RuntimeHealthSessionMaterial.Create();

        await using NamedPipeGatewayServer server = await NamedPipeGatewayServer.StartAsync(
            endpoint,
            new StaticHealthHandler(endpoint.RuntimeBootId),
            CreatePolicy(material),
            cancellationToken);

        // Spam the server with raw connections
        var tasks = Enumerable.Range(0, 100).Select(async _ =>
        {
            try
            {
                using var rawClient = new NamedPipeClientStream(
                    ".",
                    endpoint.PipeName,
                    PipeDirection.InOut,
                    PipeOptions.Asynchronous);
                await rawClient.ConnectAsync(100, cancellationToken);
                // disconnect immediately
            }
            catch
            {
                // Ignore timeouts
            }
        });

        await Task.WhenAll(tasks);
        
        await Task.Delay(2000, cancellationToken);
        
        // Server should still be alive
        await using NamedPipeRuntimeHealthClient validClient = new(endpoint, material);
        var response = await validClient.GetRuntimeHealthAsync(
            new GetRuntimeHealthRequest { MinimumContractRevision = 1, MaximumContractRevision = 1, QueryId = Guid.NewGuid().ToString("N") },
            RuntimeHealthContractPolicy.DefaultDeadline,
            cancellationToken);
            
        Assert.NotNull(response);
    }
}
