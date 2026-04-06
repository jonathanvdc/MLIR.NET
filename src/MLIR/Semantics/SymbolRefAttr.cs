namespace MLIR.Semantics;

using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Represents an MLIR symbol reference attribute, which identifies a symbol by name with
/// optional nested path components for referencing symbols across nested symbol tables.
/// </summary>
/// <remarks>
/// <para>
/// A flat (non-nested) symbol reference corresponds to MLIR syntax <c>@foo</c> and has only
/// a <see cref="RootReference"/> with no <see cref="NestedReferences"/>.
/// </para>
/// <para>
/// A nested symbol reference corresponds to MLIR syntax <c>@outer::@inner</c> and adds one or
/// more <see cref="NestedReferences"/> that are resolved inside the symbol table of the outer symbol.
/// </para>
/// <para>
/// Use <see cref="Operation.Resolve{TSymbol}"/> to resolve a reference starting from an operation.
/// </para>
/// </remarks>
public sealed class SymbolRefAttr
{
    /// <summary>
    /// Initializes a new flat symbol reference (no nested path).
    /// </summary>
    /// <param name="rootReference">The root symbol name (without the leading <c>@</c>).</param>
    public SymbolRefAttr(string rootReference)
        : this(rootReference, [])
    {
    }

    /// <summary>
    /// Initializes a new symbol reference with optional nested path components.
    /// </summary>
    /// <param name="rootReference">The root symbol name (without the leading <c>@</c>).</param>
    /// <param name="nestedReferences">
    /// Nested symbol names that are resolved sequentially inside the root symbol's symbol table.
    /// Pass an empty list for a flat reference.
    /// </param>
    public SymbolRefAttr(string rootReference, IReadOnlyList<string> nestedReferences)
    {
        RootReference = rootReference;
        NestedReferences = nestedReferences;
    }

    /// <summary>
    /// Gets the root symbol name (the leading component before any <c>::</c> separators).
    /// </summary>
    public string RootReference { get; }

    /// <summary>
    /// Gets the nested symbol name components that follow the root reference.
    /// Empty for a flat reference such as <c>@foo</c>.
    /// </summary>
    public IReadOnlyList<string> NestedReferences { get; }

    /// <summary>
    /// Gets a value indicating whether this is a flat (non-nested) reference with no nested components.
    /// </summary>
    public bool IsFlat => NestedReferences.Count == 0;

    /// <summary>
    /// Gets the leaf symbol name—the final component of the reference chain. For a flat reference
    /// this is the same as <see cref="RootReference"/>; for a nested reference it is the last element
    /// of <see cref="NestedReferences"/>.
    /// </summary>
    public string LeafReference =>
        NestedReferences.Count > 0
            ? NestedReferences[NestedReferences.Count - 1]
            : RootReference;

    /// <inheritdoc/>
    public override string ToString()
    {
        if (IsFlat)
        {
            return "@" + RootReference;
        }

        return "@" + RootReference + "::" + string.Join("::", NestedReferences.Select(n => "@" + n));
    }
}
