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
    public IntegerAttributeValueSyntax(Token? signToken, Token integerToken, ApInt value)
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
    public IntegerAttributeValueSyntax(Token integerToken, ApInt value)
        : this(null, integerToken, value)
    {
    }

    /// <summary>
    /// Gets the optional sign token.
    /// </summary>
    public Token? SignToken { get; }

    /// <summary>
    /// Gets the digits token.
    /// </summary>
    public Token IntegerToken { get; }

    /// <summary>
    /// Gets the parsed integer value.
    /// </summary>
    public ApInt Value { get; }

    /// <inheritdoc/>
    public override SourceLocation Location =>
        SourceLocation.Merge(SignToken?.Location ?? SourceLocation.Unknown, IntegerToken.Location);

    /// <inheritdoc/>
    public override void WriteTo(Text.SyntaxWriter writer)
    {
        if (SignToken.HasValue)
        {
            writer.WriteToken(SignToken.Value);
        }

        writer.WriteToken(IntegerToken);
    }

    /// <inheritdoc/>
    public override SyntaxNode Rewrite(SyntaxRewriter rewriter)
    {
        return new IntegerAttributeValueSyntax(
            rewriter.VisitToken(SignToken),
            rewriter.VisitToken(IntegerToken),
            Value);
    }
}
