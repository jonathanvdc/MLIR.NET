namespace MLIR.ODS.Model.AssemblyFormat;

/// <summary>
/// custom&lt;Name&gt;(params...)
/// Params may be variables, type(...) directives, attr-dict / prop-dict,
/// string literals of C++ code, and ref(...) wrappers.
/// </summary>
public sealed class CustomDirectiveChunk : DirectiveChunk
{
    /// <summary>
    /// The name of the custom directive.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The parameters passed to the custom directive.
    /// </summary>
    public IReadOnlyList<DirectiveOperand> Parameters { get; }

    /// <summary>
    /// Creates the custom directive.
    /// </summary>
    public CustomDirectiveChunk(
        string name,
        IReadOnlyList<DirectiveOperand> parameters)
    {
        Name = name;
        Parameters = parameters;
    }
}
