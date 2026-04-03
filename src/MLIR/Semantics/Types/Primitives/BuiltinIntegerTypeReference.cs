using MLIR.Dialects;
using MLIR.Syntax;
using MLIR.Syntax.Types.Primitives;

namespace MLIR.Semantics.Types.Primitives;

/// <summary>
/// Represents a builtin integer type such as <c>i32</c> or <c>si64</c>.
/// </summary>
public sealed class BuiltinIntegerTypeReference : TypeReference
{
    /// <summary>
    /// Gets the shared builtin type definition.
    /// </summary>
    public static TypeDefinition TypeDefinition { get; } = new("integer");

    /// <summary>
    /// Initializes a new parsed builtin integer type reference.
    /// </summary>
    public BuiltinIntegerTypeReference(BuiltinIntegerTypeSyntax syntax)
        : this(syntax.Signedness, syntax.Width, syntax, syntax.Location)
    {
    }

    /// <summary>
    /// Initializes a new synthetic builtin integer type reference.
    /// </summary>
    public BuiltinIntegerTypeReference(IntegerTypeSignedness signedness, int width)
        : this(signedness, width, null, SourceLocation.Unknown)
    {
    }

    /// <summary>
    /// Gets the integer signedness.
    /// </summary>
    public IntegerTypeSignedness Signedness { get; }

    /// <summary>
    /// Gets the integer bit width.
    /// </summary>
    public int Width { get; }

    /// <inheritdoc/>
    public override string? Name => Signedness switch
    {
        IntegerTypeSignedness.Signed => "si" + Width,
        IntegerTypeSignedness.Unsigned => "ui" + Width,
        _ => "i" + Width,
    };

    /// <inheritdoc/>
    public override TypeDefinition? Definition => TypeDefinition;

    private BuiltinIntegerTypeReference(IntegerTypeSignedness signedness, int width, TypeSyntax? syntax, SourceLocation location)
        : base(syntax ?? BuildSyntax(signedness, width), location)
    {
        Signedness = signedness;
        Width = width;
    }

    private static BuiltinIntegerTypeSyntax BuildSyntax(IntegerTypeSignedness signedness, int width)
    {
        var text = signedness switch
        {
            IntegerTypeSignedness.Signed => "si" + width,
            IntegerTypeSignedness.Unsigned => "ui" + width,
            _ => "i" + width,
        };
        return new BuiltinIntegerTypeSyntax(new SyntaxToken(text), signedness, width);
    }
}
