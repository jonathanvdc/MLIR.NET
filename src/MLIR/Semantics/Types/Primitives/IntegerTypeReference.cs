using MLIR.Dialects;
using MLIR.Syntax;
using MLIR.Syntax.Types.Primitives;

namespace MLIR.Semantics.Types.Primitives;

/// <summary>
/// Represents a builtin integer type such as <c>i32</c> or <c>si64</c>.
/// </summary>
public class IntegerTypeReference : TypeReference
{
    /// <summary>
    /// Gets the shared builtin type definition.
    /// </summary>
    public static TypeDefinition TypeDefinition { get; } = new("integer");

    /// <summary>
    /// Initializes a new parsed builtin integer type reference.
    /// </summary>
    public IntegerTypeReference(BuiltinIntegerTypeSyntax syntax)
        : this(syntax.Signedness, syntax.Width, syntax, syntax.Location)
    {
    }

    /// <summary>
    /// Initializes a new synthetic builtin integer type reference.
    /// </summary>
    public IntegerTypeReference(IntegerTypeSignedness signedness, int width)
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

    /// <summary>
    /// Initializes a new instance of the <see cref="IntegerTypeReference"/> class
    /// with an optional preserved syntax node.
    /// </summary>
    protected IntegerTypeReference(IntegerTypeSignedness signedness, int width, TypeSyntax? syntax, SourceLocation location)
        : base(syntax ?? BuildSyntax(signedness, width), location)
    {
        Signedness = signedness;
        Width = width;
    }

    /// <inheritdoc/>
    protected override Type SemanticFamily => typeof(IntegerTypeReference);

    /// <inheritdoc/>
    protected override bool SemanticEqualsValue(TypeReference other)
    {
        var otherInteger = (IntegerTypeReference)other;
        return Signedness == otherInteger.Signedness && Width == otherInteger.Width;
    }

    /// <inheritdoc/>
    protected override int GetSemanticHashCodeValue()
    {
        unchecked
        {
            return ((int)Signedness * 397) ^ Width;
        }
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
