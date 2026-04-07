namespace MLIR.ODS;

using MLIR.ODS.Model;

internal static class TypeRecordImporter
{
    public static void Import(OdsRecordIndex index, DialectModelBuilder builder)
    {
        foreach (var record in index.GetRecordsWithBaseClass("TypeDef"))
        {
            if (!index.TryGetDialectName(record, out var typeDialectName)
                || !index.TryGetStringField(record, "typeName", out var typeName))
            {
                continue;
            }

            var dialect = builder.GetOrCreateDialect(typeDialectName);
            var className = index.GetOptionalStringField(record, "cppClassName") ?? record.Name;
            var parameters = index.GetAttrOrTypeParameters(record);

            dialect.Types.Add(new TypeModel(typeName, record.Name, className, parameters));
        }
    }
}
