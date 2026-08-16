using System.Collections.Generic;

namespace Opure.Runtime.Contracts.Models;

public record ModelHostResponse
{
    public string? Text { get; init; }
    public double? Confidence { get; init; }
    public IReadOnlyDictionary<string, object>? Metadata { get; init; }
    public bool IsError { get; init; }

    public static ModelHostResponse Success(string text, double? confidence = null, IReadOnlyDictionary<string, object>? metadata = null) =>
        new() { Text = text, Confidence = confidence, Metadata = metadata, IsError = false };

    public static ModelHostResponse Error(string text, double? confidence = null) =>
        new() { Text = text, Confidence = confidence, IsError = true };
}
