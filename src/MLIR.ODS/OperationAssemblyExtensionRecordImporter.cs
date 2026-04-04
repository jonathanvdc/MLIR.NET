namespace MLIR.ODS;

using MLIR.ODS.Model;

/// <summary>
/// Imports MLIR.NET-specific operation assembly extensions that overlay upstream ODS records.
/// </summary>
internal static class OperationAssemblyExtensionRecordImporter
{
    public static void Import(OdsRecordIndex index, DialectModelBuilder builder)
    {
        foreach (var record in index.GetRecordsWithBaseClass("MLIRNet_OpAsmExtension"))
        {
            if (!index.TryGetStringField(record, "opName", out var operationName)
                || !index.TryGetStringField(record, "csharpAsmFormatCode", out var strategy))
            {
                continue;
            }

            var separatorIndex = operationName.IndexOf('.');
            if (separatorIndex <= 0)
            {
                continue;
            }

            var dialect = builder.GetOrCreateDialect(operationName.Substring(0, separatorIndex));
            var existingIndex = dialect.Operations.FindIndex(operation => operation.Name == operationName);
            if (existingIndex >= 0)
            {
                var existing = dialect.Operations[existingIndex];
                dialect.Operations[existingIndex] = new OperationModel(
                    existing.Name,
                    existing.ClassName,
                    existing.Operands,
                    existing.Results,
                    existing.Attributes,
                    existing.Summary,
                    existing.Description,
                    existing.AssemblyFormat,
                    existing.Traits,
                    strategy);
            }
            else
            {
                dialect.Operations.Add(new OperationModel(operationName, assemblyExtensionKind: strategy));
            }
        }
    }
}
