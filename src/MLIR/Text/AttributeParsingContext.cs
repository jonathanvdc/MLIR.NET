namespace MLIR.Text;

using System.Numerics;
using MLIR.Dialects;
using MLIR.Dialects.Attributes.Primitives;
using MLIR.Syntax;
using MLIR.Syntax.Attributes.Primitives;

/// <summary>
/// Provides dialect-specific attribute parsers controlled access to the MLIR parser.
/// </summary>
public sealed class AttributeParsingContext : DialectParsingContext
{
    internal AttributeParsingContext(Parser parser, DialectRegistry? dialectRegistry, AttributeConstraintDefinition? expectedDefinition)
        : base(parser)
    {
        DialectRegistry = dialectRegistry;
        ExpectedDefinition = expectedDefinition;
    }

    /// <summary>
    /// Gets the dialect registry used for parsing, if one is available.
    /// </summary>
    public DialectRegistry? DialectRegistry { get; }

    /// <summary>
    /// Gets the attribute definition expected by the caller, if one is known.
    /// </summary>
    public AttributeConstraintDefinition? ExpectedDefinition { get; }

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
        if (!IntegerLiteralAttributeAssemblyFormat.TryParseSignedIntegerLiteral(this, out var rawText, out var value))
        {
            return ParseResult<AttributeValueSyntax>.NoMatch();
        }

        return ParseResult<AttributeValueSyntax>.Success(
            new IntegerAttributeValueSyntax(IntegerLiteralAttributeAssemblyFormat.CreateSingleToken(rawText), value));
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
        return FloatingPointAssemblyFormatHelper.TryParseDecimalLiteral(this);
    }
}
