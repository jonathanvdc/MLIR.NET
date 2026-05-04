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
/// <param name="assemblyFormatFactory">The optional custom assembly interpretation hook factory.</param>
/// <param name="parserAliases">Additional names that may be used to resolve this attribute during parsing.</param>
public sealed class AttributeDefinition(
    string name,
    IAttributeAssemblyFormat? assemblyFormat = null,
    System.Func<AttributeDefinition, IAttributeAssemblyFormat>? assemblyFormatFactory = null,
    IReadOnlyList<string>? parserAliases = null)
    : AttributeConstraintDefinition(
        name,
        assemblyFormat,
        assemblyFormatFactory is null ? null : definition => assemblyFormatFactory((AttributeDefinition)definition))
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
