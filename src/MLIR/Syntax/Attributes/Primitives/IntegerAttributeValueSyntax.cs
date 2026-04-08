namespace MLIR.Syntax.Attributes.Primitives;

using MLIR.Numerics;
using MLIR.Semantics;
using MLIR.Syntax;

/// <summary>
/// Represents a primitive integer attribute literal.
/// </summary>
public sealed class IntegerAttributeValueSyntax : AttributeValueSyntax
{
    /// <summary>
    /// Initializes a new instance of the <see cref="IntegerAttributeValueSyntax"/> class.
    /// </summary>
    /// <param name="signToken">The optional source token for the sign.</param>
    /// <param name="integerToken">The source token for the digits.</param>
    /// <param name="value">The parsed integer value.</param>
    public IntegerAttributeValueSyntax(SyntaxToken? signToken, SyntaxToken integerToken, ApInt value)
    {
        SignToken = signToken;
        IntegerToken = integerToken;
        Value = value;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="IntegerAttributeValueSyntax"/> class.
    /// </summary>
    /// <param name="integerToken">The source token for the integer literal.</param>
    /// <param name="value">The parsed integer value.</param>
    public IntegerAttributeValueSyntax(SyntaxToken integerToken, ApInt value)
        : this(null, integerToken, value)
    {
    }

    /// <summary>
    /// Gets the optional sign token.
    /// </summary>
    public SyntaxToken? SignToken { get; }

    /// <summary>
    /// Gets the digits token.
    /// </summary>
    public SyntaxToken IntegerToken { get; }

    /// <summary>
    /// Gets the parsed integer value.
    /// </summary>
    public ApInt Value { get; }

    /// <inheritdoc/>
    public override SourceLocation Location => SignToken?.Location ?? IntegerToken.Location;

    /// <inheritdoc/>
    public override void WriteTo(Text.SyntaxWriter writer)
    {
        if (SignToken.HasValue)
        {
            writer.WriteToken(SignToken.Value);
        }

        writer.WriteToken(IntegerToken);
    }
}
