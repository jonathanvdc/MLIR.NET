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
            var className = index.GetOptionalStringField(record, "cppClassName") ?? record.Name;

            // If this is an EnumAttr (has the 'enum' field pointing to an EnumInfo record),
            // extract the enum model so the generator can produce typed C# enums and value properties.
            EnumModel? enumModel = null;
            if (record.HasBaseClass("EnumAttr")
                && record.Fields.TryGetValue("enum", out var enumField)
                && enumField is TableGen.Evaluation.RecordReferenceValue enumRef
                && index.TryGetRecord(enumRef.RecordName, out var enumRecord))
            {
                index.TryGetEnumModel(enumRecord, out enumModel);
            }

            var parameters = index.GetAttrOrTypeParameters(record);

            var assemblyFormatString = index.GetOptionalStringField(record, "assemblyFormat");
            var assemblyFormat = !string.IsNullOrEmpty(assemblyFormatString)
                ? AssemblyFormatParser.Parse(assemblyFormatString!)
                : null;

            dialect.Attributes.Add(new AttributeModel(attributeName, record.Name, className, enumModel, parameters, assemblyFormat));
        }
    }
}
