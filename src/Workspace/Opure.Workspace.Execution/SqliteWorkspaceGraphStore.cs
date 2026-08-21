using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Opure.Workspace.Contracts;
using Opure.Workspace.Contracts.Models;

namespace Opure.Workspace.Execution;

public sealed class SqliteWorkspaceGraphStore : IWorkspaceGraphStore
{
    private readonly SqliteConnection _connection;

    public SqliteWorkspaceGraphStore(SqliteConnection connection)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        InitializeSchema();
    }

    private void InitializeSchema()
    {
        using var command = _connection.CreateCommand();
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS graph_nodes (
                id TEXT PRIMARY KEY,
                label TEXT NOT NULL,
                kind INTEGER NOT NULL,
                file_path TEXT NOT NULL,
                metadata_json TEXT
            );

            CREATE TABLE IF NOT EXISTS graph_edges (
                source_id TEXT NOT NULL,
                target_id TEXT NOT NULL,
                kind INTEGER NOT NULL,
                PRIMARY KEY(source_id, target_id, kind),
                FOREIGN KEY(source_id) REFERENCES graph_nodes(id) ON DELETE CASCADE,
                FOREIGN KEY(target_id) REFERENCES graph_nodes(id) ON DELETE CASCADE
            );

            CREATE INDEX IF NOT EXISTS idx_edges_source ON graph_edges(source_id);
            CREATE INDEX IF NOT EXISTS idx_edges_target ON graph_edges(target_id);
        ";
        command.ExecuteNonQuery();
    }

    public async Task SaveGraphAsync(WorkspaceGraph graph, CancellationToken cancellationToken)
    {
        await ClearGraphAsync(cancellationToken);

        using var transaction = _connection.BeginTransaction();

        const string insertNodeSql = @"
            INSERT INTO graph_nodes (id, label, kind, file_path, metadata_json)
            VALUES (@Id, @Label, @Kind, @FilePath, @MetadataJson);
        ";

        const string insertEdgeSql = @"
            INSERT INTO graph_edges (source_id, target_id, kind)
            VALUES (@SourceId, @TargetId, @Kind);
        ";

        using var nodeCommand = _connection.CreateCommand();
        nodeCommand.Transaction = transaction;
        nodeCommand.CommandText = insertNodeSql;

        var pNodeId = nodeCommand.Parameters.Add("@Id", SqliteType.Text);
        var pNodeLabel = nodeCommand.Parameters.Add("@Label", SqliteType.Text);
        var pNodeKind = nodeCommand.Parameters.Add("@Kind", SqliteType.Integer);
        var pNodeFilePath = nodeCommand.Parameters.Add("@FilePath", SqliteType.Text);
        var pNodeMetadata = nodeCommand.Parameters.Add("@MetadataJson", SqliteType.Text);

        foreach (var node in graph.Nodes)
        {
            cancellationToken.ThrowIfCancellationRequested();
            pNodeId.Value = node.Id;
            pNodeLabel.Value = node.Label;
            pNodeKind.Value = (int)node.Kind;
            pNodeFilePath.Value = node.FilePath;
            
            if (node.Metadata == null || node.Metadata.Count == 0)
            {
                pNodeMetadata.Value = DBNull.Value;
            }
            else
            {
                pNodeMetadata.Value = JsonSerializer.Serialize(node.Metadata, GraphMetadataJsonContext.Default.IReadOnlyDictionaryStringString);
            }

            await nodeCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        using var edgeCommand = _connection.CreateCommand();
        edgeCommand.Transaction = transaction;
        edgeCommand.CommandText = insertEdgeSql;

        var pEdgeSourceId = edgeCommand.Parameters.Add("@SourceId", SqliteType.Text);
        var pEdgeTargetId = edgeCommand.Parameters.Add("@TargetId", SqliteType.Text);
        var pEdgeKind = edgeCommand.Parameters.Add("@Kind", SqliteType.Integer);

        foreach (var edge in graph.Edges)
        {
            cancellationToken.ThrowIfCancellationRequested();
            pEdgeSourceId.Value = edge.SourceId;
            pEdgeTargetId.Value = edge.TargetId;
            pEdgeKind.Value = (int)edge.Kind;
            await edgeCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<WorkspaceGraph> LoadGraphAsync(CancellationToken cancellationToken)
    {
        var nodes = new List<GraphNode>();
        var edges = new List<GraphEdge>();

        using (var nodeCmd = _connection.CreateCommand())
        {
            nodeCmd.CommandText = "SELECT id, label, kind, file_path, metadata_json FROM graph_nodes";
            using var reader = await nodeCmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                nodes.Add(ParseNode(reader));
            }
        }

        using (var edgeCmd = _connection.CreateCommand())
        {
            edgeCmd.CommandText = "SELECT source_id, target_id, kind FROM graph_edges";
            using var reader = await edgeCmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                edges.Add(ParseEdge(reader));
            }
        }

        return new WorkspaceGraph(nodes, edges);
    }

    public async Task<WorkspaceGraph> GetNeighborhoodAsync(string nodeId, int maxDepth, CancellationToken cancellationToken)
    {
        const string cte = @"
            WITH RECURSIVE neighborhood(id, depth) AS (
                SELECT @StartId, 0
                UNION ALL
                SELECT e.target_id, n.depth + 1
                FROM neighborhood n
                JOIN graph_edges e ON n.id = e.source_id
                WHERE n.depth < @MaxDepth
                UNION ALL
                SELECT e.source_id, n.depth + 1
                FROM neighborhood n
                JOIN graph_edges e ON n.id = e.target_id
                WHERE n.depth < @MaxDepth
            )
        ";

        var nodes = new List<GraphNode>();
        using (var nodeCmd = _connection.CreateCommand())
        {
            nodeCmd.CommandText = cte + @"
                SELECT DISTINCT gn.id, gn.label, gn.kind, gn.file_path, gn.metadata_json 
                FROM graph_nodes gn
                JOIN neighborhood n ON gn.id = n.id;
            ";
            nodeCmd.Parameters.AddWithValue("@StartId", nodeId);
            nodeCmd.Parameters.AddWithValue("@MaxDepth", maxDepth);

            using var reader = await nodeCmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                nodes.Add(ParseNode(reader));
            }
        }

        var edges = new List<GraphEdge>();
        using (var edgeCmd = _connection.CreateCommand())
        {
            edgeCmd.CommandText = cte + @"
                SELECT DISTINCT e.source_id, e.target_id, e.kind
                FROM graph_edges e
                WHERE e.source_id IN (SELECT id FROM neighborhood)
                  AND e.target_id IN (SELECT id FROM neighborhood);
            ";
            edgeCmd.Parameters.AddWithValue("@StartId", nodeId);
            edgeCmd.Parameters.AddWithValue("@MaxDepth", maxDepth);

            using var reader = await edgeCmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                edges.Add(ParseEdge(reader));
            }
        }

        return new WorkspaceGraph(nodes, edges);
    }

    public async Task<IReadOnlyList<GraphNode>> GetDownstreamDependentsAsync(string nodeId, CancellationToken cancellationToken)
    {
        const string sql = @"
            WITH RECURSIVE dependents(id) AS (
                SELECT e.source_id
                FROM graph_edges e
                WHERE e.target_id = @StartId
                UNION ALL
                SELECT e.source_id
                FROM dependents d
                JOIN graph_edges e ON d.id = e.target_id
            )
            SELECT DISTINCT gn.id, gn.label, gn.kind, gn.file_path, gn.metadata_json 
            FROM graph_nodes gn
            JOIN dependents d ON gn.id = d.id;
        ";

        var nodes = new List<GraphNode>();
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue("@StartId", nodeId);

        using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            nodes.Add(ParseNode(reader));
        }

        return nodes;
    }

    public async Task ClearGraphAsync(CancellationToken cancellationToken)
    {
        using var command = _connection.CreateCommand();
        command.CommandText = @"
            DELETE FROM graph_edges;
            DELETE FROM graph_nodes;
        ";
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static GraphNode ParseNode(SqliteDataReader reader)
    {
        string id = reader.GetString(0);
        string label = reader.GetString(1);
        NodeKind kind = (NodeKind)reader.GetInt32(2);
        string filePath = reader.GetString(3);
        
        IReadOnlyDictionary<string, string> metadata;
        if (reader.IsDBNull(4))
        {
            metadata = new Dictionary<string, string>();
        }
        else
        {
            string json = reader.GetString(4);
            metadata = JsonSerializer.Deserialize(json, GraphMetadataJsonContext.Default.IReadOnlyDictionaryStringString) 
                       ?? new Dictionary<string, string>();
        }

        return new GraphNode(id, label, kind, filePath, metadata);
    }

    private static GraphEdge ParseEdge(SqliteDataReader reader)
    {
        string sourceId = reader.GetString(0);
        string targetId = reader.GetString(1);
        EdgeKind kind = (EdgeKind)reader.GetInt32(2);

        return new GraphEdge(sourceId, targetId, kind);
    }
}
