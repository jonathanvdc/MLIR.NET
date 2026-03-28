namespace MLIR.Semantics;

using System.Collections.Generic;
using System;
using MLIR.Dialects;
using MLIR.Syntax;

/// <summary>
/// Represents a semantic operation bound from generic MLIR syntax.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="Operation"/> class.
/// </remarks>
/// <param name="syntax">The concrete syntax node for the operation.</param>
/// <param name="name">The canonical operation name without MLIR string-literal quoting.</param>
/// <param name="definition">The registered operation definition, if one exists.</param>
/// <param name="regions">The semantic regions nested under the operation.</param>
/// <param name="attributes">The semantic attributes attached to the operation.</param>
/// <param name="properties">The semantic properties interpreted from dialect-specific assembly.</param>
public sealed class Operation(
    OperationSyntax syntax,
    string name,
    OperationDefinition? definition,
    IReadOnlyList<Region> regions,
    IReadOnlyList<NamedAttribute> attributes,
    IReadOnlyDictionary<string, object?> properties)
{
    /// <summary>
    /// Gets the concrete syntax node for the operation.
    /// </summary>
    public OperationSyntax Syntax { get; } = syntax;

    /// <summary>
    /// Gets the canonical operation name without MLIR string-literal quoting.
    /// </summary>
    public string Name { get; } = name;

    /// <summary>
    /// Gets the registered operation definition, if one exists.
    /// </summary>
    public OperationDefinition? Definition { get; } = definition;

    /// <summary>
    /// Gets the semantic regions nested under the operation.
    /// </summary>
    public IReadOnlyList<Region> Regions { get; } = regions;

    /// <summary>
    /// Gets the semantic attributes attached to the operation.
    /// </summary>
    public IReadOnlyList<NamedAttribute> Attributes { get; } = attributes;

    /// <summary>
    /// Gets the semantic properties interpreted from dialect-specific assembly.
    /// </summary>
    public IReadOnlyDictionary<string, object?> Properties { get; } = properties;

    /// <summary>
    /// Gets a value indicating whether the operation was recognized by a registered dialect.
    /// </summary>
    public bool IsKnown => Definition != null;

    /// <summary>
    /// Gets the operation name exactly as written in the source.
    /// </summary>
    public string SyntaxName => Syntax.Name;

    /// <summary>
    /// Gets the dialect namespace portion of the operation name, if present.
    /// </summary>
    public string DialectName
    {
        get
        {
            var separatorIndex = Name.IndexOf('.');
            return separatorIndex >= 0 ? Name.Substring(0, separatorIndex) : string.Empty;
        }
    }

    /// <summary>
    /// Gets the SSA results produced by the operation.
    /// </summary>
    public IReadOnlyList<string> Results => Syntax.Results;

    /// <summary>
    /// Gets the SSA operands passed to the operation.
    /// </summary>
    public IReadOnlyList<string> Operands => Syntax.Operands;

    /// <summary>
    /// Gets the successor block labels referenced by the operation.
    /// </summary>
    public IReadOnlyList<string> Successors => Syntax.Successors;

    /// <summary>
    /// Gets the raw type signature text, if present.
    /// </summary>
    public RawSyntaxText? TypeSignature => Syntax.TypeSignature;

    /// <summary>
    /// Determines whether the operation has an attribute with the supplied name.
    /// </summary>
    /// <param name="name">The attribute name to look for.</param>
    /// <returns><see langword="true"/> when the attribute is present; otherwise, <see langword="false"/>.</returns>
    public bool HasAttribute(string name)
    {
        foreach (var attribute in Attributes)
        {
            if (string.Equals(attribute.Name, name, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Gets an attribute by name.
    /// </summary>
    /// <param name="name">The attribute name to look for.</param>
    /// <returns>The matching attribute.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when the attribute is not present.</exception>
    public NamedAttribute GetAttribute(string name)
    {
        foreach (var attribute in Attributes)
        {
            if (string.Equals(attribute.Name, name, StringComparison.Ordinal))
            {
                return attribute;
            }
        }

        throw new KeyNotFoundException($"The operation '{Name}' does not have an attribute named '{name}'.");
    }

    /// <summary>
    /// Determines whether the operation has a semantic property with the supplied name.
    /// </summary>
    public bool HasProperty(string name)
    {
        return Properties.ContainsKey(name);
    }

    /// <summary>
    /// Gets a semantic property by name.
    /// </summary>
    /// <typeparam name="T">The expected property type.</typeparam>
    /// <param name="name">The property name.</param>
    /// <returns>The property value.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when the property is not present.</exception>
    /// <exception cref="InvalidCastException">Thrown when the property has a different type than expected.</exception>
    public T GetProperty<T>(string name)
    {
        if (!Properties.TryGetValue(name, out var value))
        {
            throw new KeyNotFoundException($"The operation '{Name}' does not have a property named '{name}'.");
        }

        if (value is T typedValue)
        {
            return typedValue;
        }

        throw new InvalidCastException($"The property '{name}' on operation '{Name}' is not a '{typeof(T).FullName}'.");
    }
}
