namespace MLIR.Text;

using System.Collections.Generic;
using MLIR.Syntax;

/// <summary>
/// Provides dialect-specific parsers controlled access to the MLIR parser.
/// </summary>
public sealed class OperationParsingContext : DialectParsingContext
{
    internal OperationParsingContext(Parser parser)
        : base(parser)
    {
    }

    /// <summary>
    /// Parses an SSA value token.
    /// </summary>
    public SyntaxToken ParseSsaToken()
    {
        return Parser.ParseSsaTokenInternal();
    }

    /// <summary>
    /// Parses a block label token.
    /// </summary>
    public SyntaxToken ParseBlockLabelToken()
    {
        return Parser.ParseBlockLabelTokenInternal();
    }

    /// <summary>
    /// Parses a nested region.
    /// </summary>
    public RegionSyntax ParseRegion()
    {
        return Parser.ParseRegionInternal();
    }

    /// <summary>
    /// Parses a named attribute.
    /// </summary>
    public NamedAttributeSyntax ParseAttribute()
    {
        return Parser.ParseAttributeInternal();
    }

    /// <summary>
    /// Parses raw syntax until the end of the current operation.
    /// </summary>
    public RawSyntaxText ParseRawUntilOperationBoundary()
    {
        return Parser.ParseRawUntilOperationBoundaryInternal();
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
    /// Returns <see langword="true"/> when the current token is an identifier whose text
    /// equals <paramref name="spelling"/> (case-sensitive, exact match).
    /// </summary>
    public bool IsKeyword(string spelling)
    {
        return Parser.IsKeywordInternal(spelling);
    }

    /// <summary>
    /// Parses a type, consuming tokens until an operation boundary is reached.
    /// </summary>
    public TypeSyntax ParseTypeSyntax()
    {
        return Parser.ParseTypeSyntaxUntilOperationBoundaryInternal();
    }

    /// <summary>
    /// Parses an attribute value, stopping before any of the supplied delimiter tokens or
    /// an operation boundary, whichever comes first.
    /// </summary>
    public new AttributeValueSyntax ParseAttributeValueSyntax(params TokenKind[] stopBefore)
    {
        return Parser.ParseAttributeValueSyntaxOrBoundaryInternal(stopBefore);
    }

    /// <summary>
    /// Parses an optional attribute dictionary of the form <c>{ name = value, ... }</c>.
    /// Returns an empty list when no opening brace is present.
    /// </summary>
    public DelimitedSyntaxList<NamedAttributeSyntax> ParseAttrDict()
    {
        return Parser.ParseAttrDictInternal();
    }

    /// <summary>
    /// Parses an optional keyword-prefixed attribute dictionary of the form
    /// <c>attributes { name = value, ... }</c>.
    /// Returns an empty list when the <c>attributes</c> keyword is absent.
    /// </summary>
    public DelimitedSyntaxList<NamedAttributeSyntax> ParseAttrDictWithKeyword()
    {
        return Parser.ParseAttrDictWithKeywordInternal();
    }

    /// <summary>
    /// Expects an identifier token whose text matches <paramref name="spelling"/> exactly.
    /// </summary>
    public SyntaxToken ExpectKeyword(string spelling, string message)
    {
        return Parser.ExpectKeywordInternal(spelling, message);
    }

    /// <summary>
    /// Parses zero or more consecutive regions, each delimited by <c>{ ... }</c>.
    /// </summary>
    public IReadOnlyList<RegionSyntax> ParseRegions()
    {
        return Parser.ParseRegionsInternal();
    }

    /// <summary>
    /// Parses an optional successor list of the form <c>[ ^bb1, ^bb2, ... ]</c>.
    /// Returns an empty list when no opening bracket is present.
    /// </summary>
    public DelimitedSyntaxList<SyntaxToken> ParseSuccessors()
    {
        return Parser.ParseSuccessorsInternal();
    }

    /// <summary>
    /// Parses an operand list of the form <c>( %a, %b, ... )</c>.
    /// </summary>
    public DelimitedSyntaxList<SyntaxToken> ParseOperands()
    {
        return Parser.ParseOperandsInternal();
    }

}
