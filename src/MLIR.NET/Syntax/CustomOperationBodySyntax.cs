namespace MLIR.Syntax;

using System.Collections.Generic;

/// <summary>
/// Represents a token-preserving custom operation assembly body.
/// </summary>
public sealed class CustomOperationBodySyntax : OperationBodySyntax
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CustomOperationBodySyntax"/> class.
    /// </summary>
    /// <param name="items">The preserved custom assembly items in source order.</param>
    /// <param name="operandTokens">The operand tokens projected by the custom assembly.</param>
    /// <param name="successorTokens">The successor tokens projected by the custom assembly.</param>
    /// <param name="regions">The regions projected by the custom assembly.</param>
    /// <param name="attributes">The attribute dictionary projected by the custom assembly.</param>
    /// <param name="typeSignatureColonToken">The type-signature colon token, if present.</param>
    /// <param name="typeSignature">The raw trailing type signature, if present.</param>
    public CustomOperationBodySyntax(
        IReadOnlyList<CustomAssemblyItemSyntax> items,
        IReadOnlyList<SyntaxToken>? operandTokens = null,
        IReadOnlyList<SyntaxToken>? successorTokens = null,
        IReadOnlyList<RegionSyntax>? regions = null,
        DelimitedSyntaxList<NamedAttributeSyntax>? attributes = null,
        SyntaxToken? typeSignatureColonToken = null,
        RawSyntaxText? typeSignature = null)
    {
        Items = items;
        OperandTokens = operandTokens ?? EmptyTokens;
        SuccessorTokens = successorTokens ?? EmptyTokens;
        Regions = regions ?? EmptyRegions;
        Attributes = attributes ?? EmptyAttributes;
        TypeSignatureColonToken = typeSignatureColonToken;
        TypeSignature = typeSignature;
    }

    /// <summary>
    /// Gets the preserved custom assembly items in source order.
    /// </summary>
    public IReadOnlyList<CustomAssemblyItemSyntax> Items { get; }

    /// <inheritdoc/>
    public override IReadOnlyList<SyntaxToken> OperandTokens { get; }

    /// <inheritdoc/>
    public override IReadOnlyList<SyntaxToken> SuccessorTokens { get; }

    /// <inheritdoc/>
    public override IReadOnlyList<RegionSyntax> Regions { get; }

    /// <inheritdoc/>
    public override DelimitedSyntaxList<NamedAttributeSyntax> Attributes { get; }

    /// <inheritdoc/>
    public override SyntaxToken? TypeSignatureColonToken { get; }

    /// <inheritdoc/>
    public override RawSyntaxText? TypeSignature { get; }

    private static readonly IReadOnlyList<SyntaxToken> EmptyTokens = new SyntaxToken[0];
    private static readonly IReadOnlyList<RegionSyntax> EmptyRegions = new RegionSyntax[0];
    private static readonly DelimitedSyntaxList<NamedAttributeSyntax> EmptyAttributes =
        new DelimitedSyntaxList<NamedAttributeSyntax>(null, new NamedAttributeSyntax[0], new SyntaxToken[0], null);
}
