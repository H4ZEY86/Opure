using System;

namespace Opure.Workspace.Boundaries;

public enum ValidationResultStatus
{
    Success,
    ValidationFailure,
    DevicePathCollision,
    SymlinkDetected,
    HardLinkSubstitution,
    UnicodeCollision,
    SourceDrift
}

public readonly struct ValidationResult
{
    public ValidationResultStatus Status { get; }
    public string? Message { get; }
    public string? Path { get; }
    public WorkspaceBoundary? Boundary { get; }

    public bool IsSuccess => Status == ValidationResultStatus.Success;

    private ValidationResult(ValidationResultStatus status, string? message = null, string? path = null, WorkspaceBoundary? boundary = null)
    {
        Status = status;
        Message = message;
        Path = path;
        Boundary = boundary;
    }

    public static ValidationResult Success(WorkspaceBoundary boundary) => new(ValidationResultStatus.Success, boundary: boundary);
    public static ValidationResult Fail(ValidationResultStatus status, string message, string path) => new(status, message, path);
}
