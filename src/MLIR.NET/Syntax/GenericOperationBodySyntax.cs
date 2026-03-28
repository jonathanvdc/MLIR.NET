namespace MLIR.Syntax;

using System.Collections.Generic;
using MLIR.Text;

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
/// <param name="typeSignature">The raw trailing type signature, if present.</param>
public sealed class GenericOperationBodySyntax(
    DelimitedSyntaxList<SyntaxToken> operandList,
    DelimitedSyntaxList<SyntaxToken> successorList,
    IReadOnlyList<RegionSyntax> regions,
    DelimitedSyntaxList<NamedAttributeSyntax> attributes,
    SyntaxToken? typeSignatureColonToken,
    RawSyntaxText? typeSignature) : OperationBodySyntax
{
    /// <summary>
    /// Gets the delimited operand list.
    /// </summary>
    public DelimitedSyntaxList<SyntaxToken> OperandList { get; } = operandList;

    /// <summary>
    /// Gets the delimited successor list.
    /// </summary>
    public DelimitedSyntaxList<SyntaxToken> SuccessorList { get; } = successorList;

    /// <inheritdoc/>
    public override IReadOnlyList<SyntaxToken> OperandTokens => OperandList.Items;

    /// <inheritdoc/>
    public override IReadOnlyList<SyntaxToken> SuccessorTokens => SuccessorList.Items;

    /// <inheritdoc/>
    public override IReadOnlyList<RegionSyntax> Regions { get; } = regions;

    /// <inheritdoc/>
    public override DelimitedSyntaxList<NamedAttributeSyntax> Attributes { get; } = attributes;

    /// <inheritdoc/>
    public override SyntaxToken? TypeSignatureColonToken { get; } = typeSignatureColonToken;

    /// <inheritdoc/>
    public override RawSyntaxText? TypeSignature { get; } = typeSignature;

    /// <inheritdoc/>
    public override void Print(OperationBodyPrintingContext context)
    {
        context.WriteToken(OperandList.OpenToken!.Value, string.Empty);
        for (var i = 0; i < OperandList.Count; i++)
        {
            if (i > 0)
            {
                context.WriteToken(OperandList.SeparatorTokens[i - 1], string.Empty);
            }

            context.WriteToken(OperandList[i], i > 0 ? " " : string.Empty);
        }

        context.WriteToken(OperandList.CloseToken!.Value, string.Empty);

        if (SuccessorList.OpenToken != null)
        {
            context.WriteToken(SuccessorList.OpenToken.Value, " ");
            for (var i = 0; i < SuccessorList.Count; i++)
            {
                if (i > 0)
                {
                    context.WriteToken(SuccessorList.SeparatorTokens[i - 1], string.Empty);
                }

                context.WriteToken(SuccessorList[i], i > 0 ? " " : string.Empty);
            }

            context.WriteToken(SuccessorList.CloseToken!.Value, string.Empty);
        }

        foreach (var region in Regions)
        {
            context.WriteRegion(region);
        }

        if (Attributes.OpenToken != null)
        {
            context.WriteToken(Attributes.OpenToken.Value, " ");
            for (var i = 0; i < Attributes.Count; i++)
            {
                if (i > 0)
                {
                    context.WriteToken(Attributes.SeparatorTokens[i - 1], string.Empty);
                }

                context.WriteAttribute(Attributes[i], i > 0 ? " " : string.Empty);
            }

            context.WriteToken(Attributes.CloseToken!.Value, string.Empty);
        }

        if (TypeSignatureColonToken != null && TypeSignature != null)
        {
            context.WriteToken(TypeSignatureColonToken.Value, " ");
            context.WriteRaw(TypeSignature, " ");
        }
    }
}
