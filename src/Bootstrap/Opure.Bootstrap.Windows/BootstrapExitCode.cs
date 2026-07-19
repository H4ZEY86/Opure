namespace Opure.Bootstrap.Windows;

internal enum BootstrapExitCode
{
    Success = 0,
    InvalidArguments = 10,
    BinaryValidationFailure = 20,
    RuntimeStartFailure = 30,
    DesktopStartFailure = 40,
    ShutdownFailure = 50,
    SupervisorSafeMode = 60
}
