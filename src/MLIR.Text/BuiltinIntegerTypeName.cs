namespace MLIR.Text;

/// <summary>
/// Shared helpers for canonical builtin integer type names such as <c>i32</c>, <c>si64</c>, and <c>ui8</c>.
/// </summary>
public static class BuiltinIntegerTypeName
{
    /// <summary>
    /// Identifies the signedness encoded in a builtin integer type name.
    /// </summary>
    public enum Kind
    {
        /// <summary>Signless integer type.</summary>
        Signless,
        /// <summary>Signed integer type.</summary>
        Signed,
        /// <summary>Unsigned integer type.</summary>
        Unsigned
    }

    /// <summary>
    /// Attempts to interpret a canonical builtin integer type name.
    /// </summary>
    public static bool TryParse(string text, out Kind signedness, out int width)
    {
        signedness = Kind.Signless;
        width = 0;

        if (text.Length < 2)
        {
            return false;
        }

        var widthText = text;
        if (text.StartsWith("si", System.StringComparison.Ordinal))
        {
            signedness = Kind.Signed;
            widthText = text.Substring(2);
        }
        else if (text.StartsWith("ui", System.StringComparison.Ordinal))
        {
            signedness = Kind.Unsigned;
            widthText = text.Substring(2);
        }
        else if (text[0] == 'i')
        {
            widthText = text.Substring(1);
        }
        else
        {
            return false;
        }

        return int.TryParse(widthText, out width);
    }

    /// <summary>
    /// Formats a canonical builtin integer type name from width and signedness.
    /// </summary>
    public static string Format(int width, Kind signedness)
    {
        return signedness switch
        {
            Kind.Signed => "si" + width,
            Kind.Unsigned => "ui" + width,
            _ => "i" + width,
        };
    }
}
