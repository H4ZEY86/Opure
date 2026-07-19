namespace Opure.Runtime.Contracts;

/// <summary>
/// Provides stable process exit categories for the Runtime executable.
/// </summary>
public enum RuntimeExitCode
{
    Success = 0,
    InvalidArguments = 10,
    StartupFailure = 20,
    ShutdownFailure = 30
}
