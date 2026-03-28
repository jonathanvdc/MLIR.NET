namespace MLIR.Text;

using System.Collections.Generic;
using MLIR.Syntax;

/// <summary>
/// Provides dialect-specific parsers controlled access to the MLIR parser.
/// </summary>
public sealed class OperationParsingContext
{
    private readonly Parser parser;

    internal OperationParsingContext(Parser parser)
    {
        this.parser = parser;
    }

    /// <summary>
    /// Determines whether the current token has the supplied kind.
    /// </summary>
    public bool Is(TokenKind kind)
    {
        return parser.IsToken(kind);
    }

    /// <summary>
    /// Attempts to match the current token.
    /// </summary>
    public bool TryMatch(TokenKind kind, out SyntaxToken token)
    {
        if (parser.TryMatchToken(kind, out var rawToken))
        {
            token = Parser.ToSyntaxToken(rawToken);
            return true;
        }

        token = default;
        return false;
    }

    /// <summary>
    /// Expects a token of the supplied kind.
    /// </summary>
    public SyntaxToken Expect(TokenKind kind, string message)
    {
        return parser.ExpectTokenInternal(kind, message);
    }

    /// <summary>
    /// Parses an SSA value token.
    /// </summary>
    public SyntaxToken ParseSsaToken()
    {
        return parser.ParseSsaTokenInternal();
    }

    /// <summary>
    /// Parses a block label token.
    /// </summary>
    public SyntaxToken ParseBlockLabelToken()
    {
        return parser.ParseBlockLabelTokenInternal();
    }

    /// <summary>
    /// Parses a nested region.
    /// </summary>
    public RegionSyntax ParseRegion()
    {
        return parser.ParseRegionInternal();
    }

    /// <summary>
    /// Parses a named attribute.
    /// </summary>
    public NamedAttributeSyntax ParseAttribute()
    {
        return parser.ParseAttributeInternal();
    }

    /// <summary>
    /// Parses raw syntax until one of the supplied delimiters is reached at the outermost nesting level.
    /// </summary>
    public RawSyntaxText ParseRawUntilDelimiter(params TokenKind[] delimiters)
    {
        return parser.ParseRawUntilDelimiterInternal(delimiters);
    }

    /// <summary>
    /// Parses raw syntax until the end of the current operation.
    /// </summary>
    public RawSyntaxText ParseRawUntilOperationBoundary()
    {
        return parser.ParseRawUntilOperationBoundaryInternal();
    }

    /// <summary>
    /// Creates an empty operand list for a generic operation projection.
    /// </summary>
    public DelimitedSyntaxList<SyntaxToken> CreateEmptyOperandList()
    {
        return new DelimitedSyntaxList<SyntaxToken>(new SyntaxToken("("), new List<SyntaxToken>(), new List<SyntaxToken>(), new SyntaxToken(")"));
    }

    /// <summary>
    /// Creates an empty successor list for a generic operation projection.
    /// </summary>
    public DelimitedSyntaxList<SyntaxToken> CreateEmptySuccessorList()
    {
        return new DelimitedSyntaxList<SyntaxToken>(null, new List<SyntaxToken>(), new List<SyntaxToken>(), null);
    }

    /// <summary>
    /// Creates an attribute dictionary for a projected operation body.
    /// </summary>
    public DelimitedSyntaxList<NamedAttributeSyntax> CreateAttributeDictionary(IReadOnlyList<NamedAttributeSyntax> attributes)
    {
        return new DelimitedSyntaxList<NamedAttributeSyntax>(
            attributes.Count > 0 ? new SyntaxToken("{") : null,
            attributes,
            CreateCommaTokens(attributes.Count),
            attributes.Count > 0 ? new SyntaxToken("}") : null);
    }

    /// <summary>
    /// Creates comma tokens for a projected delimited list.
    /// </summary>
    public IReadOnlyList<SyntaxToken> CreateCommaTokens(int itemCount)
    {
        var commas = new List<SyntaxToken>();
        for (var i = 1; i < itemCount; i++)
        {
            commas.Add(new SyntaxToken(","));
        }

        return commas;
    }

    /// <summary>
    /// Wraps a token as a preserved custom assembly item.
    /// </summary>
    public CustomTokenSyntax Token(SyntaxToken token)
    {
        return new CustomTokenSyntax(token);
    }

    /// <summary>
    /// Wraps raw text as a preserved custom assembly item.
    /// </summary>
    public CustomRawSyntax Raw(RawSyntaxText text)
    {
        return new CustomRawSyntax(text);
    }

    /// <summary>
    /// Wraps a nested region as a preserved custom assembly item.
    /// </summary>
    public CustomRegionSyntax Region(RegionSyntax region)
    {
        return new CustomRegionSyntax(region);
    }

    /// <summary>
    /// Wraps an attribute dictionary as a preserved custom assembly item.
    /// </summary>
    public CustomAttributeDictionarySyntax AttributeDictionary(DelimitedSyntaxList<NamedAttributeSyntax> attributes)
    {
        return new CustomAttributeDictionarySyntax(attributes);
    }

    /// <summary>
    /// Creates a custom operation body while also exposing its projected semantic shape.
    /// </summary>
    public CustomOperationBodySyntax CreateCustomBody(
        IReadOnlyList<CustomAssemblyItemSyntax> items,
        IReadOnlyList<SyntaxToken>? operandTokens = null,
        IReadOnlyList<SyntaxToken>? successorTokens = null,
        IReadOnlyList<RegionSyntax>? regions = null,
        DelimitedSyntaxList<NamedAttributeSyntax>? attributes = null,
        SyntaxToken? typeSignatureColonToken = null,
        RawSyntaxText? typeSignature = null)
    {
        return new CustomOperationBodySyntax(
            items,
            operandTokens,
            successorTokens,
            regions,
            attributes,
            typeSignatureColonToken,
            typeSignature);
    }
}
