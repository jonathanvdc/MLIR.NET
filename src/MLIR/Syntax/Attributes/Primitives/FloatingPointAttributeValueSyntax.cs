namespace MLIR.Syntax.Attributes.Primitives;

using MLIR.Semantics;
using MLIR.Syntax;

/// <summary>
/// Represents a primitive floating-point attribute literal.
/// </summary>
public sealed class FloatingPointAttributeValueSyntax : AttributeValueSyntax
{
    private readonly RawSyntaxText rawText;

    /// <summary>
    /// Initializes a new instance of the <see cref="FloatingPointAttributeValueSyntax"/> class.
    /// </summary>
    public FloatingPointAttributeValueSyntax(RawSyntaxText rawText, string literalText)
    {
        this.rawText = rawText;
        LiteralText = literalText;
    }

    /// <summary>
    /// Gets the normalized literal text.
    /// </summary>
    public string LiteralText { get; }

    /// <inheritdoc/>
    public override SourceLocation Location => rawText.Location;

    /// <inheritdoc/>
    public override void WriteTo(Text.SyntaxWriter writer)
    {
        writer.WriteRaw(rawText);
    }
}
