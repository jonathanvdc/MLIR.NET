using MLIR.Dialects;
using MLIR.Syntax;
using MLIR.Syntax.Types.Primitives;

namespace MLIR.Semantics.Types.Primitives;

/// <summary>
/// Represents a builtin floating-point type such as <c>f32</c>.
/// </summary>
/// <remarks>
/// This base class is used as the fallback when no registered builtin dialect is present.
/// When the builtin dialect is registered, binding produces a concrete generated subclass
/// (e.g., <c>Float32Type</c>) that overrides <see cref="TypeReference.Definition"/> with
/// its own generated <c>TypeDefinition</c>.  The base class itself does not own a canonical
/// definition so that unregistered float values clearly have <c>Definition == null</c>.
/// </remarks>
public class FloatTypeReference : TypeReference
{
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
    /// <remarks>
    /// Returns <see langword="null"/> for unregistered float values.  Generated float
    /// <c>TypeDef</c> subclasses override this property with their own canonical definition.
    /// </remarks>
    public override TypeDefinition? Definition => null;

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
