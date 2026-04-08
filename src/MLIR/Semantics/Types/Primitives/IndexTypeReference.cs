using MLIR.Dialects;
using MLIR.Syntax;
using MLIR.Syntax.Types.Primitives;

namespace MLIR.Semantics.Types.Primitives;

/// <summary>
/// Represents the builtin <c>index</c> type.
/// </summary>
public class IndexTypeReference : TypeReference
{
    /// <summary>
    /// Gets the shared builtin type definition.
    /// </summary>
    public static TypeDefinition TypeDefinition { get; } = new("index");

    /// <summary>
    /// Initializes a new parsed builtin index type reference.
    /// </summary>
    public IndexTypeReference(BuiltinIndexTypeSyntax syntax)
        : this(syntax, syntax.Location)
    {
    }

    /// <summary>
    /// Initializes a new synthetic builtin index type reference.
    /// </summary>
    public IndexTypeReference()
        : this(null, SourceLocation.Unknown)
    {
    }

    /// <inheritdoc/>
    public override string? Name => "index";

    /// <inheritdoc/>
    public override TypeDefinition? Definition => TypeDefinition;

    /// <summary>
    /// Initializes a new instance of the <see cref="IndexTypeReference"/> class
    /// with an optional preserved syntax node.
    /// </summary>
    protected IndexTypeReference(TypeSyntax? syntax, SourceLocation location)
        : base(syntax ?? new BuiltinIndexTypeSyntax(TokenFactory.Identifier("index")), location)
    {
    }

    /// <inheritdoc/>
    protected override Type SemanticFamily => typeof(IndexTypeReference);
}
