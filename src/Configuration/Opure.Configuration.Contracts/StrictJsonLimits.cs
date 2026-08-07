namespace Opure.Configuration.Contracts;

/// <summary>
/// Strict resource limits enforced by the Project configuration JSON parser.
/// </summary>
public static class StrictJsonLimits
{
    public const int MaxFileSize = 1024 * 1024; // 1 MB
    public const int MaxDepth = 16;
    public const int MaxPropertiesPerObject = 500;
    public const int MaxArrayLength = 1000;
    public const int MaxStringLength = 4096;
}
