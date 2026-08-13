using Opure.Configuration.Contracts;
using Opure.Workspace.Contracts;
using System.Security.Cryptography;

namespace Opure.Configuration;

/// <summary>
/// Handles safe acquisition, parsing, and validation of project settings files
/// bound to an exact Workspace generation snapshot.
/// </summary>
public static class ProjectSettingsAcquirer
{
    public const string ProjectSettingsLogicalPath = ".opure/project.settings.json";

    /// <summary>
    /// Acquires and parses project settings file without opening files or checking directories directly.
    /// Binds project ID, generation, and content hash. Handles missing file as a valid absence.
    /// </summary>
    public static ProjectSettingsSource Acquire(
        IWorkspaceSourceProvider provider,
        string projectId,
        long generation)
    {
        ArgumentNullException.ThrowIfNull(provider);
        ArgumentException.ThrowIfNullOrWhiteSpace(projectId, nameof(projectId));

        // 1. Get file content from provider (never opens file system directly)
        WorkspaceSourceResult result = provider.GetSourceBytes(
            projectId,
            generation,
            ProjectSettingsLogicalPath);

        if (!result.Exists)
        {
            return new ProjectSettingsSource(
                projectId,
                generation,
                string.Empty,
                new Dictionary<string, string>(),
                exists: false);
        }

        if (!string.IsNullOrEmpty(result.ErrorMessage))
        {
            throw new InvalidOperationException(
                $"Project settings acquisition failed: {result.ErrorMessage}");
        }

        byte[] contentBytes = result.SourceBytes
            ?? throw new InvalidOperationException("Project settings content bytes are null.");

        try
        {
            // 2. Parse UTF-8 bytes strictly (limits size, depth, properties, strings, numbers, duplicates)
            StrictJsonNode root = StrictJsonParser.Parse(contentBytes);

            // 3. Validate against local schema registry (fails remote/file refs)
            string canonicalJson = root.ToCanonicalJson();
            LocalSchemaRegistry.Validate("opure.project-settings/1", canonicalJson);

            // 4. Extract individual setting values
            Dictionary<string, string> settingsDict = [];
            if (root is StrictJsonObject obj &&
                obj.Properties.TryGetValue("settings", out StrictJsonNode? settingsNode) &&
                settingsNode is StrictJsonObject settingsObj)
            {
                foreach (KeyValuePair<string, StrictJsonNode> kvp in settingsObj.Properties)
                {
                    settingsDict.Add(kvp.Key, kvp.Value.ToCanonicalJson());
                }
            }

            return new ProjectSettingsSource(
                projectId,
                generation,
                result.ContentHash,
                settingsDict,
                exists: true);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(contentBytes);
        }
    }
}
