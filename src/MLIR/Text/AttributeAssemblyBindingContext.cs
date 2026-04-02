namespace MLIR.Text;

using MLIR.Semantics;
using MLIR.Syntax;

/// <summary>
/// Provides custom attribute binders access to the current attribute value and a diagnostic sink.
/// </summary>
public sealed class AttributeAssemblyBindingContext
{
    private readonly List<AssemblyDiagnostic> diagnostics;

    internal AttributeAssemblyBindingContext(AttributeValueSyntax syntax, List<AssemblyDiagnostic> diagnostics)
    {
        Syntax = syntax;
        this.diagnostics = diagnostics;
    }

    /// <summary>
    /// Gets the attribute syntax currently being interpreted.
    /// </summary>
    public AttributeValueSyntax Syntax { get; }

    /// <summary>
     /// Reports a binding diagnostic for the current attribute.
     /// </summary>
    public void Report(string message)
    {
        diagnostics.Add(new AssemblyDiagnostic(Syntax.Location, message));
    }
}
