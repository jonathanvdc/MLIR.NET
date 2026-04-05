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
    public override bool TryGetRawText(out RawSyntaxText? rawText)
    {
        rawText = new RawSyntaxText([KeywordToken]);
        return true;
    }

    /// <inheritdoc/>
    public override void WriteTo(Text.SyntaxWriter writer)
    {
        writer.WriteToken(KeywordToken);
    }
}
