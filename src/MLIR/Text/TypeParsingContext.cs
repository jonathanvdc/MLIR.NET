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
    private readonly TokenKind[] defaultTypeStopBefore;
    private readonly string[] defaultTypeStopBeforeKeywords;
    private readonly bool stopAtOperationBoundary;

    internal TypeParsingContext(Parser parser)
        : this(parser, [], [], stopAtOperationBoundary: false)
    {
    }

    internal TypeParsingContext(
        Parser parser,
        TokenKind[] defaultTypeStopBefore,
        string[] defaultTypeStopBeforeKeywords,
        bool stopAtOperationBoundary)
        : base(parser)
    {
        this.defaultTypeStopBefore = defaultTypeStopBefore;
        this.defaultTypeStopBeforeKeywords = defaultTypeStopBeforeKeywords;
        this.stopAtOperationBoundary = stopAtOperationBoundary;
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
    /// Parses a nested type syntax node using the ambient stop conditions from the caller that invoked
    /// the current type assembly format. This is useful for recursive builtin forms such as function
    /// results, where the outer parser has already determined which delimiters or keywords should end
    /// the current type.
    /// </summary>
    public ParseResult<TypeSyntax> TryParseCurrentTypeSyntax()
    {
        return Parser.TryParseCurrentTypeSyntaxInternal(defaultTypeStopBefore, defaultTypeStopBeforeKeywords, stopAtOperationBoundary);
    }

    /// <summary>
    /// Re-parses a standalone type text fragment using the current parser's dialect registry.
    /// This is used by shaped builtin type formats after splitting the outer raw body into
    /// dimensions and element-type text.
    /// </summary>
    public ParseResult<TypeSyntax> TryParseStandaloneTypeText(string text)
    {
        return Parser.TryParseStandaloneTypeTextInternal(text);
    }
}
