using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Opure.Workspace.Contracts;
using Opure.Workspace.Contracts.Models;

namespace Opure.Workspace.Execution;

/// <summary>
/// A hybrid semantic search engine combining BM25 lexical ranking (FTS5) with Cosine Similarity vector search.
/// Merges results using Reciprocal Rank Fusion (RRF).
/// </summary>
public sealed class HybridSearchEngine : ISemanticSearchEngine
{
    private readonly SqliteConnection _connection;
    // FTS5 BM25 rank is a negative value by default where smaller (more negative) is better.
    // In Reciprocal Rank Fusion, k is typically 60.
    private const double RrfK = 60.0;

    public HybridSearchEngine(SqliteConnection connection)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
    }

    public async Task<IReadOnlyList<CodeChunk>> SearchAsync(string query, EmbeddingVector queryVector, int topK, CancellationToken cancellationToken)
    {
        // 1. Lexical Search via FTS5
        var lexicalCandidates = await PerformLexicalSearchAsync(query, topK, cancellationToken);
        
        // 2. Semantic Search via Vector Simd
        var semanticCandidates = await PerformSemanticSearchAsync(queryVector, topK, cancellationToken);

        // 3. Reciprocal Rank Fusion (RRF)
        var fusedScores = new Dictionary<long, double>();

        for (int i = 0; i < lexicalCandidates.Count; i++)
        {
            var rowId = lexicalCandidates[i];
            int rank = i + 1; // 1-indexed rank
            fusedScores[rowId] = 1.0 / (RrfK + rank);
        }

        for (int i = 0; i < semanticCandidates.Count; i++)
        {
            var rowId = semanticCandidates[i];
            int rank = i + 1; // 1-indexed rank
            double score = 1.0 / (RrfK + rank);
            
            if (fusedScores.ContainsKey(rowId))
                fusedScores[rowId] += score;
            else
                fusedScores[rowId] = score;
        }

        // Get top-K fused rowIds
        var topFusedRowIds = fusedScores
            .OrderByDescending(kvp => kvp.Value)
            .Take(topK)
            .Select(kvp => kvp.Key)
            .ToList();

        if (topFusedRowIds.Count == 0)
        {
            return Array.Empty<CodeChunk>();
        }

        // 4. Fetch the full CodeChunk records for the top-K rowIds
        return await FetchCodeChunksAsync(topFusedRowIds, cancellationToken);
    }

    private async Task<List<long>> PerformLexicalSearchAsync(string query, int fetchK, CancellationToken cancellationToken)
    {
        var rowIds = new List<long>();
        if (string.IsNullOrWhiteSpace(query))
            return rowIds;

        using var command = _connection.CreateCommand();
        // FTS5 MATCH syntax escaping: wrap in quotes to do exact phrase or simple term match.
        // For robustness, replacing double quotes with single spaces.
        string escapedQuery = "\"" + query.Replace("\"", " ") + "\"";

        command.CommandText = @"
            SELECT rowid, rank 
            FROM fts_code_chunks 
            WHERE fts_code_chunks MATCH @query 
            ORDER BY rank 
            LIMIT @fetchK;
        ";
        command.Parameters.AddWithValue("@query", escapedQuery);
        command.Parameters.AddWithValue("@fetchK", fetchK);

        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            rowIds.Add(reader.GetInt64(0));
        }

        return rowIds;
    }

    private async Task<List<long>> PerformSemanticSearchAsync(EmbeddingVector queryVector, int fetchK, CancellationToken cancellationToken)
    {
        using var command = _connection.CreateCommand();
        command.CommandText = "SELECT chunk_rowid, scale, quantized_data, float_data, is_quantized FROM vector_embeddings;";
        
        var scores = new List<(long RowId, float Score)>();

        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            long rowId = reader.GetInt64(0);
            float scale = reader.GetFloat(1);
            bool isQuantized = reader.GetInt32(4) != 0;

            float score = 0;
            if (isQuantized && queryVector.IsQuantized && !reader.IsDBNull(2))
            {
                byte[] qData = (byte[])reader.GetValue(2);
                ReadOnlySpan<sbyte> chunkVec = MemoryMarshal.Cast<byte, sbyte>(qData);
                ReadOnlySpan<sbyte> queryVec = MemoryMarshal.Cast<byte, sbyte>(queryVector.QuantizedDimensions.Span);
                // We assume scaleA is 1.0f for query or embedded into queryVector for now.
                // Using 1.0f as placeholder since VectorQuantizer uses scale. 
                // Actual scale should ideally be attached to queryVector. 
                score = SimdVectorOperations.QuantizedCosineSimilarity(chunkVec, scale, queryVec, 1.0f);
            }
            else if (!isQuantized && !queryVector.IsQuantized && !reader.IsDBNull(3))
            {
                byte[] fData = (byte[])reader.GetValue(3);
                ReadOnlySpan<float> chunkVec = MemoryMarshal.Cast<byte, float>(fData);
                score = SimdVectorOperations.CosineSimilarity(chunkVec, queryVector.Dimensions.Span);
            }
            else 
            {
                // Mismatched or missing vector formats. We could upcast or skip. Skip for simplicity.
                continue;
            }

            scores.Add((rowId, score));
        }

        // Sort descending and take top K
        scores.Sort((a, b) => b.Score.CompareTo(a.Score));

        return scores.Take(fetchK).Select(s => s.RowId).ToList();
    }

    private async Task<IReadOnlyList<CodeChunk>> FetchCodeChunksAsync(List<long> rowIds, CancellationToken cancellationToken)
    {
        if (rowIds.Count == 0) return Array.Empty<CodeChunk>();

        using var command = _connection.CreateCommand();
        
        var parameters = new string[rowIds.Count];
        for (int i = 0; i < rowIds.Count; i++)
        {
            parameters[i] = $"@p{i}";
            command.Parameters.AddWithValue(parameters[i], rowIds[i]);
        }

        string inClause = string.Join(",", parameters);
        command.CommandText = $@"
            SELECT id, chunk_id, file_path, start_line, end_line, content, language, document_hash 
            FROM code_chunks 
            WHERE id IN ({inClause});
        ";

        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        
        var chunkDict = new Dictionary<long, CodeChunk>();
        while (await reader.ReadAsync(cancellationToken))
        {
            long id = reader.GetInt64(0);
            var chunk = new CodeChunk
            {
                ChunkId = reader.GetString(1),
                FilePath = reader.GetString(2),
                StartLine = reader.GetInt32(3),
                EndLine = reader.GetInt32(4),
                Content = reader.GetString(5),
                Language = reader.GetString(6),
                DocumentHash = reader.GetString(7)
            };
            chunkDict[id] = chunk;
        }

        var sortedChunks = new List<CodeChunk>(rowIds.Count);
        foreach (var id in rowIds)
        {
            if (chunkDict.TryGetValue(id, out var chunk))
            {
                sortedChunks.Add(chunk);
            }
        }

        return sortedChunks;
    }
}
