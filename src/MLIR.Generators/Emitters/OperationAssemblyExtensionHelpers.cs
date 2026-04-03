namespace MLIR.Generators.Emitters;

using MLIR.ODS.Model;

internal static class OperationAssemblyExtensionHelpers
{
    public static bool HasCustomAssembly(OperationModel operation)
    {
        return operation.AssemblyFormat != null || operation.AssemblyExtensionKind != null;
    }

    public static string? GetAssemblyFormatInstantiationExpression(OperationModel operation, string operationClassName)
    {
        if (operation.AssemblyFormat != null)
        {
            return "new " + operationClassName + "AssemblyFormat()";
        }

        return operation.AssemblyExtensionKind switch
        {
            "select_like" => "new global::MLIR.Dialects.Extensions.SelectLikeOperationAssemblyFormat()",
            _ => null,
        };
    }
}
