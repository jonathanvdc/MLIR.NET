namespace MLIR.Semantics;

using System;
using System.Collections.Generic;
using MLIR.Dialects;
using MLIR.Syntax;

/// <summary>
/// Represents the common semantic substrate shared by all bound operations.
/// </summary>
public abstract class Operation
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Operation"/> class.
    /// </summary>
    protected Operation(
        OperationSyntax syntax,
        string name,
        OperationDefinition? definition,
        IReadOnlyList<Region> regions,
        IReadOnlyList<NamedAttribute> attributes,
        TypeReference? typeSignatureReference,
        IReadOnlyList<ValueReference> resultValues,
        IReadOnlyList<ValueReference> operandValues,
        IReadOnlyList<BlockReference> successorReferences,
        IReadOnlyDictionary<string, object?> properties)
    {
        Syntax = syntax;
        Name = name;
        Definition = definition;
        Regions = regions;
        Attributes = attributes;
        TypeSignatureReference = typeSignatureReference;
        ResultValues = resultValues;
        OperandValues = operandValues;
        SuccessorReferences = successorReferences;
        Properties = properties;
    }

    /// <summary>
    /// Gets the concrete syntax node for the operation.
    /// </summary>
    public OperationSyntax Syntax { get; }

    /// <summary>
    /// Gets the canonical operation name without MLIR string-literal quoting.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the registered operation definition, if one exists.
    /// </summary>
    public OperationDefinition? Definition { get; }

    /// <summary>
    /// Gets the semantic regions nested under the operation.
    /// </summary>
    public IReadOnlyList<Region> Regions { get; }

    /// <summary>
    /// Gets the semantic attributes attached to the operation.
    /// </summary>
    public IReadOnlyList<NamedAttribute> Attributes { get; }

    /// <summary>
    /// Gets the semantic type reference for the raw trailing type signature, if one was recognized.
    /// </summary>
    public TypeReference? TypeSignatureReference { get; }

    /// <summary>
    /// Gets the typed SSA result references produced by the operation.
    /// </summary>
    public IReadOnlyList<ValueReference> ResultValues { get; }

    /// <summary>
    /// Gets the typed SSA operand references passed to the operation.
    /// </summary>
    public IReadOnlyList<ValueReference> OperandValues { get; }

    /// <summary>
    /// Gets the typed block successor references used by the operation.
    /// </summary>
    public IReadOnlyList<BlockReference> SuccessorReferences { get; }

    /// <summary>
    /// Gets the semantic properties interpreted from dialect-specific assembly.
    /// </summary>
    public IReadOnlyDictionary<string, object?> Properties { get; }

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
    public IReadOnlyList<string> Results => GetNames(ResultValues);

    /// <summary>
    /// Gets the SSA operands passed to the operation.
    /// </summary>
    public IReadOnlyList<string> Operands => GetNames(OperandValues);

    /// <summary>
    /// Gets the successor block labels referenced by the operation.
    /// </summary>
    public IReadOnlyList<string> Successors => GetLabels(SuccessorReferences);

    /// <summary>
    /// Gets the raw type signature text, if present.
    /// </summary>
    public RawSyntaxText? TypeSignature => Syntax.RawTypeSignature;

    /// <summary>
    /// Gets the source location of the operation name, if known.
    /// </summary>
    public SourceLocation Location => SourceLocation.FromToken(Syntax.NameToken);

    /// <summary>
    /// Determines whether the operation has an attribute with the supplied name.
    /// </summary>
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

    private static IReadOnlyList<string> GetNames(IReadOnlyList<ValueReference> values)
    {
        var names = new List<string>(values.Count);
        foreach (var value in values)
        {
            names.Add(value.Name);
        }

        return names;
    }

    private static IReadOnlyList<string> GetLabels(IReadOnlyList<BlockReference> blocks)
    {
        var labels = new List<string>(blocks.Count);
        foreach (var block in blocks)
        {
            labels.Add(block.Label);
        }

        return labels;
    }
}
