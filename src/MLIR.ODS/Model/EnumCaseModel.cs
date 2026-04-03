namespace MLIR.ODS.Model;

/// <summary>
/// Represents a single case of an enum imported from ODS.
/// </summary>
public sealed class EnumCaseModel(string symbol, string str, long value)
{
    /// <summary>
    /// Gets the C# enumerant symbol (the identifier used in code).
    /// </summary>
    public string Symbol { get; } = symbol;

    /// <summary>
    /// Gets the string representation used in MLIR text (may differ from <see cref="Symbol"/>).
    /// </summary>
    public string Str { get; } = str;

    /// <summary>
    /// Gets the integer value of this case.
    /// For bit enum cases this is a bitmask; for regular enum cases it is a discriminator.
    /// </summary>
    public long Value { get; } = value;
}
