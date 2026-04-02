namespace MLIR.Semantics;

using System.Collections.Generic;
using System.Linq;
using MLIR.Dialects;
using MLIR.Syntax;

/// <summary>
/// Represents the common semantic substrate shared by all bound operations.
/// </summary>
public abstract class Operation
{
    private readonly List<Region> regions;
    private readonly List<OperationResult> results;
    private readonly List<OpOperand> operands;
    private readonly List<OpSuccessor> successors;

    /// <summary>
    /// Initializes a new instance of the <see cref="Operation"/> class.
    /// </summary>
    protected Operation(
        OperationSyntax? syntax,
        IReadOnlyList<Region>? regions = null,
        NamedAttributeCollection? attributes = null,
        TypeReference? typeSignatureReference = null,
        IReadOnlyList<OperationResult>? resultValues = null,
        IReadOnlyList<Value?>? operandValues = null,
        IReadOnlyList<BlockReference>? successorReferences = null)
    {
        Syntax = syntax;
        Attributes = attributes ?? NamedAttributeCollection.Empty;
        TypeSignatureReference = typeSignatureReference;
        this.regions = new List<Region>(regions?.Count ?? 0);
        this.results = new List<OperationResult>(resultValues?.Count ?? 0);
        operands = new List<OpOperand>(operandValues?.Count ?? 0);
        this.successors = new List<OpSuccessor>(successorReferences?.Count ?? 0);

        if (regions != null)
        {
            foreach (var region in regions)
            {
                AttachRegion(region, invalidateSyntax: false);
            }
        }

        if (resultValues != null)
        {
            for (var i = 0; i < resultValues.Count; i++)
            {
                var result = resultValues[i];
                result.Bind(this, i);
                this.results.Add(result);
            }
        }

        if (operandValues != null)
        {
            for (var i = 0; i < operandValues.Count; i++)
            {
                operands.Add(new OpOperand(this, i, operandValues[i]));
            }
        }

        if (successorReferences != null)
        {
            for (var i = 0; i < successorReferences.Count; i++)
            {
                successors.Add(new OpSuccessor(this, i, successorReferences[i]));
            }
        }
    }

    /// <summary>
    /// Gets or sets the concrete syntax node for the operation, or null if this is a synthetic operation with no corresponding source text.
    /// </summary>
    public OperationSyntax? Syntax { get; private set; }

    /// <summary>
    /// Gets the canonical operation name without MLIR string-literal quoting.
    /// </summary>
    public abstract string Name { get; }

    /// <summary>
    /// Gets the registered operation definition, if one exists.
    /// </summary>
    public abstract OperationDefinition? Definition { get; }

    /// <summary>
    /// Gets the parent block that owns this operation, if any.
    /// </summary>
    public Block? ParentBlock { get; private set; }

    /// <summary>
    /// Gets the semantic regions nested under the operation.
    /// </summary>
    public IReadOnlyList<Region> Regions => regions;

    /// <summary>
    /// Gets the semantic attributes attached to the operation.
    /// </summary>
    public NamedAttributeCollection Attributes { get; private set; }

    /// <summary>
    /// Gets the semantic type reference for the raw trailing type signature, if one was recognized.
    /// </summary>
    public TypeReference? TypeSignatureReference { get; private set; }

    /// <summary>
    /// Gets the typed SSA results produced by the operation.
    /// </summary>
    public IReadOnlyList<OperationResult> Results => results;

    /// <summary>
    /// Gets the operand slots owned by the operation.
    /// </summary>
    public IReadOnlyList<OpOperand> Operands => operands;

    /// <summary>
    /// Gets the typed SSA operand values that are currently present.
    /// </summary>
    public IReadOnlyList<Value> OperandValues => operands.Where(static operand => operand.Value is not null).Select(static operand => operand.Value!).ToArray();

    /// <summary>
    /// Gets the successor slots owned by the operation.
    /// </summary>
    public IReadOnlyList<OpSuccessor> Successors => successors;

    /// <summary>
    /// Gets a value indicating whether the operation was recognized by a registered dialect.
    /// </summary>
    public bool IsKnown => Definition != null;

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
    /// Gets the source location of the operation name, if known.
    /// </summary>
    public SourceLocation Location => Syntax != null ? SourceLocation.FromToken(Syntax.NameToken) : SourceLocation.Unknown;

    /// <summary>
    /// Determines whether the operation has an attribute with the supplied name.
    /// </summary>
    public bool HasAttribute(string name) => Attributes.Contains(name);

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

    /// <summary>
    /// Sets the value of an operand slot.
    /// </summary>
    public void SetOperand(int index, Value? value)
    {
        operands[index].Value = value;
    }

    /// <summary>
    /// Sets the block of a successor slot.
    /// </summary>
    public void SetSuccessor(int index, Block? block)
    {
        successors[index].Block = block;
    }

    /// <summary>
    /// Replaces the region list attached to this operation.
    /// </summary>
    public void SetRegions(IReadOnlyList<Region> newRegions)
    {
        regions.Clear();
        foreach (var region in newRegions)
        {
            regions.Add(region);
            region.Bind(this);
        }

        InvalidateSyntax();
    }

    /// <summary>
    /// Adds a region to this operation.
    /// </summary>
    public void AddRegion(Region region)
    {
        AttachRegion(region, invalidateSyntax: true);
    }

    private void AttachRegion(Region region, bool invalidateSyntax)
    {
        regions.Add(region);
        region.Bind(this);
        if (invalidateSyntax)
        {
            InvalidateSyntax();
        }
    }

    /// <summary>
    /// Replaces the attribute collection attached to this operation.
    /// </summary>
    public void SetAttributes(NamedAttributeCollection attributes)
    {
        Attributes = attributes;
        InvalidateSyntax();
    }

    /// <summary>
    /// Replaces the trailing type signature reference attached to this operation.
    /// </summary>
    public void SetTypeSignatureReference(TypeReference? typeSignatureReference)
    {
        TypeSignatureReference = typeSignatureReference;
        InvalidateSyntax();
    }

    /// <summary>
    /// Invalidates any cached syntax for this operation and its ancestors.
    /// </summary>
    public void InvalidateSyntax()
    {
        Syntax = null;
        ParentBlock?.InvalidateSyntax();
    }

    internal void Bind(Block parentBlock)
    {
        ParentBlock = parentBlock;
    }
}
