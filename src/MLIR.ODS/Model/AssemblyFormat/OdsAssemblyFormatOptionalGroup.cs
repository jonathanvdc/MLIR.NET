namespace MLIR.ODS.Model.AssemblyFormat;

/// <summary>
/// An optional group of the form:
/// ( then-elements )?
/// or
/// ( then-elements ):( else-elements )?
/// </summary>
public sealed class OdsAssemblyFormatOptionalGroup : OdsAssemblyFormatElement
{
    /// <summary>
    /// The anchor variable that controls whether the optional group is printed.
    /// This must correspond to a variable marked with '^' inside ThenElements.
    /// </summary>
    public string AnchorName { get; }

    /// <summary>
    /// Elements that are printed when the anchor is present.
    /// </summary>
    public IReadOnlyList<OdsAssemblyFormatElement> ThenElements { get; }

    /// <summary>
    /// Elements that are printed when the anchor is absent (if any).
    /// </summary>
    public IReadOnlyList<OdsAssemblyFormatElement>? ElseElements { get; }

    /// <summary>
    /// Creates an optional group.
    /// </summary>
    public OdsAssemblyFormatOptionalGroup(
        string anchorName,
        IReadOnlyList<OdsAssemblyFormatElement> thenElements,
        IReadOnlyList<OdsAssemblyFormatElement>? elseElements = null)
    {
        AnchorName = anchorName;
        ThenElements = thenElements;
        ElseElements = elseElements;
    }
}
