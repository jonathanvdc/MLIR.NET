using MLIR.Dialects;
using MLIR.Syntax;
using MLIR.Syntax.Types.Primitives;

namespace MLIR.Semantics.Types.Primitives;

/// <summary>
/// Represents the builtin <c>index</c> type.
/// </summary>
public sealed class BuiltinIndexTypeReference : TypeReference
{
    /// <summary>
    /// Gets the shared builtin type definition.
    /// </summary>
    public static TypeDefinition TypeDefinition { get; } = new("index");

    /// <summary>
    /// Initializes a new parsed builtin index type reference.
    /// </summary>
    public BuiltinIndexTypeReference(BuiltinIndexTypeSyntax syntax)
        : this(syntax, syntax.Location)
    {
    }

    /// <summary>
    /// Initializes a new synthetic builtin index type reference.
    /// </summary>
    public BuiltinIndexTypeReference()
        : this(null, SourceLocation.Unknown)
    {
    }

    /// <inheritdoc/>
    public override string? Name => "index";

    /// <inheritdoc/>
    public override TypeDefinition? Definition => TypeDefinition;

    private BuiltinIndexTypeReference(TypeSyntax? syntax, SourceLocation location)
        : base(syntax ?? new BuiltinIndexTypeSyntax(new SyntaxToken("index")), location)
    {
    }
}
