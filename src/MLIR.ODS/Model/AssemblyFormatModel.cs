using MLIR.ODS.Model.AssemblyFormat;

namespace MLIR.ODS.Model;

/// <summary>
/// Represents a declarative MLIR ODS assembly format.
/// </summary>
public sealed class AssemblyFormatModel
{
    /// <summary>
    /// The sequence of elements that make up the assembly format.
    /// </summary>
    public IReadOnlyList<Element> Elements { get; }

    /// <summary>
    /// Creates a new assembly format model.
    /// </summary>
    public AssemblyFormatModel(IReadOnlyList<Element> elements)
    {
        Elements = elements;
    }
}
