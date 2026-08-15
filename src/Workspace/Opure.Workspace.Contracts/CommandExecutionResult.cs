using System;

namespace Opure.Workspace.Contracts;

public sealed record CommandExecutionResult(
    int ExitCode,
    CommandOutputBuffer StandardOutput,
    CommandOutputBuffer StandardError);
