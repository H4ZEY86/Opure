namespace Opure.Desktop.Contracts;

/// <summary>
/// Supplies a framework-neutral shell projection without owning domain state.
/// </summary>
public interface IDesktopShellStateSource
{
    /// <summary>
    /// Gets the current immutable shell projection.
    /// </summary>
    DesktopShellSnapshot GetCurrent();
}
