namespace MLIR.Text;

using System.Collections.Generic;
using MLIR.Semantics;

/// <summary>
/// Provides custom attribute binders a place to store semantic properties and diagnostics.
/// </summary>
public sealed class AttributeAssemblyBindingContext
{
    private readonly List<AssemblyDiagnostic> diagnostics;
    private readonly Dictionary<string, object?> properties;

    internal AttributeAssemblyBindingContext(AttributeValue attribute, List<AssemblyDiagnostic> diagnostics, Dictionary<string, object?> properties)
    {
        Attribute = attribute;
        this.diagnostics = diagnostics;
        this.properties = properties;
    }

    /// <summary>
    /// Gets the attribute value currently being interpreted.
    /// </summary>
    public AttributeValue Attribute { get; }

    /// <summary>
    /// Stores a semantic property for the current attribute.
    /// </summary>
    public void SetProperty(string name, object? value)
    {
        properties[name] = value;
    }

    /// <summary>
    /// Reports a binding diagnostic for the current attribute.
    /// </summary>
    public void Report(string message)
    {
        diagnostics.Add(new AssemblyDiagnostic(Attribute.Location, message));
    }
}
