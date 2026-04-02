namespace MLIR.ODS;

using TableGen.Evaluation;
using MLIR.ODS.Model;

/// <summary>
/// Translates interpreted TableGen records into a coarse ODS model.
/// </summary>
public static class DialectImporter
{
    /// <summary>
    /// Imports dialect models from an interpreted TableGen document.
    /// </summary>
    public static IReadOnlyList<DialectModel> Import(InterpretedDocument document)
    {
        var index = new OdsRecordIndex(document);
        var builder = new DialectModelBuilder();

        DialectRecordImporter.Import(index, builder);
        OperationRecordImporter.Import(index, builder);
        AttributeRecordImporter.Import(index, builder);
        AttributeConstraintImporter.Import(index, builder);
        TypeRecordImporter.Import(index, builder);

        return builder.Build();
    }
}
