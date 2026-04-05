namespace MLIR.Syntax.Types.Primitives;

/// <summary>
/// Represents a builtin floating-point type such as <c>f32</c> or <c>bf16</c>.
/// </summary>
public sealed class BuiltinFloatTypeSyntax(SyntaxToken nameToken) : TypeSyntax
{
    /// <summary>
    /// Gets the original identifier token.
    /// </summary>
    public SyntaxToken NameToken { get; } = nameToken;

    /// <summary>
    /// Gets the canonical builtin type name.
    /// </summary>
    public string Name => NameToken.Text;

    /// <inheritdoc/>
    public override bool TryGetRawText(out RawSyntaxText? rawText)
    {
        rawText = new RawSyntaxText([NameToken]);
        return true;
    }

    /// <inheritdoc/>
    public override void WriteTo(Text.SyntaxWriter writer)
    {
        writer.WriteToken(NameToken);
    }
}
