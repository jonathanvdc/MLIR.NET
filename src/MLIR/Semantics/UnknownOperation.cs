namespace MLIR.Semantics;

using System.Collections.Generic;
using MLIR.Syntax;

/// <summary>
/// Represents a bound operation whose dialect-specific semantic type is unknown.
/// </summary>
public sealed class UnknownOperation : Operation
{
    private readonly string name;
    private readonly Dialects.OperationDefinition? definition;
    private readonly IReadOnlyList<Region> regions;
    private readonly NamedAttributeCollection attributes;
    private readonly TypeReference? typeSignatureReference;
    private readonly IReadOnlyList<ValueReference> resultValues;
    private readonly IReadOnlyList<ValueReference> operandValues;
    private readonly IReadOnlyList<BlockReference> successorReferences;
    /// <summary>
    /// Initializes a new instance of the <see cref="UnknownOperation"/> class.
    /// </summary>
    public UnknownOperation(
        OperationSyntax syntax,
        string name,
        Dialects.OperationDefinition? definition,
        IReadOnlyList<Region> regions,
        NamedAttributeCollection attributes,
        TypeReference? typeSignatureReference,
        IReadOnlyList<ValueReference> resultValues,
        IReadOnlyList<ValueReference> operandValues,
        IReadOnlyList<BlockReference> successorReferences)
        : base(syntax)
    {
        this.name = name;
        this.definition = definition;
        this.regions = regions;
        this.attributes = attributes;
        this.typeSignatureReference = typeSignatureReference;
        this.resultValues = resultValues;
        this.operandValues = operandValues;
        this.successorReferences = successorReferences;
    }

    /// <inheritdoc/>
    public override string Name => name;

    /// <inheritdoc/>
    public override Dialects.OperationDefinition? Definition => definition;

    /// <inheritdoc/>
    public override IReadOnlyList<Region> Regions => regions;

    /// <inheritdoc/>
    public override NamedAttributeCollection Attributes => attributes;

    /// <inheritdoc/>
    public override TypeReference? TypeSignatureReference => typeSignatureReference;

    /// <inheritdoc/>
    public override IReadOnlyList<ValueReference> ResultValues => resultValues;

    /// <inheritdoc/>
    public override IReadOnlyList<ValueReference> OperandValues => operandValues;

    /// <inheritdoc/>
    public override IReadOnlyList<BlockReference> SuccessorReferences => successorReferences;

    /// <inheritdoc/>
    public override Operation RewriteChildren(SemanticRewriter rewriter)
    {
        List<Region>? newRegionList = null;
        for (int i = 0; i < regions.Count; i++)
        {
            var original = regions[i];
            var rewritten = rewriter.VisitRegion(original);
            if (newRegionList != null)
            {
                newRegionList.Add(rewritten);
            }
            else if (!ReferenceEquals(original, rewritten))
            {
                newRegionList = new List<Region>(regions.Count);
                for (int j = 0; j < i; j++)
                    newRegionList.Add(regions[j]);
                newRegionList.Add(rewritten);
            }
        }

        IReadOnlyList<Region> finalRegions = (IReadOnlyList<Region>?)newRegionList ?? regions;
        var finalAttributes = rewriter.VisitNamedAttributeCollection(attributes);
        var finalTypeRef = typeSignatureReference != null ? rewriter.VisitTypeReference(typeSignatureReference) : null;

        if (ReferenceEquals(finalRegions, regions) &&
            ReferenceEquals(finalAttributes, attributes) &&
            ReferenceEquals(finalTypeRef, typeSignatureReference))
        {
            return this;
        }

        return new UnknownOperation(Syntax!, name, definition, finalRegions, finalAttributes, finalTypeRef, resultValues, operandValues, successorReferences);
    }

}
