using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Opure.Workspace.Contracts;
using Opure.Workspace.Contracts.Models;

namespace Opure.Workspace.Execution;

/// <summary>
/// A lightweight graph extractor that analyzes C# files and csproj files to build a semantic dependency graph
/// of the workspace without requiring full compilation or MSBuild evaluation.
/// </summary>
public sealed class SymbolGraphExtractor : IGraphExtractor
{
    public Task<WorkspaceGraph> ExtractGraphAsync(ITrustedWorkspaceDirectory workspace, CancellationToken ct)
    {
        workspace.EnsureExists();

        var nodes = new List<GraphNode>();
        var edges = new List<GraphEdge>();

        var allFiles = Directory.GetFiles(workspace.TrustedRoot, "*.*", SearchOption.AllDirectories)
                                .Where(f => !f.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar) &&
                                            !f.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar) &&
                                            !f.Contains(Path.DirectorySeparatorChar + ".git" + Path.DirectorySeparatorChar))
                                .ToList();

        var csprojFiles = allFiles.Where(f => f.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase)).ToList();
        var csFiles = allFiles.Where(f => f.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)).ToList();

        var projectNodes = new Dictionary<string, GraphNode>(StringComparer.OrdinalIgnoreCase);

        // 1. Process CSPROJ files
        foreach (var csproj in csprojFiles)
        {
            ct.ThrowIfCancellationRequested();
            string relativePath = GetRelativePath(workspace.TrustedRoot, csproj);
            string projectId = $"project:{relativePath}";
            
            var projNode = new GraphNode(
                Id: projectId,
                Label: Path.GetFileNameWithoutExtension(csproj),
                Kind: NodeKind.Project,
                FilePath: relativePath,
                Metadata: ImmutableDictionary<string, string>.Empty);

            nodes.Add(projNode);
            projectNodes[csproj] = projNode;

            try
            {
                var doc = XDocument.Load(csproj);
                // Extract ProjectReferences
                var projectReferences = doc.Descendants("ProjectReference")
                                           .Select(x => x.Attribute("Include")?.Value)
                                           .Where(x => !string.IsNullOrEmpty(x));

                foreach (var pref in projectReferences)
                {
                    // Resolve relative path to absolute, then back to workspace relative
                    string absoluteRefPath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(csproj)!, pref!));
                    if (absoluteRefPath.StartsWith(workspace.TrustedRoot, StringComparison.OrdinalIgnoreCase))
                    {
                        string refRelativePath = GetRelativePath(workspace.TrustedRoot, absoluteRefPath);
                        edges.Add(new GraphEdge(SourceId: projectId, TargetId: $"project:{refRelativePath}", Kind: EdgeKind.References));
                    }
                }
            }
            catch
            {
                // Ignore XML parse errors
            }
        }

        // 2. Process CS files
        foreach (var csFile in csFiles)
        {
            ct.ThrowIfCancellationRequested();
            string relativePath = GetRelativePath(workspace.TrustedRoot, csFile);
            string fileId = $"file:{relativePath}";

            var fileNode = new GraphNode(
                Id: fileId,
                Label: Path.GetFileName(csFile),
                Kind: NodeKind.File,
                FilePath: relativePath,
                Metadata: ImmutableDictionary<string, string>.Empty);
            
            nodes.Add(fileNode);

            // Link file to nearest parent project
            string? parentProject = FindParentProject(csFile, projectNodes.Keys);
            if (parentProject != null)
            {
                string projRelative = GetRelativePath(workspace.TrustedRoot, parentProject);
                edges.Add(new GraphEdge(SourceId: $"project:{projRelative}", TargetId: fileId, Kind: EdgeKind.Contains));
            }

            try
            {
                string code = File.ReadAllText(csFile);
                var syntaxTree = CSharpSyntaxTree.ParseText(code, cancellationToken: ct);
                var root = syntaxTree.GetRoot(ct);

                var declarations = root.DescendantNodes().OfType<TypeDeclarationSyntax>();

                foreach (var decl in declarations)
                {
                    string name = decl.Identifier.Text;
                    string fullNamespace = GetNamespace(decl);
                    string fullName = string.IsNullOrEmpty(fullNamespace) ? name : $"{fullNamespace}.{name}";
                    string typeId = $"type:{fullName}";

                    NodeKind typeKind = decl switch
                    {
                        ClassDeclarationSyntax => NodeKind.Class,
                        InterfaceDeclarationSyntax => NodeKind.Interface,
                        StructDeclarationSyntax => NodeKind.Struct,
                        RecordDeclarationSyntax r when r.ClassOrStructKeyword.IsKind(SyntaxKind.StructKeyword) => NodeKind.Struct,
                        RecordDeclarationSyntax => NodeKind.Class,
                        _ => NodeKind.Class
                    };

                    nodes.Add(new GraphNode(
                        Id: typeId,
                        Label: name,
                        Kind: typeKind,
                        FilePath: relativePath,
                        Metadata: ImmutableDictionary<string, string>.Empty));

                    edges.Add(new GraphEdge(SourceId: fileId, TargetId: typeId, Kind: EdgeKind.Contains));

                    // Check for inheritance and interface implementation
                    if (decl.BaseList != null)
                    {
                        foreach (var baseType in decl.BaseList.Types)
                        {
                            string baseTypeName = baseType.Type.ToString();
                            // In lightweight extraction, we assume the baseTypeName might be simple or fully qualified.
                            // To perfectly resolve it requires semantics, but we can do a naive edge for now.
                            // If it starts with 'I' and the second char is uppercase, heuristically it's an interface (unless it's a known class).
                            // We use a naive ID mapping. A more robust implementation would try to resolve the namespace.
                            string targetId = $"type:{baseTypeName}"; 
                            
                            bool isLikelyInterface = baseTypeName.Length >= 2 && baseTypeName[0] == 'I' && char.IsUpper(baseTypeName[1]);
                            EdgeKind edgeKind = isLikelyInterface ? EdgeKind.Implements : EdgeKind.Inherits;

                            edges.Add(new GraphEdge(SourceId: typeId, TargetId: targetId, Kind: edgeKind));
                        }
                    }
                }
            }
            catch
            {
                // Ignore parse errors for individual files
            }
        }

        return Task.FromResult(new WorkspaceGraph(nodes, edges));
    }

    private static string GetRelativePath(string root, string fullPath)
    {
        string rootWithSeparator = root.EndsWith(Path.DirectorySeparatorChar) ? root : root + Path.DirectorySeparatorChar;
        if (fullPath.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase))
        {
            return fullPath.Substring(rootWithSeparator.Length).Replace('\\', '/');
        }
        return fullPath.Replace('\\', '/');
    }

    private static string? FindParentProject(string filePath, IEnumerable<string> projectPaths)
    {
        string? currentDir = Path.GetDirectoryName(filePath);
        while (!string.IsNullOrEmpty(currentDir))
        {
            var match = projectPaths.FirstOrDefault(p => string.Equals(Path.GetDirectoryName(p), currentDir, StringComparison.OrdinalIgnoreCase));
            if (match != null)
                return match;
            currentDir = Path.GetDirectoryName(currentDir);
        }
        return null;
    }

    private static string GetNamespace(SyntaxNode node)
    {
        var namespaceDeclaration = node.Ancestors().OfType<BaseNamespaceDeclarationSyntax>().FirstOrDefault();
        return namespaceDeclaration?.Name.ToString() ?? string.Empty;
    }
}
