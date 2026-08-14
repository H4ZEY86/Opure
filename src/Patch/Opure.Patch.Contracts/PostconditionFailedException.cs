using System;

namespace Opure.Patch.Contracts;

public class PostconditionFailedException : Exception
{
    public PostconditionFailedException(string message) : base(message) { }
}
