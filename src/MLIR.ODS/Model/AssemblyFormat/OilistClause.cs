namespace MLIR.ODS.Model.AssemblyFormat;

/// <summary>
/// A single clause in an oilist directive.
/// </summary>
public sealed class OilistClause
{
    /// <summary>
    /// The keyword that triggers this clause.
    /// </summary>
    public string Keyword { get; }

    /// <summary>
    /// Elements printed when this clause is selected.
    /// </summary>
    public IReadOnlyList<OilistElement> Elements { get; }

    /// <summary>
    /// Creates an oilist clause.
    /// </summary>
    public OilistClause(
        string keyword,
        IReadOnlyList<OilistElement> elements)
    {
        Keyword = keyword;
        Elements = elements;
    }
}
