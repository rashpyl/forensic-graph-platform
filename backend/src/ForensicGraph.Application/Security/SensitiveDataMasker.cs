namespace ForensicGraph.Application.Security;

/// <summary>
/// Masks personal identifiers for users who are not allowed to read them.
/// Keeps the first two and last two characters so a viewer can still tell
/// two records apart ("AB•••••12") without learning the full value.
/// Values of four characters or fewer are fully masked.
/// </summary>
public static class SensitiveDataMasker
{
    private const char MaskChar = '•';

    public static string Mask(string value)
    {
        if (value.Length <= 4)
        {
            return new string(MaskChar, value.Length);
        }

        return string.Concat(
            value.AsSpan(0, 2),
            new string(MaskChar, value.Length - 4),
            value.AsSpan(value.Length - 2));
    }

    public static string? MaskOrNull(string? value) => value is null ? null : Mask(value);
}
