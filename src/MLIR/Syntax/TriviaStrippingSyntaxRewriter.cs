namespace MLIR.Syntax;

using MLIR.Text;

/// <summary>
/// Rewrites concrete syntax trees while stripping trivia from reused source tokens.
/// </summary>
public sealed class TriviaStrippingSyntaxRewriter : SyntaxRewriter
{
    private TriviaStrippingSyntaxRewriter()
    {
    }

    /// <summary>
    /// Gets a singleton instance of the <see cref="TriviaStrippingSyntaxRewriter"/>.
    /// </summary>
    public static readonly TriviaStrippingSyntaxRewriter Instance = new();

    /// <inheritdoc/>
    public override Token VisitToken(Token token)
    {
        return token.HasSourceLocation
            ? new Token(token.TokenKind, token.Text, null, token.Document!, token.TokenStart, token.TokenLength)
            : new Token(token.TokenKind, token.Text);
    }
}
