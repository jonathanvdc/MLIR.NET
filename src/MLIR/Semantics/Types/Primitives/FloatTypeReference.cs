using MLIR.Dialects;
using MLIR.Syntax;
using MLIR.Syntax.Types.Primitives;

namespace MLIR.Semantics.Types.Primitives;

/// <summary>
/// Represents a builtin floating-point type such as <c>f32</c>.
/// </summary>
public class FloatTypeReference : TypeReference
{
    /// <summary>
    /// Gets the shared builtin type definition.
    /// </summary>
    public static TypeDefinition TypeDefinition { get; } = new("float");

    /// <summary>
    /// Initializes a new parsed builtin floating-point type reference.
    /// </summary>
    public FloatTypeReference(BuiltinFloatTypeSyntax syntax)
        : this(syntax.Name, syntax, syntax.Location)
    {
    }

    /// <summary>
    /// Initializes a new synthetic builtin floating-point type reference.
    /// </summary>
    public FloatTypeReference(string name)
        : this(name, null, SourceLocation.Unknown)
    {
    }

    /// <inheritdoc/>
    public override string? Name { get; }

    /// <inheritdoc/>
    public override TypeDefinition? Definition => TypeDefinition;

    /// <summary>
    /// Initializes a new instance of the <see cref="FloatTypeReference"/> class
    /// with an optional preserved syntax node.
    /// </summary>
    protected FloatTypeReference(string name, TypeSyntax? syntax, SourceLocation location)
        : base(syntax ?? new BuiltinFloatTypeSyntax(TokenFactory.Identifier(name)), location)
    {
        Name = name;
    }

    /// <inheritdoc/>
    protected override Type SemanticFamily => typeof(FloatTypeReference);
}
