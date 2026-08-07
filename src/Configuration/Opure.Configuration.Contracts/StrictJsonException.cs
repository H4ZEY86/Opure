namespace Opure.Configuration.Contracts;

public sealed class StrictJsonException : Exception
{
    public StrictJsonException(string message) : base(message)
    {
        Path = "$";
        Line = 0;
        Column = 0;
    }

    public StrictJsonException(string message, (int Line, int Column) location)
        : base($"{message} at Line: {location.Line}, Column: {location.Column}")
    {
        Path = "$";
        Line = location.Line;
        Column = location.Column;
    }

    public StrictJsonException(string message, string path, (int Line, int Column) location)
        : base($"{message} at Path: '{path}', Line: {location.Line}, Column: {location.Column}")
    {
        Path = path;
        Line = location.Line;
        Column = location.Column;
    }

    public string Path { get; }
    public int Line { get; }
    public int Column { get; }
}
