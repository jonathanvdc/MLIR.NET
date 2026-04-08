using MLIR.Semantics;

namespace MLIR.Syntax.Types.Collections;

/// <summary>
/// Represents a dynamic dimension marker (<c>?</c>) in a shaped type.
/// </summary>
public sealed class DynamicShapedTypeDimensionSyntax(Token questionToken) : ShapedTypeDimensionSyntax
{
    /// <summary>
    /// Gets the question-mark token.
    /// </summary>
    public Token QuestionToken { get; } = questionToken;

    /// <inheritdoc/>
    public override SourceLocation Location => QuestionToken.Location;

    /// <inheritdoc/>
    public override void WriteTo(Text.SyntaxWriter writer)
    {
        writer.WriteToken(QuestionToken);
    }

    /// <inheritdoc/>
    public override SyntaxNode Rewrite(SyntaxRewriter rewriter)
    {
        return new DynamicShapedTypeDimensionSyntax(rewriter.VisitToken(QuestionToken));
    }
}
