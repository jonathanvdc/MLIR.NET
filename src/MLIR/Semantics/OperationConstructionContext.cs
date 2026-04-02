namespace MLIR.Semantics;

using System.Collections.Generic;
using MLIR.Dialects;
using MLIR.Syntax;

/// <summary>
/// Carries the shared semantic state needed to construct a typed operation node.
/// </summary>
public sealed class OperationConstructionContext
{
    internal OperationConstructionContext(
        OperationSyntax syntax,
        string name,
        OperationDefinition definition,
        IReadOnlyList<Region> regions,
        NamedAttributeCollection attributes,
        TypeReference? typeSignatureReference,
        IReadOnlyList<OperationResult> resultValues,
        IReadOnlyList<Value?> operandValues,
        IReadOnlyList<Block?> successors)
    {
        Syntax = syntax;
        Name = name;
        Definition = definition;
        Regions = regions;
        Attributes = attributes;
        TypeSignatureReference = typeSignatureReference;
        ResultValues = resultValues;
        OperandValues = operandValues;
        Successors = successors;
    }

    /// <summary>
    /// Gets the concrete syntax node for the operation.
    /// </summary>
    public OperationSyntax Syntax { get; }

    /// <summary>
    /// Gets the canonical operation name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the registered operation definition.
    /// </summary>
    public OperationDefinition Definition { get; }

    /// <summary>
    /// Gets the semantic regions nested under the operation.
    /// </summary>
    public IReadOnlyList<Region> Regions { get; }

    /// <summary>
    /// Gets the semantic attributes attached to the operation.
    /// </summary>
    public NamedAttributeCollection Attributes { get; }

    /// <summary>
    /// Gets the semantic type reference for the trailing type signature, if one was recognized.
    /// </summary>
    public TypeReference? TypeSignatureReference { get; }

    /// <summary>
    /// Gets the typed SSA result definitions owned by the operation.
    /// </summary>
    public IReadOnlyList<OperationResult> ResultValues { get; }

    /// <summary>
    /// Gets the typed SSA operand values in operation-definition order.
    /// </summary>
    public IReadOnlyList<Value?> OperandValues { get; }

    /// <summary>
    /// Gets the resolved successor blocks in definition order. An entry is null when the
    /// successor label could not be resolved to a block within the enclosing region.
    /// </summary>
    public IReadOnlyList<Block?> Successors { get; }

    /// <summary>
    /// Gets an attribute by name.
    /// </summary>
    public NamedAttribute GetAttribute(string name)
    {
        if (Attributes.TryGet(name, out NamedAttribute attribute))
        {
            return attribute;
        }

        throw new KeyNotFoundException($"The operation '{Name}' does not have an attribute named '{name}'.");
    }
}
