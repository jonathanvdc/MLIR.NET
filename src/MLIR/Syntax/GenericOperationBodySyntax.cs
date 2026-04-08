namespace MLIR.Syntax;

using MLIR.Semantics;

/// <summary>
/// Represents the generic MLIR operation body syntax.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="GenericOperationBodySyntax"/> class.
/// </remarks>
/// <param name="operandList">The delimited operand list.</param>
/// <param name="successorList">The delimited successor list.</param>
/// <param name="regions">The regions nested under the operation.</param>
/// <param name="attributes">The delimited attribute dictionary.</param>
/// <param name="typeSignatureColonToken">The colon token that introduces the type signature, if present.</param>
/// <param name="typeSignatureSyntax">The trailing type signature syntax, if present.</param>
public sealed class GenericOperationBodySyntax(
    DelimitedSyntaxList<SyntaxToken> operandList,
    DelimitedSyntaxList<SyntaxToken> successorList,
    IReadOnlyList<RegionSyntax> regions,
    DelimitedSyntaxList<NamedAttributeSyntax> attributes,
    SyntaxToken? typeSignatureColonToken,
    TypeSyntax? typeSignatureSyntax) : OperationBodySyntax
{
    /// <summary>
    /// Gets the delimited operand list.
    /// </summary>
    public DelimitedSyntaxList<SyntaxToken> OperandList { get; } = operandList;

    /// <summary>
    /// Gets the delimited successor list.
    /// </summary>
    public DelimitedSyntaxList<SyntaxToken> SuccessorList { get; } = successorList;

    /// <summary>
    /// Gets the regions nested under the operation.
    /// </summary>
    public IReadOnlyList<RegionSyntax> Regions { get; } = regions;

    /// <summary>
    /// Gets the delimited attribute dictionary.
    /// </summary>
    public DelimitedSyntaxList<NamedAttributeSyntax> Attributes { get; } = attributes;

    /// <summary>
    /// Gets the colon token that introduces the type signature, if present.
    /// </summary>
    public SyntaxToken? TypeSignatureColonToken { get; } = typeSignatureColonToken;

    /// <summary>
    /// Gets the trailing type signature syntax, if present.
    /// </summary>
    public TypeSyntax? TypeSignatureSyntax { get; } = typeSignatureSyntax;

    /// <inheritdoc/>
    public override SourceLocation Location
    {
        get
        {
            // Merge all delimiters and subtrees in parse order so the resulting span
            // covers the entire body from the opening operand parenthesis to the end
            // of the type signature (or whichever construct appears last).
            var result = SourceLocation.Unknown;
            if (OperandList.OpenToken.HasValue)
                result = SourceLocation.Merge(result, OperandList.OpenToken.Value.Location);
            if (OperandList.CloseToken.HasValue)
                result = SourceLocation.Merge(result, OperandList.CloseToken.Value.Location);
            if (SuccessorList.OpenToken.HasValue)
                result = SourceLocation.Merge(result, SuccessorList.OpenToken.Value.Location);
            if (SuccessorList.CloseToken.HasValue)
                result = SourceLocation.Merge(result, SuccessorList.CloseToken.Value.Location);
            foreach (var region in Regions)
                result = SourceLocation.Merge(result, region.Location);
            if (Attributes.OpenToken.HasValue)
                result = SourceLocation.Merge(result, Attributes.OpenToken.Value.Location);
            if (Attributes.CloseToken.HasValue)
                result = SourceLocation.Merge(result, Attributes.CloseToken.Value.Location);
            if (TypeSignatureColonToken.HasValue)
                result = SourceLocation.Merge(result, TypeSignatureColonToken.Value.Location);
            if (TypeSignatureSyntax != null)
                result = SourceLocation.Merge(result, TypeSignatureSyntax.Location);
            return result;
        }
    }

    /// <inheritdoc/>
    public override void WriteTo(Text.SyntaxWriter writer)
    {
        writer.WriteDelimitedList(OperandList);

        writer.WriteDelimitedList(SuccessorList, " ");

        foreach (var region in Regions)
        {
            writer.WriteRegion(region);
        }

        writer.WriteDelimitedList(Attributes, " ");

        if (TypeSignatureColonToken != null && TypeSignatureSyntax != null)
        {
            writer.WriteToken(TypeSignatureColonToken.Value, " ");
            writer.SuggestTrivia(" ");
            TypeSignatureSyntax.WriteTo(writer);
        }
    }

    /// <inheritdoc/>
    public override SyntaxNode Rewrite(SyntaxRewriter rewriter)
    {
        return new GenericOperationBodySyntax(
            rewriter.VisitDelimitedTokenList(OperandList),
            rewriter.VisitDelimitedTokenList(SuccessorList),
            rewriter.VisitList(Regions),
            rewriter.VisitDelimitedList(Attributes),
            rewriter.VisitToken(TypeSignatureColonToken),
            TypeSignatureSyntax != null ? (TypeSyntax)rewriter.Visit(TypeSignatureSyntax) : null);
    }
}
