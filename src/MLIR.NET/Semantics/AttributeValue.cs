namespace MLIR.Semantics;

using System.Collections.Generic;
using MLIR.Dialects;
using MLIR.Syntax;

/// <summary>
/// Represents a semantic attribute value bound from raw syntax text.
/// </summary>
public sealed class AttributeValue
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AttributeValue"/> class.
    /// </summary>
    public AttributeValue(RawSyntaxText syntax, string? name, AttributeDefinition? definition, SourceLocation location, IReadOnlyDictionary<string, object?> properties)
    {
        Syntax = syntax;
        Name = name;
        Definition = definition;
        Location = location;
        Properties = properties;
    }

    /// <summary>
    /// Gets the raw syntax text for the attribute value.
    /// </summary>
    public RawSyntaxText Syntax { get; }

    /// <summary>
    /// Gets the canonical attribute name, if one was recognized.
    /// </summary>
    public string? Name { get; }

    /// <summary>
    /// Gets the registered definition, if one exists.
    /// </summary>
    public AttributeDefinition? Definition { get; }

    /// <summary>
    /// Gets a value indicating whether the attribute value was recognized by a registered dialect.
    /// </summary>
    public bool IsKnown => Definition != null;

    /// <summary>
    /// Gets the source location of the attribute value, if known.
    /// </summary>
    public SourceLocation Location { get; }

    /// <summary>
    /// Gets semantic properties interpreted from custom assembly.
    /// </summary>
    public IReadOnlyDictionary<string, object?> Properties { get; }

    /// <summary>
    /// Gets a semantic property by name.
    /// </summary>
    public T GetProperty<T>(string name)
    {
        if (!Properties.TryGetValue(name, out var value))
        {
            throw new KeyNotFoundException($"The attribute value does not have a property named '{name}'.");
        }

        if (value is T typedValue)
        {
            return typedValue;
        }

        throw new System.InvalidCastException($"The property '{name}' on the attribute value is not a '{typeof(T).FullName}'.");
    }
}
