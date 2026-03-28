namespace MLIR.Text;

using MLIR.Semantics;

/// <summary>
/// Provides custom attribute binders access to the current attribute value and a diagnostic sink.
/// </summary>
public sealed class AttributeAssemblyBindingContext
{
    private readonly List<AssemblyDiagnostic> diagnostics;

    internal AttributeAssemblyBindingContext(AttributeValue attribute, List<AssemblyDiagnostic> diagnostics)
    {
        Attribute = attribute;
        this.diagnostics = diagnostics;
    }

    /// <summary>
    /// Gets the attribute value currently being interpreted.
    /// </summary>
    public AttributeValue Attribute { get; }

    /// <summary>
    /// Reports a binding diagnostic for the current attribute.
    /// </summary>
    public void Report(string message)
    {
        diagnostics.Add(new AssemblyDiagnostic(Attribute.Location, message));
    }
}
