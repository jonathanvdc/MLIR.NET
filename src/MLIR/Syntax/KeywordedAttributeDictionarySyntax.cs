namespace MLIR.Syntax;

using MLIR.Text;

/// <summary>
/// Represents the optional <c>attributes { ... }</c> operation-format directive.
/// </summary>
/// <remarks>
/// Declarative assembly formats model <c>attr-dict-with-keyword</c> as one logical
/// directive, but the concrete syntax has two pieces: the leading <c>attributes</c>
/// keyword and the following dictionary delimiters. Keeping them together lets generated
/// syntax classes preserve the keyword token without teaching every generated body about
/// a two-property special case.
/// </remarks>
public sealed class KeywordedAttributeDictionarySyntax(
    Token? keywordToken,
    DelimitedSyntaxList<NamedAttributeSyntax> attributes) : SyntaxNode
{
    /// <summary>
    /// Gets the optional <c>attributes</c> keyword token.
    /// </summary>
    public Token? KeywordToken { get; } = keywordToken;

    /// <summary>
    /// Gets the attribute dictionary following the keyword.
    /// </summary>
    public DelimitedSyntaxList<NamedAttributeSyntax> Attributes { get; } = attributes;

    /// <summary>
    /// Gets a value indicating whether the keyworded dictionary is present in the source.
    /// </summary>
    public bool IsPresent => KeywordToken.HasValue;

    /// <inheritdoc/>
    public override SourceLocation Location
    {
        get
        {
            var result = SourceLocation.Unknown;
            if (KeywordToken.HasValue)
            {
                result = SourceLocation.Merge(result, KeywordToken.Value.Location);
            }

            result = SourceLocation.Merge(result, Attributes.Location);
            return result;
        }
    }

    /// <inheritdoc/>
    public override void WriteTo(SyntaxWriter writer)
    {
        if (!KeywordToken.HasValue)
        {
            return;
        }

        writer.WriteToken(KeywordToken.Value);
        writer.WriteDelimitedList(Attributes, " ");
    }

    /// <inheritdoc/>
    public override SyntaxNode Rewrite(SyntaxRewriter rewriter)
    {
        return new KeywordedAttributeDictionarySyntax(
            rewriter.VisitToken(KeywordToken),
            rewriter.VisitDelimitedList(Attributes));
    }
}
