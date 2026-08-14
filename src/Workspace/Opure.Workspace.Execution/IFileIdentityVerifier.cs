using System.Threading.Tasks;

namespace Opure.Workspace.Execution;

public interface IFileIdentityVerifier
{
    Task VerifyPreconditionsAsync(
        string workspaceRootPath,
        string logicalPath,
        bool expectedExists,
        long expectedLength,
        string expectedSha256);
}
