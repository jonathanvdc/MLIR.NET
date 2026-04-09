namespace MLIR.ODS;

using MLIR.ODS.Model;
using TableGen.Evaluation;

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
            builder.AddSharedAttr(
                new AttrModel(
                    record.Name,
                    record.Name,
                    constraintKind,
                    enumModel,
                    elementConstraintRecordName,
                    summary: index.GetOptionalStringField(record, "summary"),
                    csharpStorageType: index.GetOptionalStringField(record, "csharpStorageType"),
                    csharpReturnType: index.GetOptionalStringField(record, "csharpReturnType"),
                    csharpConvertFromStorage: index.GetOptionalStringField(record, "csharpConvertFromStorage"),
                    csharpConstBuilderCall: index.GetOptionalStringField(record, "csharpConstBuilderCall"),
                    csharpDefaultValue: index.GetOptionalStringField(record, "csharpDefaultValue"),
                    csharpValueType: index.GetOptionalStringField(record, "csharpValueType"),
                    isOptional: record.Fields.TryGetValue("isOptional", out var isOptionalField)
                        && isOptionalField is BitValue bitValue
                        && bitValue.Value,
                    baseAttrRecordName: index.GetOptionalStringField(record, "baseAttr"),
                    cppNamespace: index.GetOptionalStringField(record, "cppNamespace"),
                    description: index.GetOptionalStringField(record, "description")));
        }
    }
}
