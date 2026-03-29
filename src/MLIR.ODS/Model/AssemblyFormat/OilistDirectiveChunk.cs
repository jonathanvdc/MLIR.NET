namespace MLIR.ODS.Model.AssemblyFormat;

/// <summary>
/// oilist(`keyword` elements | `otherKeyword` elements ...)
/// </summary>
public sealed class OilistDirectiveChunk : DirectiveChunk
{
    /// <summary>
    /// The clauses that make up the oilist directive.
    /// </summary>
    public IReadOnlyList<OilistClause> Clauses { get; }

    /// <summary>
    /// Creates the oilist directive.
    /// </summary>
    public OilistDirectiveChunk(IReadOnlyList<OilistClause> clauses)
    {
        Clauses = clauses;
    }
}
