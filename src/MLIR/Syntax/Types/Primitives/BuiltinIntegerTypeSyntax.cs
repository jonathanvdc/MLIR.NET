using MLIR.Semantics;
using MLIR.Semantics.Types.Primitives;

namespace MLIR.Syntax.Types.Primitives;

/// <summary>
/// Represents a builtin integer type such as <c>i32</c>, <c>si64</c>, or <c>ui8</c>.
/// </summary>
public sealed class BuiltinIntegerTypeSyntax(SyntaxToken nameToken, IntegerTypeSignedness signedness, int width) : TypeSyntax
{
    /// <summary>
    /// Gets the original identifier token.
    /// </summary>
    public SyntaxToken NameToken { get; } = nameToken;

    /// <summary>
    /// Gets the signedness marker.
    /// </summary>
    public IntegerTypeSignedness Signedness { get; } = signedness;

    /// <summary>
    /// Gets the integer bit width.
    /// </summary>
    public int Width { get; } = width;

    /// <inheritdoc/>
    public override SourceLocation Location => NameToken.Location;

    /// <inheritdoc/>
    public override void WriteTo(Text.SyntaxWriter writer)
    {
        writer.WriteToken(NameToken);
    }

    /// <inheritdoc/>
    public override SyntaxNode Rewrite(SyntaxRewriter rewriter)
    {
        return new BuiltinIntegerTypeSyntax(rewriter.VisitToken(NameToken), Signedness, Width);
    }
}
