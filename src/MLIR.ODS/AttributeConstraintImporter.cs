namespace MLIR.ODS;

using MLIR.ODS.Model;

internal static class AttributeConstraintImporter
{
    public static void Import(OdsRecordIndex index, DialectModelBuilder builder)
    {
        foreach (var record in index.GetRecordsWithBaseClass("Attr"))
        {
            if (record.HasBaseClass("AttrDef")
                || !index.TryGetAttributeConstraintKind(record, out var constraintKind))
            {
                continue;
            }

            builder.AddSharedAttributeConstraint(new AttributeConstraintModel(record.Name, record.Name, constraintKind));
        }
    }
}
