namespace MLIR.Generators;

using System.Collections.Generic;
using System.Linq;
using MLIR.ODS.Model;

internal static class OdsDialectGeneratorModel
{
    public static OdsDialectModel MergeDialectGroup(IGrouping<string, OdsDialectModel> group)
    {
        var operations = new List<OdsOperationModel>();
        var attributes = new List<OdsAttributeModel>();
        var types = new List<OdsTypeModel>();
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

        return new OdsDialectModel(group.Key, cppNamespace, summary, description, hasConstantMaterializer, operations, attributes, types);
    }
}
