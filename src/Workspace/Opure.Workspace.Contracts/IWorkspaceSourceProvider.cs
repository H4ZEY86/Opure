namespace Opure.Workspace.Contracts;

public sealed record WorkspaceSourceResult(
    string ProjectId,
    long Generation,
    string LogicalPath,
    string ContentHash,
    byte[]? SourceBytes,
    bool Exists,
    string ErrorMessage = "");

public interface IWorkspaceSourceProvider
{
    WorkspaceSourceResult GetSourceBytes(
        string projectId,
        long generation,
        string logicalPath);
}
