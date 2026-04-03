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
    public override bool TryGetRawText(out RawSyntaxText? rawText)
    {
        rawText = new RawSyntaxText([NameToken]);
        return true;
    }

    /// <inheritdoc/>
    public override void WriteTo(Text.SyntaxWriter writer, string defaultLeadingTrivia)
    {
        writer.WriteToken(NameToken, defaultLeadingTrivia);
    }
}
