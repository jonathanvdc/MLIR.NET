namespace MLIR.ODS;

using MLIR.ODS.Model;
using MLIR.ODS.Model.AssemblyFormat;

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
            var summary = index.GetOptionalStringField(record, "summary");
            var description = index.GetOptionalStringField(record, "description");
            var csharpName = index.GetOptionalStringField(record, "csharpName");
            var csharpAssemblyFormat = index.GetOptionalStringField(record, "csharpAssemblyFormat");
            var parameters = index.GetAttrOrTypeParameters(record);
            var assemblyFormatString = index.GetOptionalStringField(record, "assemblyFormat");
            var assemblyFormat = !string.IsNullOrEmpty(assemblyFormatString)
                ? AssemblyFormatParser.Parse(assemblyFormatString!)
                : null;

            dialect.Types.Add(new TypeModel(typeName, record.Name, className, summary, description, csharpName, csharpAssemblyFormat, parameters, assemblyFormat));
        }
    }
}
