namespace MLIR.Syntax;

/// <summary>
/// Rewrites concrete syntax trees while stripping trivia from reused source tokens.
/// </summary>
public sealed class TriviaStrippingSyntaxRewriter : SyntaxRewriter
{
    /// <inheritdoc/>
    public override SyntaxToken VisitToken(SyntaxToken token)
    {
        return token.HasSourceLocation
            ? new SyntaxToken(token.TokenKind, token.Text, null, token.Document!, token.TokenStart, token.TokenLength)
            : new SyntaxToken(token.TokenKind, token.Text);
    }
}
