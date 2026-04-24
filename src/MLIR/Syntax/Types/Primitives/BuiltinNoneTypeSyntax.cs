using MLIR.Semantics;

namespace MLIR.Syntax.Types.Primitives;

using MLIR.Text;

/// <summary>
/// Represents the builtin <c>none</c> type.
/// </summary>
public sealed class BuiltinNoneTypeSyntax(Token keywordToken) : TypeSyntax
{
    /// <summary>
    /// Gets the keyword token.
    /// </summary>
    public Token KeywordToken { get; } = keywordToken;

    /// <inheritdoc/>
    public override SourceLocation Location => KeywordToken.Location;

    /// <inheritdoc/>
    public override void WriteTo(Text.SyntaxWriter writer)
    {
        writer.WriteToken(KeywordToken);
    }

    /// <inheritdoc/>
    public override SyntaxNode Rewrite(SyntaxRewriter rewriter)
    {
        return new BuiltinNoneTypeSyntax(rewriter.VisitToken(KeywordToken));
    }
}
