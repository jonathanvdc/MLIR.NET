namespace MLIR.Generators.Emitters.Operation;

using MLIR.ODS.Model;

internal static class OperationAssemblyExtensionHelpers
{
    public static bool HasCustomAssembly(OperationModel operation)
    {
        return operation.AssemblyFormat != null || operation.AssemblyFormatCode != null;
    }

    public static string? GetAssemblyFormatInstantiationExpression(OperationModel operation, string operationClassName)
    {
        if (operation.AssemblyFormat != null)
        {
            return "new " + operationClassName + "AssemblyFormat()";
        }

        return operation.AssemblyFormatCode;
    }
}
