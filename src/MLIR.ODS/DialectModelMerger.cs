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
        var isPrelude = false;

        foreach (var dialect in group)
        {
            cppNamespace ??= dialect.CppNamespace;
            summary ??= dialect.Summary;
            description ??= dialect.Description;
            hasConstantMaterializer |= dialect.HasConstantMaterializer;
            isPrelude |= dialect.IsPrelude;
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
            operations
                .GroupBy(static operation => operation.Name, System.StringComparer.Ordinal)
                .Select(MergeOperationGroup)
                .ToArray(),
            attributes,
            attributeConstraints
                .GroupBy(static constraint => constraint.RecordName, System.StringComparer.Ordinal)
                .Select(static constraints => constraints.First())
                .ToArray(),
            typeConstraints
                .GroupBy(static constraint => constraint.RecordName, System.StringComparer.Ordinal)
                .Select(static constraints => constraints.First())
                .ToArray(),
            types,
            isPrelude);
    }

    private static OperationModel MergeOperationGroup(IGrouping<string, OperationModel> group)
    {
        OperationModel? primary = null;
        string? assemblyExtensionKind = null;

        foreach (var operation in group)
        {
            if (primary == null && (operation.ClassName != null
                || operation.Regions.Count > 0
                || operation.Operands.Count > 0
                || operation.Results.Count > 0
                || operation.Attributes.Count > 0
                || operation.Summary != null
                || operation.Description != null
                || operation.AssemblyFormat != null
                || operation.Traits.Count > 0))
            {
                primary = operation;
            }

            assemblyExtensionKind ??= operation.AssemblyFormatCode;
        }

        primary ??= group.First();
        return new OperationModel(
            primary.Name,
            primary.ClassName,
            primary.Regions,
            primary.Operands,
            primary.Results,
            primary.Attributes,
            primary.Summary,
            primary.Description,
            primary.AssemblyFormat,
            primary.Traits,
            assemblyExtensionKind);
    }
}
