namespace MLIR.ODS;

using MLIR.ODS.Model;

/// <summary>
/// Imports MLIR.NET-specific operation assembly overlays into the operation model.
/// </summary>
internal static class OperationAssemblyExtensionRecordImporter
{
    public static void Import(OdsRecordIndex index, DialectModelBuilder builder)
    {
        foreach (var record in index.GetRecordsWithBaseClass("Op"))
        {
            if (!index.TryGetStringField(record, "csharpAsmFormatCode", out var strategy)
                || !index.TryGetOperationName(record, out var mnemonic)
                || !index.TryGetDialectName(record, out var dialectName))
            {
                continue;
            }

            var dialect = builder.GetOrCreateDialect(dialectName);
            var operationName = dialectName + "." + mnemonic;
            var existingIndex = dialect.Operations.FindIndex(operation => operation.Name == operationName);
            if (existingIndex >= 0)
            {
                var existing = dialect.Operations[existingIndex];
                dialect.Operations[existingIndex] = new OperationModel(
                    existing.Name,
                    existing.ClassName,
                    existing.Regions,
                    existing.Operands,
                    existing.Results,
                    existing.Attributes,
                    existing.Summary,
                    existing.Description,
                    existing.AssemblyFormat,
                    existing.Traits,
                    strategy);
            }
        }
    }
}
