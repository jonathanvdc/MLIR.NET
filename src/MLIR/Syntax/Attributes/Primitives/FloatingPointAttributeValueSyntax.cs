namespace MLIR.Syntax.Attributes.Primitives;

using MLIR.Numerics;
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
    public FloatingPointAttributeValueSyntax(RawSyntaxText rawText, ApFloat value)
    {
        this.rawText = rawText;
        Value = value;
    }

    /// <summary>
    /// Gets the parsed floating-point value, including its semantics.
    /// </summary>
    public ApFloat Value { get; }

    /// <summary>
    /// Gets the literal text as written in source.
    /// </summary>
    public string LiteralText => rawText.Text;

    /// <inheritdoc/>
    public override SourceLocation Location => rawText.Location;

    /// <inheritdoc/>
    public override void WriteTo(Text.SyntaxWriter writer)
    {
        writer.WriteRaw(rawText);
    }
}
