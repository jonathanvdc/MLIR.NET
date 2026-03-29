namespace MLIR.ODS.Model.AssemblyFormat;

/// <summary>
/// Represents a declarative MLIR ODS assembly format.
/// </summary>
public sealed class OdsAssemblyFormatModel
{
    /// <summary>
    /// The sequence of elements that make up the assembly format.
    /// </summary>
    public IReadOnlyList<OdsAssemblyFormatElement> Elements { get; }

    /// <summary>
    /// Creates a new assembly format model.
    /// </summary>
    public OdsAssemblyFormatModel(IReadOnlyList<OdsAssemblyFormatElement> elements)
    {
        Elements = elements;
    }
}
