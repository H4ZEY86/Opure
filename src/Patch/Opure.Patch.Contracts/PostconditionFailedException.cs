using System;

namespace Opure.Patch.Contracts;

public class PostconditionFailedException : Exception
{
    public PostconditionFailedException(string message) : base(message) { }

    public PostconditionFailedException(string message, string actualHash)
        : base(message)
    {
        ActualHash = actualHash;
    }

    /// <summary>
    /// The SHA-256 hex string that was actually observed in the target file
    /// after the write completed.  May be <see langword="null"/> if the hash
    /// was not available at the point of the throw.
    /// </summary>
    public string? ActualHash { get; }
}
