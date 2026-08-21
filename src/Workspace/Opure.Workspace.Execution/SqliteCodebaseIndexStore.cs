using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Opure.Workspace.Contracts;
using Opure.Workspace.Contracts.Models;

namespace Opure.Workspace.Execution;

/// <summary>
/// A local SQLite-based implementation of <see cref="ICodebaseIndexStore"/> leveraging FTS5 and vector storage.
/// </summary>
public sealed class SqliteCodebaseIndexStore : ICodebaseIndexStore
{
    private readonly SqliteConnection _connection;

    public SqliteCodebaseIndexStore(SqliteConnection connection)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        InitializeSchema();
    }

    private void InitializeSchema()
    {
        using var command = _connection.CreateCommand();
        // Ensure PRAGMA foreign_keys = ON; is assumed at connection level, but we can set it here or rely on the host
        
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS code_chunks (
                id INTEGER PRIMARY KEY,
                chunk_id TEXT UNIQUE NOT NULL,
                file_path TEXT NOT NULL,
                start_line INTEGER NOT NULL,
                end_line INTEGER NOT NULL,
                content TEXT NOT NULL,
                language TEXT NOT NULL,
                document_hash TEXT NOT NULL
            );

            CREATE VIRTUAL TABLE IF NOT EXISTS fts_code_chunks USING fts5(
                content,
                content='code_chunks',
                content_rowid='id',
                tokenize='trigram'
            );

            CREATE TABLE IF NOT EXISTS vector_embeddings (
                chunk_rowid INTEGER PRIMARY KEY,
                scale REAL NOT NULL,
                quantized_data BLOB,
                float_data BLOB,
                is_quantized INTEGER NOT NULL,
                FOREIGN KEY(chunk_rowid) REFERENCES code_chunks(id) ON DELETE CASCADE
            );

            CREATE TRIGGER IF NOT EXISTS fts_code_chunks_ai AFTER INSERT ON code_chunks BEGIN
                INSERT INTO fts_code_chunks(rowid, content) VALUES (new.id, new.content);
            END;

            CREATE TRIGGER IF NOT EXISTS fts_code_chunks_ad AFTER DELETE ON code_chunks BEGIN
                INSERT INTO fts_code_chunks(fts_code_chunks, rowid, content) VALUES ('delete', old.id, old.content);
            END;

            CREATE TRIGGER IF NOT EXISTS fts_code_chunks_au AFTER UPDATE ON code_chunks BEGIN
                INSERT INTO fts_code_chunks(fts_code_chunks, rowid, content) VALUES ('delete', old.id, old.content);
                INSERT INTO fts_code_chunks(rowid, content) VALUES (new.id, new.content);
            END;

            CREATE INDEX IF NOT EXISTS idx_code_chunks_file_path ON code_chunks(file_path);
        ";
        command.ExecuteNonQuery();
    }

    public async Task UpsertChunksAsync(IEnumerable<(CodeChunk Chunk, EmbeddingVector Vector)> items, CancellationToken cancellationToken)
    {
        using var transaction = _connection.BeginTransaction();

        // Use INSERT OR REPLACE so that ChunkId conflicts correctly update the underlying record,
        // and the FTS triggers keep the full-text index synchronized.
        const string insertChunkSql = @"
            INSERT OR REPLACE INTO code_chunks (chunk_id, file_path, start_line, end_line, content, language, document_hash)
            VALUES (@ChunkId, @FilePath, @StartLine, @EndLine, @Content, @Language, @DocumentHash)
            RETURNING id;
        ";

        const string insertVectorSql = @"
            INSERT OR REPLACE INTO vector_embeddings (chunk_rowid, scale, quantized_data, float_data, is_quantized)
            VALUES (@ChunkRowId, @Scale, @QuantizedData, @FloatData, @IsQuantized);
        ";

        using var chunkCommand = _connection.CreateCommand();
        chunkCommand.Transaction = transaction;
        chunkCommand.CommandText = insertChunkSql;

        var pChunkId = chunkCommand.Parameters.Add("@ChunkId", SqliteType.Text);
        var pFilePath = chunkCommand.Parameters.Add("@FilePath", SqliteType.Text);
        var pStartLine = chunkCommand.Parameters.Add("@StartLine", SqliteType.Integer);
        var pEndLine = chunkCommand.Parameters.Add("@EndLine", SqliteType.Integer);
        var pContent = chunkCommand.Parameters.Add("@Content", SqliteType.Text);
        var pLanguage = chunkCommand.Parameters.Add("@Language", SqliteType.Text);
        var pDocumentHash = chunkCommand.Parameters.Add("@DocumentHash", SqliteType.Text);

        using var vectorCommand = _connection.CreateCommand();
        vectorCommand.Transaction = transaction;
        vectorCommand.CommandText = insertVectorSql;

        var pChunkRowId = vectorCommand.Parameters.Add("@ChunkRowId", SqliteType.Integer);
        var pScale = vectorCommand.Parameters.Add("@Scale", SqliteType.Real);
        var pQuantizedData = vectorCommand.Parameters.Add("@QuantizedData", SqliteType.Blob);
        var pFloatData = vectorCommand.Parameters.Add("@FloatData", SqliteType.Blob);
        var pIsQuantized = vectorCommand.Parameters.Add("@IsQuantized", SqliteType.Integer);

        foreach (var (chunk, vector) in items)
        {
            cancellationToken.ThrowIfCancellationRequested();

            pChunkId.Value = chunk.ChunkId;
            pFilePath.Value = chunk.FilePath;
            pStartLine.Value = chunk.StartLine;
            pEndLine.Value = chunk.EndLine;
            pContent.Value = chunk.Content;
            pLanguage.Value = chunk.Language;
            pDocumentHash.Value = chunk.DocumentHash;

            var rowIdResult = await chunkCommand.ExecuteScalarAsync(cancellationToken);
            long rowId = (long)rowIdResult!;

            pChunkRowId.Value = rowId;
            pIsQuantized.Value = vector.IsQuantized ? 1 : 0;
            pScale.Value = 1.0f; 

            if (vector.IsQuantized)
            {
                pQuantizedData.Value = vector.QuantizedDimensions.ToArray();
                pFloatData.Value = DBNull.Value;
            }
            else
            {
                pQuantizedData.Value = DBNull.Value;
                // Convert float array to bytes for blob storage
                var floatBytes = new byte[vector.Dimensions.Length * sizeof(float)];
                Buffer.BlockCopy(vector.Dimensions.ToArray(), 0, floatBytes, 0, floatBytes.Length);
                pFloatData.Value = floatBytes;
            }

            await vectorCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }

    public async Task RemoveFileAsync(string filePath, CancellationToken cancellationToken)
    {
        using var command = _connection.CreateCommand();
        command.CommandText = "DELETE FROM code_chunks WHERE file_path = @FilePath;";
        command.Parameters.AddWithValue("@FilePath", filePath);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
