using System.Collections.ObjectModel;

namespace Opure.Configuration.Contracts;

public sealed class SettingValueTypeDefinition
{
    public const int MaximumDefinitionValueBytes = 1_048_576;

    public SettingValueTypeDefinition(
        SettingValueKind kind,
        int maximumEncodedBytes,
        SettingValueKind? elementKind = null,
        IEnumerable<string>? enumerationValues = null,
        IEnumerable<string>? elementEnumerationValues = null,
        decimal? minimum = null,
        decimal? maximum = null,
        int? maximumItems = null)
    {
        if (!Enum.IsDefined(kind))
        {
            throw new ArgumentOutOfRangeException(nameof(kind));
        }

        if (maximumEncodedBytes is < 1 or > MaximumDefinitionValueBytes)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumEncodedBytes));
        }

        if (elementKind is not null && !Enum.IsDefined(elementKind.Value))
        {
            throw new ArgumentOutOfRangeException(nameof(elementKind));
        }

        string[] values = (enumerationValues ?? []).ToArray();
        string[] elementValues = (elementEnumerationValues ?? []).ToArray();
        ValidateEnumeration(kind, values);
        ValidateElementKind(kind, elementKind);
        ValidateElementEnumeration(elementKind, elementValues);
        if (minimum > maximum)
        {
            throw new ArgumentException("The minimum cannot exceed the maximum.", nameof(minimum));
        }

        bool collection = kind is SettingValueKind.OrderedList or
            SettingValueKind.UnorderedSet or SettingValueKind.StringMap or
            SettingValueKind.BoundedRuleList;
        if (maximumItems is not null && (!collection || maximumItems is < 1 or > 65_536))
        {
            throw new ArgumentOutOfRangeException(nameof(maximumItems));
        }

        Kind = kind;
        MaximumEncodedBytes = maximumEncodedBytes;
        ElementKind = elementKind;
        EnumerationValues = new ReadOnlyCollection<string>(values);
        ElementEnumerationValues = new ReadOnlyCollection<string>(elementValues);
        Minimum = minimum;
        Maximum = maximum;
        MaximumItems = maximumItems;
    }

    public SettingValueKind Kind { get; }

    public int MaximumEncodedBytes { get; }

    public SettingValueKind? ElementKind { get; }

    public IReadOnlyList<string> EnumerationValues { get; }

    public IReadOnlyList<string> ElementEnumerationValues { get; }

    public decimal? Minimum { get; }

    public decimal? Maximum { get; }

    public int? MaximumItems { get; }

    private static void ValidateEnumeration(SettingValueKind kind, string[] values)
    {
        if (kind != SettingValueKind.Enumeration && values.Length != 0)
        {
            throw new ArgumentException(
                "Enumeration values are valid only for an enumeration type.",
                nameof(values));
        }

        if (kind == SettingValueKind.Enumeration &&
            (values.Length == 0 || values.Length > 256 ||
             values.Any(static value => !SettingDefinitionContract.IsStableToken(value)) ||
             values.Distinct(StringComparer.Ordinal).Count() != values.Length))
        {
            throw new ArgumentException(
                "Enumeration values must be a non-empty unique stable-token set.",
                nameof(values));
        }
    }

    private static void ValidateElementKind(
        SettingValueKind kind,
        SettingValueKind? elementKind)
    {
        bool requiresElement = kind is SettingValueKind.OrderedList or
            SettingValueKind.UnorderedSet or SettingValueKind.StringMap or
            SettingValueKind.BoundedRuleList;
        if (requiresElement != (elementKind is not null))
        {
            throw new ArgumentException(
                "Collection types require one explicit element kind and scalar types prohibit it.",
                nameof(elementKind));
        }

        if (elementKind is SettingValueKind.OrderedList or
            SettingValueKind.UnorderedSet or SettingValueKind.StringMap or
            SettingValueKind.BoundedRuleList)
        {
            throw new ArgumentException(
                "Nested collection definitions are deferred from schema revision one.",
                nameof(elementKind));
        }
    }

    private static void ValidateElementEnumeration(
        SettingValueKind? elementKind,
        string[] values)
    {
        if (elementKind != SettingValueKind.Enumeration && values.Length != 0)
        {
            throw new ArgumentException(
                "Element enumeration values require an enumeration element kind.",
                nameof(values));
        }

        if (elementKind == SettingValueKind.Enumeration &&
            (values.Length == 0 || values.Length > 256 ||
             values.Any(static value => !SettingDefinitionContract.IsStableToken(value)) ||
             values.Distinct(StringComparer.Ordinal).Count() != values.Length))
        {
            throw new ArgumentException(
                "Enumeration elements require a non-empty unique stable-token set.",
                nameof(values));
        }
    }
}
