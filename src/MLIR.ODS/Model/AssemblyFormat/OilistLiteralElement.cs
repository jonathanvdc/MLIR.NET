namespace MLIR.ODS.Model.AssemblyFormat;

/// <summary>
/// A literal element in an oilist clause.
/// </summary>
public sealed class OilistLiteralElement : OilistElement
{
    /// <summary>
    /// The literal value.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Creates a literal oilist element.
    /// </summary>
    public OilistLiteralElement(string value)
    {
        Value = value;
    }
}
