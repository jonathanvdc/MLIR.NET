namespace MLIR.ODS;

using System.Collections.Generic;
using System.Linq;
using MLIR.ODS.Model;

/// <summary>
/// Merges grouped dialect models produced by the generator.
/// </summary>
public static class DialectModelMerger
{
    /// <summary>
    /// Merges a set of dialect fragments with the same logical dialect name.
    /// </summary>
    public static DialectModel MergeDialectGroup(IGrouping<string, DialectModel> group)
    {
        var operations = new List<OperationModel>();
        var attributes = new List<AttributeModel>();
        var attributeConstraints = new List<AttributeConstraintModel>();
        var typeConstraints = new List<TypeConstraintModel>();
        var types = new List<TypeModel>();
        string? cppNamespace = null;
        string? summary = null;
        string? description = null;
        var hasConstantMaterializer = false;

        foreach (var dialect in group)
        {
            cppNamespace ??= dialect.CppNamespace;
            summary ??= dialect.Summary;
            description ??= dialect.Description;
            hasConstantMaterializer |= dialect.HasConstantMaterializer;
            operations.AddRange(dialect.Operations);
            attributes.AddRange(dialect.Attributes);
            attributeConstraints.AddRange(dialect.AttributeConstraints);
            typeConstraints.AddRange(dialect.TypeConstraints);
            types.AddRange(dialect.Types);
        }

        return new DialectModel(
            group.Key,
            cppNamespace,
            summary,
            description,
            hasConstantMaterializer,
            operations,
            attributes,
            attributeConstraints
                .GroupBy(static constraint => constraint.RecordName, System.StringComparer.Ordinal)
                .Select(static constraints => constraints.First())
                .ToArray(),
            typeConstraints
                .GroupBy(static constraint => constraint.RecordName, System.StringComparer.Ordinal)
                .Select(static constraints => constraints.First())
                .ToArray(),
            types);
    }
}
