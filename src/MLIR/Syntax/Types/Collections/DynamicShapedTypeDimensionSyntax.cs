namespace MLIR.Syntax.Types.Collections;

/// <summary>
/// Represents a dynamic dimension marker (<c>?</c>) in a shaped type.
/// </summary>
public sealed class DynamicShapedTypeDimensionSyntax(SyntaxToken questionToken) : ShapedTypeDimensionSyntax
{
    /// <summary>
    /// Gets the question-mark token.
    /// </summary>
    public SyntaxToken QuestionToken { get; } = questionToken;

    /// <inheritdoc/>
    public override bool TryGetRawText(out RawSyntaxText? rawText)
    {
        rawText = new RawSyntaxText([QuestionToken]);
        return true;
    }

    /// <inheritdoc/>
    public override void WriteTo(Text.SyntaxWriter writer, string defaultLeadingTrivia)
    {
        writer.WriteToken(QuestionToken, defaultLeadingTrivia);
    }
}
