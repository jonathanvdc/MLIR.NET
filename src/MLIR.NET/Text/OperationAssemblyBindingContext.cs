namespace MLIR.Text;

using MLIR.Semantics;

/// <summary>
/// Provides custom assembly binders access to the current operation and a diagnostic sink.
/// </summary>
public sealed class OperationAssemblyBindingContext
{
    private readonly List<AssemblyDiagnostic> diagnostics;

    internal OperationAssemblyBindingContext(Operation operation, List<AssemblyDiagnostic> diagnostics)
    {
        Operation = operation;
        this.diagnostics = diagnostics;
    }

    /// <summary>
    /// Gets the operation currently being interpreted.
    /// </summary>
    public Operation Operation { get; }

    /// <summary>
    /// Reports a binding diagnostic for the current operation.
    /// </summary>
    public void Report(string message)
    {
        diagnostics.Add(new AssemblyDiagnostic(Operation.Location, message));
    }
}
