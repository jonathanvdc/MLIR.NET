namespace MLIR.Semantics;

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using MLIR.Semantics.Attributes.Primitives;

/// <summary>
/// Base class for operations that carry the MLIR <c>SymbolTable</c> ODS trait.
/// </summary>
/// <remarks>
/// <para>
/// Operations that inherit from this class automatically maintain a lazily-built O(1) dictionary
/// of the symbols that are directly contained in their first region. The dictionary is
/// invalidated—and rebuilt on the next access—whenever child operations, blocks, or regions
/// are added or removed (via the <see cref="InvalidateSyntax"/> override).
/// </para>
/// <para>
/// When the code generator encounters an operation with the ODS <c>SymbolTable</c> trait it
/// emits a class that inherits from <see cref="SymbolTableOperation"/> instead of
/// <see cref="Operation"/>, so all symbol-management logic lives here rather than in the
/// generated class body.
/// </para>
/// <para>
/// Child operations that implement <see cref="ISymbolOp"/> are indexed via the interface.
/// Untyped or generic child operations that carry a raw <c>sym_name</c> attribute are indexed
/// via attribute inspection as a fallback, ensuring that symbol tables work even when some
/// children have not been bound to a generated operation class.
/// </para>
/// </remarks>
public abstract class SymbolTableOperation : Operation
{
    private Dictionary<string, Operation>? symbolCache;

    /// <summary>
    /// Initializes a new instance of the <see cref="SymbolTableOperation"/> class.
    /// </summary>
    protected SymbolTableOperation(
        Syntax.OperationSyntax? syntax,
        IReadOnlyList<Region>? regions = null,
        NamedAttributeCollection? attributes = null,
        TypeReference? typeSignatureReference = null,
        IReadOnlyList<OperationResult>? resultValues = null,
        IReadOnlyList<Value?>? operandValues = null,
        IReadOnlyList<Block?>? successors = null)
        : base(syntax, regions, attributes, typeSignatureReference, resultValues, operandValues, successors)
    {
    }

    /// <summary>
    /// Gets a read-only dictionary of symbols immediately contained in this operation's first
    /// region, keyed by their symbol name.
    /// </summary>
    /// <remarks>
    /// The dictionary is built lazily on first access and cached; it is invalidated whenever the
    /// region contents change (see <see cref="InvalidateSyntax"/>).
    /// No deep traversal is performed: only direct children of the first region are indexed.
    /// </remarks>
    public IReadOnlyDictionary<string, Operation> Symbols => GetOrBuildSymbolCache();

    /// <inheritdoc/>
    /// <remarks>
    /// In addition to the standard syntax invalidation, this override clears the cached symbol
    /// dictionary so that the next access rebuilds it from the updated region contents.
    /// </remarks>
    public override void InvalidateSyntax()
    {
        symbolCache = null;
        base.InvalidateSyntax();
    }

    /// <inheritdoc/>
    [return: MaybeNull]
    public override TSymbol GetSymbol<TSymbol>(string name)
    {
        return GetOrBuildSymbolCache().TryGetValue(name, out var op) && op is TSymbol typedOp ? typedOp : null;
    }

    /// <summary>
    /// Returns the cached symbol dictionary, building it from the first region's direct children
    /// if the cache has been cleared or was never built.
    /// </summary>
    private Dictionary<string, Operation> GetOrBuildSymbolCache()
    {
        if (symbolCache != null)
        {
            return symbolCache;
        }

        var cache = new Dictionary<string, Operation>();
        if (Regions.Count > 0)
        {
            foreach (var block in Regions[0].Blocks)
            {
                foreach (var op in block.Operations)
                {
                    // Prefer the typed interface for generated ops; fall back to raw
                    // attribute inspection for untyped/generic ops.
                    string? symName;
                    if (op is ISymbolOp symbolOp)
                    {
                        symName = symbolOp.SymbolName;
                    }
                    else if (op.Attributes.TryGet("sym_name", out var attr) && attr.Value is StringAttributeValue sv)
                    {
                        symName = sv.Value;
                    }
                    else
                    {
                        symName = null;
                    }

                    if (symName != null)
                    {
                        cache[symName] = op;
                    }
                }
            }
        }

        symbolCache = cache;
        return cache;
    }
}
