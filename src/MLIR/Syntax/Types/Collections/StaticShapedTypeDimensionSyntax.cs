namespace MLIR.Syntax.Types.Collections;

/// <summary>
/// Represents a static integer dimension in a shaped type.
/// </summary>
public sealed class StaticShapedTypeDimensionSyntax(SyntaxToken sizeToken, long size) : ShapedTypeDimensionSyntax
{
    /// <summary>
    /// Gets the size token.
    /// </summary>
    public SyntaxToken SizeToken { get; } = sizeToken;

    /// <summary>
    /// Gets the parsed dimension size.
    /// </summary>
    public long Size { get; } = size;

    /// <inheritdoc/>
    public override bool TryGetRawText(out RawSyntaxText? rawText)
    {
        rawText = new RawSyntaxText([SizeToken]);
        return true;
    }

    /// <inheritdoc/>
    public override void WriteTo(Text.SyntaxWriter writer, string defaultLeadingTrivia)
    {
        writer.WriteToken(SizeToken, defaultLeadingTrivia);
    }
}
