namespace MLIR.Text;

using System.Globalization;
using MLIR.Dialects.Attributes.Primitives;
using MLIR.Numerics;
using MLIR.Syntax;
using MLIR.Syntax.Attributes.Primitives;

/// <summary>
/// Provides dialect-specific type parsers controlled access to the MLIR parser.
/// </summary>
public sealed class TypeParsingContext : DialectParsingContext
{
    internal TypeParsingContext(Parser parser)
        : base(parser)
    {
    }

    /// <summary>
    /// Tries to match a string literal token and returns it as a
    /// <see cref="StringAttributeValueSyntax"/> with the surrounding double-quotes stripped
    /// and escape sequences resolved.
    /// </summary>
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
    /// Tries to parse a signed integer literal and returns it as an
    /// <see cref="IntegerAttributeValueSyntax"/>.
    /// </summary>
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
    /// </summary>
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

    /// <summary>
    /// Parses a nested attribute-style value syntax node, stopping before any of the supplied delimiters.
    /// This mirrors the attribute parsing helpers so type parameter assembly formats can reuse the
    /// same literal-oriented parameter contracts.
    /// </summary>
    public new ParseResult<AttributeValueSyntax> TryParseAttributeValueSyntax(params TokenKind[] stopBefore)
    {
        return Parser.TryParseAttributeValueInternal(stopBefore);
    }
}
