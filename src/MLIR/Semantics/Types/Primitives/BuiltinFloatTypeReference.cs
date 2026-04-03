using MLIR.Dialects;
using MLIR.Syntax;
using MLIR.Syntax.Types.Primitives;

namespace MLIR.Semantics.Types.Primitives;

/// <summary>
/// Represents a builtin floating-point type such as <c>f32</c>.
/// </summary>
public class BuiltinFloatTypeReference : TypeReference
{
    /// <summary>
    /// Gets the shared builtin type definition.
    /// </summary>
    public static TypeDefinition TypeDefinition { get; } = new("float");

    /// <summary>
    /// Initializes a new parsed builtin floating-point type reference.
    /// </summary>
    public BuiltinFloatTypeReference(BuiltinFloatTypeSyntax syntax)
        : this(syntax.Name, syntax, syntax.Location)
    {
    }

    /// <summary>
    /// Initializes a new synthetic builtin floating-point type reference.
    /// </summary>
    public BuiltinFloatTypeReference(string name)
        : this(name, null, SourceLocation.Unknown)
    {
    }

    /// <inheritdoc/>
    public override string? Name { get; }

    /// <inheritdoc/>
    public override TypeDefinition? Definition => TypeDefinition;

    /// <summary>
    /// Initializes a new instance of the <see cref="BuiltinFloatTypeReference"/> class
    /// with an optional preserved syntax node.
    /// </summary>
    protected BuiltinFloatTypeReference(string name, TypeSyntax? syntax, SourceLocation location)
        : base(syntax ?? new BuiltinFloatTypeSyntax(new SyntaxToken(name)), location)
    {
        Name = name;
    }
}
