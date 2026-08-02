using Opure.Filesystem.Contracts;

namespace Opure.Project.Contracts;

public sealed record ProjectRootOpenPolicyDecision(
    bool IsAllowed,
    string StableCode,
    string SafeDetail);

public interface IProjectRootOpenPolicy
{
    ProjectRootOpenPolicyDecision Evaluate(
        FilesystemVolumeClass volumeClass);
}
