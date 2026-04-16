using MLIR.Semantics;
using MLIR.Syntax;
using MLIR.Syntax.Types.Primitives;

namespace MLIR.Dialects.Builtin;

/// <summary>
/// Represents the builtin <c>index</c> type.
/// </summary>
/// <remarks>
/// The source generator contributes the public wrapper constructors and the shared
/// <see cref="TypeDefinition"/> metadata. This handwritten partial keeps the semantic
/// identity and syntax-preservation logic used by the runtime and generated constraint
/// wrappers.
/// </remarks>
public partial class IndexType : TypeReference
{
    /// <summary>
    /// Initializes a new synthetic builtin index type.
    /// </summary>
    public IndexType()
        : this(null, SourceLocation.Unknown)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="IndexType"/> class
    /// with an optional preserved syntax node.
    /// </summary>
    protected IndexType(TypeSyntax? syntax, SourceLocation location)
        : base(syntax ?? new BuiltinIndexTypeSyntax(TokenFactory.Identifier("index")), location)
    {
    }

    /// <inheritdoc/>
    public override string? Name => "index";

    /// <inheritdoc/>
    public override global::MLIR.Dialects.TypeDefinition? Definition => TypeDefinition;

    /// <inheritdoc/>
    protected override Type SemanticFamily => typeof(IndexType);
}
