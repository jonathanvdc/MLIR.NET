namespace MLIR.Text;

using System.Globalization;
using MLIR.Dialects;
using MLIR.Dialects.Attributes.Primitives;
using MLIR.Numerics;
using MLIR.Syntax;
using MLIR.Syntax.Attributes.Primitives;

/// <summary>
/// Provides dialect-specific attribute parsers controlled access to the MLIR parser.
/// </summary>
public sealed class AttributeParsingContext : DialectParsingContext
{
    internal AttributeParsingContext(
        Parser parser,
        DialectRegistry? dialectRegistry,
        AttributeConstraintDefinition? expectedDefinition,
        DialectAttributePrefix? prefix = null)
        : base(parser)
    {
        DialectRegistry = dialectRegistry;
        ExpectedDefinition = expectedDefinition;
        Prefix = prefix;
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
    /// Gets the <c>#dialect.mnemonic</c> prefix tokens that were consumed by the self-identifying
    /// attribute parser before delegating to this format's <c>TryParse</c> method.
    /// </summary>
    /// <remarks>
    /// This is set only for formats that implement
    /// <see cref="Dialects.IBodyOnlyAttributeAssemblyFormat"/>.  The generated assembly format
    /// class passes <c>context.Prefix</c> directly to the generated syntax constructor, so that
    /// the actual parsed source tokens are stored on the syntax node and reproduced faithfully
    /// by <c>WriteTo</c>.  For programmatic construction, use
    /// <see cref="Syntax.DialectAttributePrefix.Synthetic"/> to create placeholder tokens.
    /// When <see langword="null"/>, the format is not body-only or the prefix was not consumed.
    /// </remarks>
    public DialectAttributePrefix? Prefix { get; }

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
            new IntegerAttributeValueSyntax(
                IntegerLiteralAttributeAssemblyFormat.CreateSingleToken(rawText),
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
        return FloatingPointAssemblyFormatHelper.TryParseDecimalLiteral(this);
    }
}
