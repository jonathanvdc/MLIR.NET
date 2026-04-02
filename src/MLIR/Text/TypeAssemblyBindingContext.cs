namespace MLIR.Text;

using MLIR.Semantics;
using MLIR.Syntax;

/// <summary>
/// Provides custom type binders access to the current type reference and a diagnostic sink.
/// </summary>
public sealed class TypeAssemblyBindingContext
{
    private readonly List<AssemblyDiagnostic> diagnostics;

    internal TypeAssemblyBindingContext(TypeSyntax syntax, List<AssemblyDiagnostic> diagnostics)
    {
        Syntax = syntax;
        this.diagnostics = diagnostics;
    }

    /// <summary>
    /// Gets the type syntax currently being interpreted.
    /// </summary>
    public TypeSyntax Syntax { get; }

    /// <summary>
    /// Reports a binding diagnostic for the current type.
    /// </summary>
    public void Report(string message)
    {
        diagnostics.Add(new AssemblyDiagnostic(Syntax.Location, message));
    }
}
