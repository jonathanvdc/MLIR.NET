namespace MLIR.ODS.Model.AssemblyFormat;

/// <summary>
/// oilist(`keyword` elements | `otherKeyword` elements ...)
/// </summary>
public sealed class OdsAssemblyFormatOilistDirectiveChunk : OdsAssemblyFormatDirectiveChunk
{
    /// <summary>
    /// The clauses that make up the oilist directive.
    /// </summary>
    public IReadOnlyList<OdsAssemblyFormatOilistClause> Clauses { get; }

    /// <summary>
    /// Creates the oilist directive.
    /// </summary>
    public OdsAssemblyFormatOilistDirectiveChunk(IReadOnlyList<OdsAssemblyFormatOilistClause> clauses)
    {
        Clauses = clauses;
    }
}
