namespace MLIR.ODS;

using MLIR.ODS.Model;

internal static class TypeConstraintImporter
{
    public static void Import(OdsRecordIndex index, DialectModelBuilder builder)
    {
        foreach (var record in index.GetRecordsWithBaseClass("Type"))
        {
            if (record.HasBaseClass("TypeDef")
                || !index.TryGetTypeConstraintKind(record, out var constraintKind, out var canonicalTypeName))
            {
                continue;
            }

            builder.AddSharedTypeConstraint(new TypeConstraintModel(record.Name, record.Name, constraintKind, canonicalTypeName));
        }
    }
}
