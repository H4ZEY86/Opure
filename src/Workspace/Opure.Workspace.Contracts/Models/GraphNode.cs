using System.Collections.Generic;

namespace Opure.Workspace.Contracts.Models;

/// <summary>
/// Represents a single entity (Project, File, Type, etc.) in the workspace semantic graph.
/// </summary>
public sealed record GraphNode(
    string Id,
    string Label,
    NodeKind Kind,
    string FilePath,
    IReadOnlyDictionary<string, string> Metadata);
