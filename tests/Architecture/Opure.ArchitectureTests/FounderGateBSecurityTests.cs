using Xunit;
using System.IO;
using System.Linq;
using System;

namespace Opure.ArchitectureTests;

public sealed class FounderGateBSecurityTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();

    [Fact]
    public void FoundationContainsZeroAiAgentOrPluginCapability()
    {
        string[] allSourceFiles = Directory.GetFiles(
            Path.Combine(RepositoryRoot, "src"), 
            "*.cs", 
            SearchOption.AllDirectories);

        foreach (string file in allSourceFiles)
        {
            string content = File.ReadAllText(file);
            Assert.DoesNotContain("using Microsoft.SemanticKernel", content, StringComparison.Ordinal);
            Assert.DoesNotContain("using Azure.AI.OpenAI", content, StringComparison.Ordinal);
            Assert.DoesNotContain("LangChain", content, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Ollama", content, StringComparison.OrdinalIgnoreCase);
            
            // Check for MCP / Plugin terminology outside of boundary test files themselves.
            Assert.DoesNotContain("ModelContextProtocol", content, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void FoundationContainsZeroArbitraryShellInvocations()
    {
        string[] allSourceFiles = Directory.GetFiles(
            Path.Combine(RepositoryRoot, "src"), 
            "*.cs", 
            SearchOption.AllDirectories);

        foreach (string file in allSourceFiles)
        {
            if (file.EndsWith("ToolTemplateValidator.cs", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string content = File.ReadAllText(file);
            // We should never invoke arbitrary shells directly
            Assert.DoesNotContain("\"cmd.exe\"", content, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("\"cmd\"", content, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("\"/c\"", content, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("\"pwsh\"", content, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("\"powershell\"", content, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("\"bash\"", content, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("\"sh\"", content, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void FoundationContainsZeroNetworkListenersOutsideIpc()
    {
        string[] allSourceFiles = Directory.GetFiles(
            Path.Combine(RepositoryRoot, "src"), 
            "*.cs", 
            SearchOption.AllDirectories);

        foreach (string file in allSourceFiles)
        {
            // We only allow NamedPipeServerStream or similar IPC, no TCP/UDP listeners in src
            string content = File.ReadAllText(file);
            Assert.DoesNotContain("TcpListener", content, StringComparison.Ordinal);
            Assert.DoesNotContain("UdpClient", content, StringComparison.Ordinal);
            Assert.DoesNotContain("HttpListener", content, StringComparison.Ordinal);
            Assert.DoesNotContain("SocketType.Stream", content, StringComparison.Ordinal);
            Assert.DoesNotContain("HttpClient", content, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void DesktopCannotReferenceWorkspaceExecutionDirectly()
    {
        string desktopRoot = Path.Combine(RepositoryRoot, "src", "Desktop", "Opure.Desktop");
        string project = File.ReadAllText(Path.Combine(desktopRoot, "Opure.Desktop.csproj"));
        
        Assert.DoesNotContain("Opure.Workspace.Execution.csproj", project, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Opure.Workspace.Containment.csproj", project, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Opure.Workspace.Windows.csproj", project, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Opure.Workspace.Service.csproj", project, StringComparison.OrdinalIgnoreCase);
    }

    private static string FindRepositoryRoot()
    {
        string? current = AppContext.BaseDirectory;
        while (current != null)
        {
            if (File.Exists(Path.Combine(current, "Opure.slnx")))
            {
                return current;
            }
            current = Path.GetDirectoryName(current);
        }
        throw new InvalidOperationException("Could not find repository root.");
    }
}
