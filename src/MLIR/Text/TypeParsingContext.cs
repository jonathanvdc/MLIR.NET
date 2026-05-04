namespace MLIR.Text;

using MLIR.Syntax;

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
    /// Parses a nested type syntax node using the ambient stop conditions from the caller that invoked
    /// the current type assembly format. This is useful for recursive builtin forms such as function
    /// results, where the outer parser has already determined which delimiters or keywords should end
    /// the current type.
    /// </summary>
    public ParseResult<TypeSyntax> TryParseCurrentTypeSyntax()
    {
        return Parser.TryParseCurrentTypeSyntaxInternal(defaultTypeStopBefore, defaultTypeStopBeforeKeywords, stopAtOperationBoundary);
    }
}
