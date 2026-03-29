namespace MLIR.Generators;

using System.Collections.Generic;
using System.Linq;
using MLIR.ODS.Model;

internal static class DialectGeneratorModel
{
    public static DialectModel MergeDialectGroup(IGrouping<string, DialectModel> group)
    {
        var operations = new List<OperationModel>();
        var attributes = new List<AttributeModel>();
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
            types.AddRange(dialect.Types);
        }

        return new DialectModel(group.Key, cppNamespace, summary, description, hasConstantMaterializer, operations, attributes, types);
    }
}
