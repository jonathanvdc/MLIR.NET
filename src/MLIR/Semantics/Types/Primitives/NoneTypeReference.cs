using MLIR.Dialects;
using MLIR.Syntax;
using MLIR.Syntax.Types.Primitives;

namespace MLIR.Semantics.Types.Primitives;

/// <summary>
/// Represents the builtin <c>none</c> type.
/// </summary>
public class NoneTypeReference : TypeReference
{
    /// <summary>
    /// Gets the shared builtin type definition.
    /// </summary>
    public static TypeDefinition TypeDefinition { get; } = new("none");

    /// <summary>
    /// Initializes a new parsed builtin none type reference.
    /// </summary>
    public NoneTypeReference(BuiltinNoneTypeSyntax syntax)
        : this(syntax, syntax.Location)
    {
    }

    /// <summary>
    /// Initializes a new synthetic builtin none type reference.
    /// </summary>
    public NoneTypeReference()
        : this(null, SourceLocation.Unknown)
    {
    }

    /// <inheritdoc/>
    public override string? Name => "none";

    /// <inheritdoc/>
    public override TypeDefinition? Definition => TypeDefinition;

    /// <summary>
    /// Initializes a new instance of the <see cref="NoneTypeReference"/> class
    /// with an optional preserved syntax node.
    /// </summary>
    protected NoneTypeReference(TypeSyntax? syntax, SourceLocation location)
        : base(syntax ?? new BuiltinNoneTypeSyntax(TokenFactory.Identifier("none")), location)
    {
    }

    /// <inheritdoc/>
    protected override Type SemanticFamily => typeof(NoneTypeReference);
}
