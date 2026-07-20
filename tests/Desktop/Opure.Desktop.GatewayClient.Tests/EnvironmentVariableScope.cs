namespace Opure.Desktop.GatewayClient.Tests;

/// <summary>
/// Sets process environment variables for the duration of a test and restores
/// their original values on disposal so global state does not leak.
/// </summary>
internal sealed class EnvironmentVariableScope : IDisposable
{
    private readonly List<(string Name, string? Original)> restore = [];

    public EnvironmentVariableScope(params (string Name, string? Value)[] variables)
    {
        ArgumentNullException.ThrowIfNull(variables);

        foreach ((string name, string? value) in variables)
        {
            restore.Add((name, Environment.GetEnvironmentVariable(name)));
            Environment.SetEnvironmentVariable(name, value);
        }
    }

    public void Dispose()
    {
        foreach ((string name, string? original) in restore)
        {
            Environment.SetEnvironmentVariable(name, original);
        }
    }
}
