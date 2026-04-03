namespace MLIR.Semantics.Attributes.Primitives;

using System;
using System.Globalization;

/// <summary>
/// Parses and formats MLIR-style floating-point literals.
/// </summary>
public static class FloatingPointLiteralParser
{
    /// <summary>
    /// Parses a single-precision floating-point literal.
    /// </summary>
    public static bool TryParseSingle(string text, out float value)
    {
        if (!TryParse(text, true, out var parsed))
        {
            value = default;
            return false;
        }

        value = (float)parsed;
        return true;
    }

    /// <summary>
    /// Parses a double-precision floating-point literal.
    /// </summary>
    public static bool TryParseDouble(string text, out double value)
    {
        return TryParse(text, false, out value);
    }

    /// <summary>
    /// Parses a single-precision floating-point literal.
    /// </summary>
    public static float ParseSingle(string text)
    {
        if (!TryParseSingle(text, out var value))
        {
            throw new FormatException($"Invalid single-precision floating-point literal '{text}'.");
        }

        return value;
    }

    /// <summary>
    /// Parses a double-precision floating-point literal.
    /// </summary>
    public static double ParseDouble(string text)
    {
        if (!TryParseDouble(text, out var value))
        {
            throw new FormatException($"Invalid double-precision floating-point literal '{text}'.");
        }

        return value;
    }

    /// <summary>
    /// Formats a single-precision floating-point value in MLIR style.
    /// </summary>
    public static string FormatSingle(float value)
    {
        var bits = BitConverter.ToInt32(BitConverter.GetBytes(value), 0);
        return Format(value, bits < 0);
    }

    /// <summary>
    /// Formats a double-precision floating-point value in MLIR style.
    /// </summary>
    public static string FormatDouble(double value)
    {
        var bits = BitConverter.ToInt64(BitConverter.GetBytes(value), 0);
        return Format(value, bits < 0);
    }

    private static bool TryParse(string text, bool single, out double value)
    {
        value = default;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var span = text;
        var negative = false;
        if (span[0] == '+' || span[0] == '-')
        {
            negative = span[0] == '-';
            span = span.Substring(1);
            if (span.Length == 0)
            {
                return false;
            }
        }

        if (IsSpecial(span, "inf") || IsSpecial(span, "infinity"))
        {
            value = negative ? double.NegativeInfinity : double.PositiveInfinity;
            return true;
        }

        if (IsSpecial(span, "nan"))
        {
            value = double.NaN;
            return true;
        }

        if (span.Length >= 2 && span[0] == '0' && (span[1] == 'x' || span[1] == 'X'))
        {
            if (negative)
            {
                return false;
            }

            var hexDigits = span.Substring(2);
            if (hexDigits.Length == 0)
            {
                return false;
            }

            if (single)
            {
                if (hexDigits.Length > 8 || !uint.TryParse(hexDigits, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out var bits))
                {
                    return false;
                }

                value = BitConverter.ToSingle(BitConverter.GetBytes(bits), 0);
                return true;
            }

            if (hexDigits.Length > 16 || !ulong.TryParse(hexDigits, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out var doubleBits))
            {
                return false;
            }

            value = BitConverter.ToDouble(BitConverter.GetBytes(doubleBits), 0);
            return true;
        }

        if (!HasDecimalMarker(span))
        {
            return false;
        }

        if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
        {
            return false;
        }

        return true;
    }

    private static bool HasDecimalMarker(string text)
    {
        foreach (var ch in text)
        {
            if (ch == '.' || ch == 'e' || ch == 'E')
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsSpecial(string span, string text)
    {
        return string.Equals(span, text, StringComparison.OrdinalIgnoreCase);
    }

    private static string Format(double value, bool negativeZero)
    {
        if (double.IsNaN(value))
        {
            return "nan";
        }

        if (double.IsPositiveInfinity(value))
        {
            return "inf";
        }

        if (double.IsNegativeInfinity(value))
        {
            return "-inf";
        }

        if (value == 0.0)
        {
            return negativeZero ? "-0.0" : "0.0";
        }

        var text = value.ToString("R", CultureInfo.InvariantCulture);
        return text.IndexOfAny(['.', 'e', 'E']) >= 0 ? text : text + ".0";
    }
}
