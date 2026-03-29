namespace MLIR.ODS.Model.AssemblyFormat;

/// <summary>
/// A single clause in an oilist directive.
/// </summary>
public sealed class OdsAssemblyFormatOilistClause
{
    /// <summary>
    /// The keyword that triggers this clause.
    /// </summary>
    public string Keyword { get; }

    /// <summary>
    /// Elements printed when this clause is selected.
    /// </summary>
    public IReadOnlyList<OdsAssemblyFormatOilistElement> Elements { get; }

    /// <summary>
    /// Creates an oilist clause.
    /// </summary>
    public OdsAssemblyFormatOilistClause(
        string keyword,
        IReadOnlyList<OdsAssemblyFormatOilistElement> elements)
    {
        Keyword = keyword;
        Elements = elements;
    }
}
