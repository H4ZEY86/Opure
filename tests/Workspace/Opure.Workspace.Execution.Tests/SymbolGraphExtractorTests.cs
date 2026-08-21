using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Opure.Workspace.Contracts;
using Opure.Workspace.Contracts.Models;
using Opure.Workspace.Execution;

namespace Opure.Workspace.Execution.Tests;

public sealed class SymbolGraphExtractorTests : IAsyncDisposable, ITrustedWorkspaceDirectory
{
    private readonly string _tempDirectory;
    private readonly SymbolGraphExtractor _sut;

    public SymbolGraphExtractorTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "OpureTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);
        _sut = new SymbolGraphExtractor();
    }

    string ITrustedWorkspaceDirectory.TrustedRoot => _tempDirectory;
    void ITrustedWorkspaceDirectory.EnsureExists() => Directory.CreateDirectory(_tempDirectory);

    public async ValueTask DisposeAsync()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, true);
        }
        await ValueTask.CompletedTask;
    }

    [Fact]
    public async Task ExtractGraphAsync_EmptyWorkspace_ReturnsEmptyGraph()
    {
        var graph = await _sut.ExtractGraphAsync(this, CancellationToken.None);
        
        Assert.Empty(graph.Nodes);
        Assert.Empty(graph.Edges);
    }

    [Fact]
    public async Task ExtractGraphAsync_ExtractsProjectReferences()
    {
        var proj1Path = Path.Combine(_tempDirectory, "Proj1", "Proj1.csproj");
        var proj2Path = Path.Combine(_tempDirectory, "Proj2", "Proj2.csproj");
        
        Directory.CreateDirectory(Path.GetDirectoryName(proj1Path)!);
        Directory.CreateDirectory(Path.GetDirectoryName(proj2Path)!);

        File.WriteAllText(proj1Path, @"<Project><ItemGroup><ProjectReference Include=""..\Proj2\Proj2.csproj"" /></ItemGroup></Project>");
        File.WriteAllText(proj2Path, @"<Project></Project>");

        var graph = await _sut.ExtractGraphAsync(this, CancellationToken.None);
        
        var proj1Id = "project:Proj1/Proj1.csproj";
        var proj2Id = "project:Proj2/Proj2.csproj";
        
        Assert.Contains(graph.Nodes, n => n.Id == proj1Id && n.Kind == NodeKind.Project);
        Assert.Contains(graph.Nodes, n => n.Id == proj2Id && n.Kind == NodeKind.Project);
        
        Assert.Contains(graph.Edges, e => e.SourceId == proj1Id && e.TargetId == proj2Id && e.Kind == EdgeKind.References);
        
        var outgoing = graph.Outgoing[proj1Id].ToList();
        Assert.Single(outgoing);
        Assert.Equal(proj2Id, outgoing[0].TargetId);
        
        var incoming = graph.Incoming[proj2Id].ToList();
        Assert.Single(incoming);
        Assert.Equal(proj1Id, incoming[0].SourceId);
    }

    [Fact]
    public async Task ExtractGraphAsync_ExtractsClassesAndInterfaces()
    {
        var csPath = Path.Combine(_tempDirectory, "Services", "MyService.cs");
        Directory.CreateDirectory(Path.GetDirectoryName(csPath)!);

        File.WriteAllText(csPath, @"
namespace MyNamespace
{
    public interface IMyService {}
    public class MyService : IMyService, System.IDisposable {}
}");

        var graph = await _sut.ExtractGraphAsync(this, CancellationToken.None);

        var fileId = "file:Services/MyService.cs";
        var interfaceId = "type:MyNamespace.IMyService";
        var classId = "type:MyNamespace.MyService";

        Assert.Contains(graph.Nodes, n => n.Id == fileId && n.Kind == NodeKind.File);
        Assert.Contains(graph.Nodes, n => n.Id == interfaceId && n.Kind == NodeKind.Interface);
        Assert.Contains(graph.Nodes, n => n.Id == classId && n.Kind == NodeKind.Class);

        Assert.Contains(graph.Edges, e => e.SourceId == fileId && e.TargetId == interfaceId && e.Kind == EdgeKind.Contains);
        Assert.Contains(graph.Edges, e => e.SourceId == fileId && e.TargetId == classId && e.Kind == EdgeKind.Contains);
        
        Assert.Contains(graph.Edges, e => e.SourceId == classId && e.TargetId == "type:IMyService" && e.Kind == EdgeKind.Implements);
        // Note: the lightweight parser does not resolve fully qualified name for base types unless explicitly written.
        // It checks the raw text of the base type, so it will be "IMyService" not "MyNamespace.IMyService".
    }

    [Fact]
    public async Task ExtractGraphAsync_ResilientToCyclicDependencies()
    {
        var csPath1 = Path.Combine(_tempDirectory, "ClassA.cs");
        var csPath2 = Path.Combine(_tempDirectory, "ClassB.cs");
        
        File.WriteAllText(csPath1, @"public class ClassA : ClassB {}");
        File.WriteAllText(csPath2, @"public class ClassB : ClassA {}");

        // The parser should not get stuck in infinite loop or throw
        var graph = await _sut.ExtractGraphAsync(this, CancellationToken.None);
        
        Assert.Contains(graph.Nodes, n => n.Id == "type:ClassA" && n.Kind == NodeKind.Class);
        Assert.Contains(graph.Nodes, n => n.Id == "type:ClassB" && n.Kind == NodeKind.Class);
        
        Assert.Contains(graph.Edges, e => e.SourceId == "type:ClassA" && e.TargetId == "type:ClassB" && e.Kind == EdgeKind.Inherits);
        Assert.Contains(graph.Edges, e => e.SourceId == "type:ClassB" && e.TargetId == "type:ClassA" && e.Kind == EdgeKind.Inherits);
    }
}
