namespace MLIR.Text;

using MLIR.Semantics;

/// <summary>
/// Provides custom assembly binders access to the current operation and a diagnostic sink.
/// </summary>
public sealed class OperationAssemblyBindingContext
{
    private readonly Binder binder;

    internal OperationAssemblyBindingContext(Operation operation, Binder binder)
    {
        Operation = operation;
        this.binder = binder;
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
        binder.Report(new AssemblyDiagnostic(Operation.Location, message));
    }
}
