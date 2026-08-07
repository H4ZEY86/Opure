using System.Collections.ObjectModel;

namespace Opure.Configuration.Contracts;

public sealed class ProjectSettingsSource
{
    public ProjectSettingsSource(
        string projectId,
        long generation,
        string contentHash,
        IDictionary<string, string> settings,
        bool exists)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectId, nameof(projectId));
        ArgumentNullException.ThrowIfNull(settings);

        ProjectId = projectId;
        Generation = generation;
        ContentHash = contentHash;
        Settings = new ReadOnlyDictionary<string, string>(
            new Dictionary<string, string>(settings, StringComparer.Ordinal));
        Exists = exists;
    }

    public string ProjectId { get; }
    public long Generation { get; }
    public string ContentHash { get; }
    public IReadOnlyDictionary<string, string> Settings { get; }
    public bool Exists { get; }
}
