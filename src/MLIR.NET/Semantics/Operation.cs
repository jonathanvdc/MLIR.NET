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

    public OperationSyntax Syntax { get; }
    public string Name { get; }
    public OperationDefinition? Definition { get; }
    public IReadOnlyList<Region> Regions { get; }
    public IReadOnlyList<NamedAttribute> Attributes { get; }
    public TypeReference? TypeSignatureReference { get; }
    public IReadOnlyList<ValueReference> ResultValues { get; }
    public IReadOnlyList<ValueReference> OperandValues { get; }
    public IReadOnlyList<BlockReference> SuccessorReferences { get; }
    public IReadOnlyDictionary<string, object?> Properties { get; }
    public bool IsKnown => Definition != null;
    public string SyntaxName => Syntax.Name;

    public string DialectName
    {
        get
        {
            var separatorIndex = Name.IndexOf('.');
            return separatorIndex >= 0 ? Name.Substring(0, separatorIndex) : string.Empty;
        }
    }

    public IReadOnlyList<string> Results => GetNames(ResultValues);
    public IReadOnlyList<string> Operands => GetNames(OperandValues);
    public IReadOnlyList<string> Successors => GetLabels(SuccessorReferences);
    public RawSyntaxText? TypeSignature => Syntax.RawTypeSignature;
    public SourceLocation Location => SourceLocation.FromToken(Syntax.NameToken);

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

    public bool HasProperty(string name)
    {
        return Properties.ContainsKey(name);
    }

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
