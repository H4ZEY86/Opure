using System;

namespace Opure.Workspace.Contracts;

public sealed record CommandOutputMetadata(
    bool Truncated,
    long TotalBytesRead,
    bool RedactionApplied,
    bool EncodingFaultsDetected);
