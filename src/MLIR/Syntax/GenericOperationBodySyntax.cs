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
    public override bool TryGetGenericBody(out GenericOperationBodySyntax? genericBody)
    {
        genericBody = this;
        return true;
    }

    /// <inheritdoc/>
    public override void WriteTo(Text.SyntaxWriter writer, int indentLevel)
    {
        OperandList.WriteTo(writer, string.Empty, static (token, w, trivia) => w.WriteToken(token, trivia));

        SuccessorList.WriteTo(writer, " ", static (token, w, trivia) => w.WriteToken(token, trivia));

        foreach (var region in Regions)
        {
            writer.WriteRegion(region, indentLevel);
        }

        Attributes.WriteTo(writer, " ", static (attr, w, trivia) => attr.WriteTo(w, trivia));

        if (TypeSignatureColonToken != null && TypeSignatureSyntax != null)
        {
            writer.WriteToken(TypeSignatureColonToken.Value, " ");
            TypeSignatureSyntax.WriteTo(writer, " ");
        }
    }
}
