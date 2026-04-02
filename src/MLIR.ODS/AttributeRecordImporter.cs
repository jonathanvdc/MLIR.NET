namespace MLIR.ODS;

using MLIR.ODS.Model;

internal static class AttributeRecordImporter
{
    public static void Import(OdsRecordIndex index, DialectModelBuilder builder)
    {
        foreach (var record in index.GetRecordsWithBaseClass("AttrDef"))
        {
            if (!index.TryGetDialectName(record, out var attrDialectName)
                || !index.TryGetStringField(record, "attrName", out var attributeName))
            {
                continue;
            }

            var dialect = builder.GetOrCreateDialect(attrDialectName);
            dialect.Attributes.Add(new AttributeModel(attributeName, record.Name, index.GetOptionalStringField(record, "cppClassName") ?? record.Name));
        }
    }
}
