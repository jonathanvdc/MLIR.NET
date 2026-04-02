namespace MLIR.Semantics;

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
    protected Operation(OperationSyntax? syntax)
    {
        Syntax = syntax;
    }

    /// <summary>
    /// Gets the concrete syntax node for the operation, or null if this is a synthetic operation with no corresponding source text.
    /// </summary>
    public OperationSyntax? Syntax { get; }

    /// <summary>
    /// Gets the canonical operation name without MLIR string-literal quoting.
    /// </summary>
    public abstract string Name { get; }

    /// <summary>
    /// Gets the registered operation definition, if one exists.
    /// </summary>
    public abstract OperationDefinition? Definition { get; }

    /// <summary>
    /// Gets the semantic regions nested under the operation.
    /// </summary>
    public abstract IReadOnlyList<Region> Regions { get; }

    /// <summary>
    /// Gets the semantic attributes attached to the operation.
    /// </summary>
    public abstract NamedAttributeCollection Attributes { get; }

    /// <summary>
    /// Gets the semantic type reference for the raw trailing type signature, if one was recognized.
    /// </summary>
    public abstract TypeReference? TypeSignatureReference { get; }

    /// <summary>
    /// Gets the typed SSA result references produced by the operation.
    /// </summary>
    public abstract IReadOnlyList<OperationResult> ResultValues { get; }

    /// <summary>
    /// Gets the typed SSA operand references passed to the operation.
    /// </summary>
    public abstract IReadOnlyList<Value> OperandValues { get; }

    /// <summary>
    /// Gets the typed block successor references used by the operation.
    /// </summary>
    public abstract IReadOnlyList<BlockReference> SuccessorReferences { get; }

    /// <summary>
    /// Gets a value indicating whether the operation was recognized by a registered dialect.
    /// </summary>
    public bool IsKnown => Definition != null;

    /// <summary>
    /// Gets the operation name exactly as written in the source, or null if this is a synthetic operation with no corresponding source text.
    /// </summary>
    public string? SyntaxName => Syntax?.Name;

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
    /// Gets the source location of the operation name, if known.
    /// </summary>
    public SourceLocation Location => Syntax != null ? SourceLocation.FromToken(Syntax.NameToken) : SourceLocation.Unknown;

    /// <summary>
    /// Rewrites this operation's children using the given rewriter and returns a new operation
    /// if any children changed, or this operation if nothing changed.
    /// </summary>
    /// <remarks>
    /// Implement this method in concrete operation subclasses to participate in traversal
    /// performed by <see cref="SemanticRewriter.VisitOperation"/>. Operations with no traversable
    /// children should return <c>this</c> unchanged.
    /// </remarks>
    /// <param name="rewriter">The rewriter to use when visiting children.</param>
    /// <returns>A rewritten operation, or this operation if nothing changed.</returns>
    public abstract Operation RewriteChildren(SemanticRewriter rewriter);

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

    private static IReadOnlyList<string> GetNames<TValue>(IReadOnlyList<TValue> values)
        where TValue : Value
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
