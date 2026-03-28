namespace MLIR.Text;

using System.Collections.Generic;
using MLIR.Semantics;

/// <summary>
/// Provides custom assembly binders a place to store semantic properties and diagnostics.
/// </summary>
public sealed class OperationAssemblyBindingContext
{
    private readonly Dictionary<string, object?> properties;
    private readonly List<AssemblyDiagnostic> diagnostics;

    internal OperationAssemblyBindingContext(Operation operation, Dictionary<string, object?> properties, List<AssemblyDiagnostic> diagnostics)
    {
        Operation = operation;
        this.properties = properties;
        this.diagnostics = diagnostics;
    }

    /// <summary>
    /// Gets the operation currently being interpreted.
    /// </summary>
    public Operation Operation { get; }

    /// <summary>
    /// Stores a semantic property for the current operation.
    /// </summary>
    public void SetProperty(string name, object? value)
    {
        properties[name] = value;
    }

    /// <summary>
    /// Reports a binding diagnostic for the current operation.
    /// </summary>
    public void Report(string message)
    {
        diagnostics.Add(new AssemblyDiagnostic(Operation, message, Operation.Location));
    }
}
