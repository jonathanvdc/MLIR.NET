namespace MLIR.Syntax;

/// <summary>
/// Rewrites concrete syntax trees while stripping trivia from reused source tokens.
/// </summary>
public sealed class TriviaStrippingSyntaxRewriter : SyntaxRewriter
{
    /// <inheritdoc/>
    public override Token VisitToken(Token token)
    {
        return token.HasSourceLocation
            ? new Token(token.TokenKind, token.Text, null, token.Document!, token.TokenStart, token.TokenLength)
            : new Token(token.TokenKind, token.Text);
    }
}
