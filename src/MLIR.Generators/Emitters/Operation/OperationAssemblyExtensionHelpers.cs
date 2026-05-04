namespace MLIR.Generators.Emitters.Operation;

using MLIR.ODS.Model;

internal static class OperationAssemblyExtensionHelpers
{
    public static bool HasCustomAssembly(OperationModel operation)
    {
        return operation.AssemblyFormat != null || operation.AssemblyFormatCode != null;
    }

    public static string? GetAssemblyFormatFactoryExpression(OperationModel operation, string operationClassName)
    {
        if (operation.AssemblyFormat != null)
        {
            return "static _ => new " + operationClassName + "AssemblyFormat()";
        }

        var template = CodeTemplate.From(operation.AssemblyFormatCode, CodeTemplateKind.Expression);
        if (template == null)
        {
            return null;
        }

        template.RequireOnly("definition");
        return template.PlaceholderNames.Count == 0
            ? "static _ => " + template.Text
            : "static definition => " + template.Render("definition", "definition");
    }
}
