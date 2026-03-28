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

        foreach (var dialect in group)
        {
            operations.AddRange(dialect.Operations);
            attributes.AddRange(dialect.Attributes);
            types.AddRange(dialect.Types);
        }

        return new OdsDialectModel(group.Key, operations, attributes, types);
    }
}
