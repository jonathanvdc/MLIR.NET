namespace MLIR.Syntax;

using System.Collections.Generic;

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
}
