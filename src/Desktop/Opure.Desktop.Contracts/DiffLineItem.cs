namespace Opure.Desktop.Contracts;

public enum DiffKind
{
    Context,
    Added,
    Deleted
}

public sealed record DiffLineItem(
    int? LineNumberOld,
    int? LineNumberNew,
    string Content,
    DiffKind Kind
);
