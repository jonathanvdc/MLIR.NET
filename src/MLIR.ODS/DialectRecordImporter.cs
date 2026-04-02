namespace MLIR.ODS;

internal static class DialectRecordImporter
{
    public static void Import(OdsRecordIndex index, DialectModelBuilder builder)
    {
        foreach (var record in index.GetRecordsWithBaseClass("Dialect"))
        {
            if (!index.TryGetStringField(record, "name", out var definedDialectName))
            {
                continue;
            }

            var dialect = builder.GetOrCreateDialect(definedDialectName);
            dialect.CppNamespace = index.GetOptionalStringField(record, "cppNamespace");
            dialect.Summary = index.GetOptionalStringField(record, "summary");
            dialect.Description = index.GetOptionalStringField(record, "description");
            dialect.HasConstantMaterializer = GetOptionalBitField(record);
        }
    }

    private static bool GetOptionalBitField(TableGen.Evaluation.Record record)
    {
        if (!record.Fields.TryGetValue("hasConstantMaterializer", out var field))
        {
            return false;
        }

        return field switch
        {
            TableGen.Evaluation.BitValue bit => bit.Value,
            TableGen.Evaluation.IntegerValue integer => integer.Value != 0,
            _ => false,
        };
    }
}
