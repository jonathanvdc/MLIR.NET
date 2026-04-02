namespace MLIR.Syntax;

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

    /// <summary>
    /// Attempts to get the trailing type signature as raw syntax text.
    /// </summary>
    public bool TryGetRawTypeSignature(out RawSyntaxText? rawTypeSignature)
    {
        if (TypeSignatureSyntax != null)
        {
            return TypeSignatureSyntax.TryGetRawText(out rawTypeSignature);
        }

        rawTypeSignature = null;
        return false;
    }

    /// <summary>
    /// Gets the trailing type signature as raw syntax text.
    /// </summary>
    public RawSyntaxText? RawTypeSignature => TypeSignatureSyntax?.GetRawText();

    /// <inheritdoc/>
    public override OperationBodySyntax RewriteChildren(SyntaxRewriter rewriter)
    {
        var newRegions = rewriter.VisitRegionList(Regions);
        var newAttributes = rewriter.VisitNamedAttributeList(Attributes);
        var newTypeSignature = TypeSignatureSyntax != null ? rewriter.VisitTypeSyntax(TypeSignatureSyntax) : null;
        if (ReferenceEquals(newRegions, Regions) && ReferenceEquals(newAttributes, Attributes) && ReferenceEquals(newTypeSignature, TypeSignatureSyntax))
            return this;
        return new GenericOperationBodySyntax(
            OperandList,
            SuccessorList,
            newRegions,
            newAttributes,
            TypeSignatureColonToken,
            newTypeSignature);
    }

    /// <inheritdoc/>
    public override void WriteTo(Text.SyntaxWriter writer, int indentLevel)
    {
        writer.WriteDelimitedList(OperandList, string.Empty);

        writer.WriteDelimitedList(SuccessorList, " ");

        foreach (var region in Regions)
        {
            writer.WriteRegion(region, indentLevel);
        }

        writer.WriteDelimitedList(Attributes, " ");

        if (TypeSignatureColonToken != null && TypeSignatureSyntax != null)
        {
            writer.WriteToken(TypeSignatureColonToken.Value, " ");
            TypeSignatureSyntax.WriteTo(writer, " ");
        }
    }
}
