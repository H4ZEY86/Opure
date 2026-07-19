using System.Text.RegularExpressions;

namespace Opure.Runtime;

public static partial class RuntimeBootIdentity
{
    public static string Create()
    {
        string value = Guid.NewGuid().ToString("N");

        if (!BootIdentityPattern().IsMatch(value))
        {
            throw new InvalidOperationException(
                "Generated Runtime boot identity did not match the required opaque format.");
        }

        return value;
    }

    [GeneratedRegex("^[0-9a-f]{32}$", RegexOptions.CultureInvariant)]
    private static partial Regex BootIdentityPattern();
}
