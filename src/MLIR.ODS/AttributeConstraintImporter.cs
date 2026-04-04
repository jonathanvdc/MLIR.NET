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

            EnumModel? enumModel = null;
            if (constraintKind == AttributeConstraintKind.EnumAttribute)
            {
                index.TryGetEnumModel(record, out enumModel);
            }

            var elementConstraintRecordName = constraintKind == AttributeConstraintKind.TypedArrayAttribute
                ? index.GetOptionalStringField(record, "elementAttr")
                : null;

            builder.AddSharedAttributeConstraint(
                new AttributeConstraintModel(record.Name, record.Name, constraintKind, elementConstraintRecordName, enumModel));
        }
    }
}
