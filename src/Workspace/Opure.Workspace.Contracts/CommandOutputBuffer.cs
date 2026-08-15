using System;

namespace Opure.Workspace.Contracts;

public sealed record CommandOutputBuffer(
    string Content,
    CommandOutputMetadata Metadata);
