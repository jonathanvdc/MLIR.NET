namespace MLIR.Dialects;

using System.Collections.Generic;
using MLIR.Semantics;

/// <summary>
/// Describes a dialect-defined attribute value.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="AttributeDefinition"/> class.
/// </remarks>
/// <param name="name">The canonical attribute name.</param>
/// <param name="assemblyFormat">The optional custom assembly interpretation hook.</param>
/// <param name="parserAliases">Additional names that may be used to resolve this attribute during parsing.</param>
/// <param name="factory">The typed attribute-value factory.</param>
public sealed class AttributeDefinition(
    string name,
    IAttributeAssemblyFormat? assemblyFormat = null,
    IReadOnlyList<string>? parserAliases = null,
    System.Func<AttributeValueConstructionContext, AttributeValue>? factory = null)
    : AttributeConstraintDefinition(name, assemblyFormat, factory)
{
    /// <summary>
    /// Gets the canonical attribute name.
    /// </summary>
    public new string Name => base.Name!;

    /// <summary>
    /// Gets additional names that may resolve to this attribute during parsing.
    /// </summary>
    public IReadOnlyList<string> ParserAliases { get; } = parserAliases ?? EmptyAliases;

    private static readonly IReadOnlyList<string> EmptyAliases = new string[0];
}
