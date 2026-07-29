namespace Opure.Observability.Contracts;

public enum OperationalLogAttributeKind
{
    String = 0,
    Integer = 1,
    FloatingPoint = 2,
    Boolean = 3
}
public sealed class OperationalLogAttribute
{
    private OperationalLogAttribute(
        string name,
        OperationalLogAttributeKind kind,
        string? stringValue,
        long integerValue,
        double floatingPointValue,
        bool booleanValue)
    {
        OperationalLogContract.ValidateAttributeName(name);

        Name = name;
        Kind = kind;
        StringValue = stringValue;
        IntegerValue = integerValue;
        FloatingPointValue = floatingPointValue;
        BooleanValue = booleanValue;
    }

    public string Name { get; }

    public OperationalLogAttributeKind Kind { get; }

    public string? StringValue { get; }

    public long IntegerValue { get; }

    public double FloatingPointValue { get; }

    public bool BooleanValue { get; }

    public static OperationalLogAttribute String(string name, string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return new OperationalLogAttribute(
            name,
            OperationalLogAttributeKind.String,
            value,
            integerValue: 0,
            floatingPointValue: 0,
            booleanValue: false);
    }

    public static OperationalLogAttribute Integer(string name, long value)
    {
        return new OperationalLogAttribute(
            name,
            OperationalLogAttributeKind.Integer,
            stringValue: null,
            value,
            floatingPointValue: 0,
            booleanValue: false);
    }

    public static OperationalLogAttribute FloatingPoint(
        string name,
        double value)
    {
        if (!double.IsFinite(value))
        {
            throw new ArgumentOutOfRangeException(
                nameof(value),
                "Operational log floating-point values must be finite.");
        }

        return new OperationalLogAttribute(
            name,
            OperationalLogAttributeKind.FloatingPoint,
            stringValue: null,
            integerValue: 0,
            value,
            booleanValue: false);
    }

    public static OperationalLogAttribute Boolean(string name, bool value)
    {
        return new OperationalLogAttribute(
            name,
            OperationalLogAttributeKind.Boolean,
            stringValue: null,
            integerValue: 0,
            floatingPointValue: 0,
            value);
    }

    public static bool TryCreate(
        string name,
        object? value,
        out OperationalLogAttribute? attribute)
    {
        attribute = value switch
        {
            string text => String(name, text),
            byte number => Integer(name, number),
            short number => Integer(name, number),
            int number => Integer(name, number),
            long number => Integer(name, number),
            float number when float.IsFinite(number) =>
                FloatingPoint(name, number),
            double number when double.IsFinite(number) =>
                FloatingPoint(name, number),
            bool flag => Boolean(name, flag),
            _ => null
        };

        return attribute is not null;
    }
}
