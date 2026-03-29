namespace MLIR.ODS.Model.AssemblyFormat;

/// <summary>
/// A keyword, punctuation token, or whitespace literal surrounded by backticks.
/// Examples: `(`, `,`, `->`, `\n`, `foo`.
/// </summary>
public sealed class OdsAssemblyFormatLiteralChunk : OdsAssemblyFormatChunk
{
    /// <summary>
    /// The literal value.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Creates a literal chunk.
    /// </summary>
    public OdsAssemblyFormatLiteralChunk(string value)
    {
        Value = value;
    }
}
