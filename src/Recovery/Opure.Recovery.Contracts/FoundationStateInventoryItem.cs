namespace Opure.Recovery.Contracts;

/// <summary>
/// Represents a single inventoried path within the owner's state directory.
/// </summary>
/// <param name="RelativePath">The path relative to the owner's root directory.</param>
/// <param name="Category">The explicitly defined state category.</param>
/// <param name="Description">A human-readable description of the state.</param>
public sealed record FoundationStateInventoryItem(
    string RelativePath,
    FoundationStateCategory Category,
    string Description
);
