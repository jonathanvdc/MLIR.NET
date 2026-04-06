namespace MLIR.Semantics;

using System;
using System.Collections.Generic;
using MLIR.Dialects;
using MLIR.Semantics.Attributes.Primitives;
using MLIR.Syntax;

/// <summary>
/// Represents the common semantic substrate shared by all bound operations.
/// </summary>
public abstract class Operation
{
    private readonly List<Region> regions;
    private readonly List<OperationResult> results;
    private readonly List<OpOperand> operands;
    private readonly OperandValueList operandValues;
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
        IReadOnlyList<Block?>? successors = null)
    {
        Syntax = syntax;
        Attributes = attributes ?? NamedAttributeCollection.Empty;
        TypeSignatureReference = typeSignatureReference;
        this.regions = new List<Region>(regions?.Count ?? 0);
        this.results = new List<OperationResult>(resultValues?.Count ?? 0);
        operands = new List<OpOperand>(operandValues?.Count ?? 0);
        this.operandValues = new OperandValueList(operands);
        this.successors = new List<OpSuccessor>(successors?.Count ?? 0);

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

        if (successors != null)
        {
            for (var i = 0; i < successors.Count; i++)
            {
                this.successors.Add(new OpSuccessor(this, i, successors[i]));
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
    /// Gets the typed SSA operand values for each operand slot.
    /// </summary>
    public IReadOnlyList<Value?> OperandValues => operandValues;

    /// <summary>
    /// Enumerates only the non-null operand values.
    /// </summary>
    public IEnumerable<Value> NonNullOperandValues
    {
        get
        {
            for (var i = 0; i < operands.Count; i++)
            {
                var value = operands[i].Value;
                if (value is not null)
                {
                    yield return value;
                }
            }
        }
    }

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
    public SourceLocation Location => Syntax != null ? Syntax.Location : SourceLocation.Unknown;

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
    /// Sets or removes an attribute on this operation by name.
    /// </summary>
    /// <param name="name">The declared attribute name.</param>
    /// <param name="attribute">
    /// The replacement attribute. Pass null to remove the attribute.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="name"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown if <paramref name="attribute"/> is non-null and its
    /// <see cref="NamedAttribute.Name"/> does not match <paramref name="name"/>.
    /// </exception>
    public void SetAttribute(string name, NamedAttribute? attribute)
    {
        if (name is null)
        {
            throw new ArgumentNullException(nameof(name));
        }

        if (attribute is null)
        {
            SetAttributes(Attributes.Remove(name));
            return;
        }

        if (!string.Equals(attribute.Name, name, System.StringComparison.Ordinal))
        {
            throw new ArgumentException(
                $"Expected an attribute named '{name}' but got '{attribute.Name}'.",
                nameof(attribute));
        }

        SetAttributes(Attributes.SetOrAdd(attribute));
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

    /// <summary>
    /// Returns the typed symbol with the supplied name that is directly defined by this operation,
    /// or <see langword="null"/> if this operation is not a symbol table or contains no matching symbol.
    /// </summary>
    /// <typeparam name="TSymbol">The expected type of the symbol operation.</typeparam>
    /// <param name="name">The symbol name to look up (without the leading <c>@</c>).</param>
    /// <returns>
    /// The matching symbol operation cast to <typeparamref name="TSymbol"/>, or
    /// <see langword="null"/> if the symbol is not found or has an incompatible type.
    /// </returns>
    /// <remarks>
    /// The default implementation always returns <see langword="null"/>. Operations with the
    /// <c>SymbolTable</c> ODS trait generate an override that searches their immediate region contents.
    /// </remarks>
    public virtual TSymbol? GetSymbol<TSymbol>(string name) where TSymbol : Operation => null;

    /// <summary>
    /// Walks up the parent operation chain and returns the first symbol named <paramref name="name"/>
    /// found in an enclosing symbol table, or <see langword="null"/> if no match is found.
    /// </summary>
    /// <typeparam name="TSymbol">The expected type of the symbol operation.</typeparam>
    /// <param name="name">The symbol name to look up (without the leading <c>@</c>).</param>
    /// <returns>
    /// The first matching symbol cast to <typeparamref name="TSymbol"/> in the nearest enclosing
    /// symbol table, or <see langword="null"/> if not found.
    /// </returns>
    /// <remarks>
    /// Mimics MLIR's lexical symbol resolution: starts at the nearest enclosing operation and
    /// walks upward, consulting each ancestor via <see cref="GetSymbol{TSymbol}"/>. Operations
    /// without the <c>SymbolTable</c> trait return <see langword="null"/> from
    /// <see cref="GetSymbol{TSymbol}"/> and are skipped transparently.
    /// </remarks>
    public TSymbol? LookupSymbol<TSymbol>(string name) where TSymbol : Operation
    {
        var current = ParentBlock?.ParentRegion?.ParentOperation;
        while (current != null)
        {
            var symbol = current.GetSymbol<TSymbol>(name);
            if (symbol != null)
            {
                return symbol;
            }

            current = current.ParentBlock?.ParentRegion?.ParentOperation;
        }

        return null;
    }

    /// <summary>
    /// Resolves a <see cref="SymbolRefAttr"/> by walking the parent operation chain for the root
    /// symbol and, for nested references, descending into each inner symbol table in turn.
    /// </summary>
    /// <typeparam name="TSymbol">The expected type of the resolved symbol operation.</typeparam>
    /// <param name="reference">The symbol reference to resolve.</param>
    /// <returns>
    /// The resolved symbol cast to <typeparamref name="TSymbol"/>, or <see langword="null"/> if
    /// any component of the reference is not found or has an incompatible type.
    /// </returns>
    /// <remarks>
    /// <para>
    /// For flat references (<c>@foo</c>), this is equivalent to
    /// <see cref="LookupSymbol{TSymbol}(string)"/>.
    /// </para>
    /// <para>
    /// For nested references (<c>@outer::@inner</c>), the root symbol is resolved first via
    /// <see cref="LookupSymbol{TSymbol}(string)"/> and then each subsequent component is looked
    /// up with <see cref="GetSymbol{TSymbol}"/> on the previously resolved operation.
    /// </para>
    /// </remarks>
    public TSymbol? Resolve<TSymbol>(SymbolRefAttr reference) where TSymbol : Operation
    {
        if (reference.NestedReferences.Count == 0)
        {
            return LookupSymbol<TSymbol>(reference.RootReference);
        }

        // For nested refs, find the root as any operation, then walk each nested component.
        var current = LookupSymbol<Operation>(reference.RootReference);
        if (current == null)
        {
            return null;
        }

        for (var i = 0; i < reference.NestedReferences.Count - 1; i++)
        {
            current = current.GetSymbol<Operation>(reference.NestedReferences[i]);
            if (current == null)
            {
                return null;
            }
        }

        return current.GetSymbol<TSymbol>(reference.NestedReferences[reference.NestedReferences.Count - 1]);
    }

    internal void Bind(Block parentBlock)
    {
        ParentBlock = parentBlock;
    }

    private sealed class OperandValueList : IReadOnlyList<Value?>
    {
        private readonly IReadOnlyList<OpOperand> operands;

        public OperandValueList(IReadOnlyList<OpOperand> operands)
        {
            this.operands = operands;
        }

        public int Count => operands.Count;

        public Value? this[int index] => operands[index].Value;

        public IEnumerator<Value?> GetEnumerator()
        {
            for (var i = 0; i < operands.Count; i++)
            {
                yield return operands[i].Value;
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
