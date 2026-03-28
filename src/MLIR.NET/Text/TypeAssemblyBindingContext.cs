namespace MLIR.Text;

using System.Collections.Generic;
using MLIR.Semantics;

/// <summary>
/// Provides custom type binders a place to store semantic properties and diagnostics.
/// </summary>
public sealed class TypeAssemblyBindingContext
{
    private readonly List<AssemblyDiagnostic> diagnostics;
    private readonly Dictionary<string, object?> properties;

    internal TypeAssemblyBindingContext(TypeReference type, List<AssemblyDiagnostic> diagnostics, Dictionary<string, object?> properties)
    {
        Type = type;
        this.diagnostics = diagnostics;
        this.properties = properties;
    }

    /// <summary>
    /// Gets the type reference currently being interpreted.
    /// </summary>
    public TypeReference Type { get; }

    /// <summary>
    /// Stores a semantic property for the current type.
    /// </summary>
    public void SetProperty(string name, object? value)
    {
        properties[name] = value;
    }

    /// <summary>
    /// Reports a binding diagnostic for the current type.
    /// </summary>
    public void Report(string message)
    {
        diagnostics.Add(new AssemblyDiagnostic(Type.Location, message));
    }
}
