using MLIR.Semantics;

namespace MLIR.Syntax.Types.Primitives;

/// <summary>
/// Represents the builtin <c>index</c> type.
/// </summary>
public sealed class BuiltinIndexTypeSyntax(SyntaxToken keywordToken) : TypeSyntax
{
    /// <summary>
    /// Gets the keyword token.
    /// </summary>
    public SyntaxToken KeywordToken { get; } = keywordToken;

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
        return new BuiltinIndexTypeSyntax(rewriter.VisitToken(KeywordToken));
    }
}
