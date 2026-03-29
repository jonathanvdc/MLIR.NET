namespace MLIR.ODS.Model.AssemblyFormat;

/// <summary>
/// An optional group of the form:
/// ( then-elements )?
/// or
/// ( then-elements ):( else-elements )?
/// </summary>
public sealed class OptionalGroup : Element
{
    /// <summary>
    /// The anchor variable that controls whether the optional group is printed.
    /// This must correspond to a variable marked with '^' inside ThenElements.
    /// </summary>
    public string AnchorName { get; }

    /// <summary>
    /// Elements that are printed when the anchor is present.
    /// </summary>
    public IReadOnlyList<Element> ThenElements { get; }

    /// <summary>
    /// Elements that are printed when the anchor is absent (if any).
    /// </summary>
    public IReadOnlyList<Element>? ElseElements { get; }

    /// <summary>
    /// Creates an optional group.
    /// </summary>
    public OptionalGroup(
        string anchorName,
        IReadOnlyList<Element> thenElements,
        IReadOnlyList<Element>? elseElements = null)
    {
        AnchorName = anchorName;
        ThenElements = thenElements;
        ElseElements = elseElements;
    }
}
