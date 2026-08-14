using System;

namespace Opure.Patch.Contracts;

public class PreconditionFailedException : InvalidOperationException
{
    public PreconditionFailedException(string message) : base(message)
    {
    }
}
