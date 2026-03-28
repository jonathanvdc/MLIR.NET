namespace MLIR.Text;

using MLIR.Semantics;

/// <summary>
/// Provides custom type binders access to the current type reference and a diagnostic sink.
/// </summary>
public sealed class TypeAssemblyBindingContext
{
    private readonly List<AssemblyDiagnostic> diagnostics;

    internal TypeAssemblyBindingContext(TypeReference type, List<AssemblyDiagnostic> diagnostics)
    {
        Type = type;
        this.diagnostics = diagnostics;
    }

    /// <summary>
    /// Gets the type reference currently being interpreted.
    /// </summary>
    public TypeReference Type { get; }

    /// <summary>
    /// Reports a binding diagnostic for the current type.
    /// </summary>
    public void Report(string message)
    {
        diagnostics.Add(new AssemblyDiagnostic(Type.Location, message));
    }
}
