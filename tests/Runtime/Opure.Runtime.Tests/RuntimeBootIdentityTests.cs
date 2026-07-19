using System.Text.RegularExpressions;
using Xunit;

namespace Opure.Runtime.Tests;

public sealed class RuntimeBootIdentityTests
{
    [Fact]
    public void Boot_identity_is_opaque_and_unique_for_each_creation()
    {
        string[] identities = Enumerable.Range(0, 100)
            .Select(_ => RuntimeBootIdentity.Create())
            .ToArray();

        Assert.Equal(identities.Length, identities.Distinct(StringComparer.Ordinal).Count());

        foreach (string identity in identities)
        {
            Assert.Matches(
                new Regex(
                    "^[0-9a-f]{32}$",
                    RegexOptions.CultureInvariant,
                    TimeSpan.FromSeconds(1)),
                identity);
        }
    }
}
