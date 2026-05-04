namespace MLIR.Text;

using System.Globalization;
using MLIR.Dialects.Attributes.Primitives;
using MLIR.Numerics;
using MLIR.Syntax;
using MLIR.Syntax.Attributes.Primitives;

/// <summary>
/// Provides dialect-specific attribute parsers controlled access to the MLIR parser.
/// </summary>
public sealed class AttributeParsingContext : DialectParsingContext
{
    internal AttributeParsingContext(Parser parser)
        : base(parser)
    {
    }

    /// <summary>
    /// Tries to match a string literal token and returns it as a
    /// <see cref="StringAttributeValueSyntax"/> with the surrounding double-quotes stripped
    /// and escape sequences resolved. Returns <see cref="ParseResult{T}.NoMatch"/> when the
    /// current token is not a string literal.
    /// </summary>
    /// <remarks>
    /// This helper is the intended target for <c>csharpParser</c> expressions on
    /// <c>StringRefParameter</c>-derived ODS parameter classes:
    /// <code>let csharpParser = "$_parser.TryParseStringLiteralSyntax()";</code>
    /// </remarks>
    public ParseResult<AttributeValueSyntax> TryParseStringLiteralSyntax()
    {
        if (!TryMatch(TokenKind.StringLiteral, out var token))
        {
            return ParseResult<AttributeValueSyntax>.NoMatch();
        }

        return ParseResult<AttributeValueSyntax>.Success(
            new StringAttributeValueSyntax(token, StringLiteralAttributeAssemblyFormat.Unescape(token.Text)));
    }

    /// <summary>
    /// Tries to parse a signed integer literal (optionally preceded by <c>+</c> or <c>-</c>)
    /// and returns it as an <see cref="IntegerAttributeValueSyntax"/>.
    /// Returns <see cref="ParseResult{T}.NoMatch"/> when the current position does not start
    /// an integer literal.
    /// </summary>
    /// <remarks>
    /// This helper is the intended target for <c>csharpParser</c> expressions on
    /// <c>APIntParameter</c>-derived ODS parameter classes:
    /// <code>let csharpParser = "$_parser.TryParseIntegerLiteralSyntax()";</code>
    /// </remarks>
    public ParseResult<AttributeValueSyntax> TryParseIntegerLiteralSyntax()
    {
        if (!IntegerLiteralAttributeAssemblyFormat.TryParseSignedIntegerLiteral(this, out var signToken, out var integerToken, out var value))
        {
            return ParseResult<AttributeValueSyntax>.NoMatch();
        }

        return ParseResult<AttributeValueSyntax>.Success(
            new IntegerAttributeValueSyntax(
                signToken,
                integerToken,
                ApInt.Parse(64, value.ToString(CultureInfo.InvariantCulture), isSigned: true)));
    }

    /// <summary>
    /// Tries to parse a decimal floating-point literal and returns it as a
    /// <see cref="FloatingPointAttributeValueSyntax"/>.
    /// Returns <see cref="ParseResult{T}.NoMatch"/> when the current position does not start
    /// a floating-point literal.
    /// </summary>
    /// <remarks>
    /// This helper is the intended target for <c>csharpParser</c> expressions on
    /// <c>APFloatParameter</c>-derived ODS parameter classes:
    /// <code>let csharpParser = "$_parser.TryParseFloatingPointLiteralSyntax()";</code>
    /// </remarks>
    public ParseResult<AttributeValueSyntax> TryParseFloatingPointLiteralSyntax()
    {
        return FloatingPointAssemblyFormatHelper.TryParseDecimalLiteral(this, FloatSemantics.IEEEDouble);
    }

    /// <summary>
    /// Tries to parse a floating-point literal using explicit semantics and returns it as a
    /// <see cref="FloatingPointAttributeValueSyntax"/>.
    /// </summary>
    public ParseResult<AttributeValueSyntax> TryParseFloatingPointLiteralSyntax(FloatSemantics semantics)
    {
        return FloatingPointAssemblyFormatHelper.TryParseDecimalLiteral(this, semantics);
    }
}
