namespace MLIR.Syntax.Attributes;

using MLIR.Syntax;

/// <summary>
/// Represents a type attribute whose value is a nested type syntax node.
/// </summary>
public sealed class TypeAttributeValueSyntax(TypeSyntax typeSyntax) : AttributeValueSyntax
{
    /// <summary>
    /// Gets the nested type syntax.
    /// </summary>
    public TypeSyntax TypeSyntax { get; } = typeSyntax;

    /// <inheritdoc/>
    public override bool TryGetRawText(out RawSyntaxText? rawText)
    {
        return TypeSyntax.TryGetRawText(out rawText);
    }

    /// <inheritdoc/>
    public override void WriteTo(Text.SyntaxWriter writer)
    {
        TypeSyntax.WriteTo(writer);
    }
}
