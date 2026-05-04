namespace MLIR.Generators.Emitters.Operation;

using System;
using System.Collections.Generic;
using System.Text;
using MLIR.Generators.Emitters;
using MLIR.Generators.Emitters.Common;
using MLIR.ODS.Model;

internal static class OperationDefinitionEmitter
{
    public static void Emit(StringBuilder builder, string className, OperationModel operation, DialectSymbolResolver resolver)
    {
        var requiredVariables = operation.AssemblyFormat != null
            ? AssemblyFormatAnalyzer.GetRequiredVariables(operation)
            : new HashSet<string>(StringComparer.Ordinal);

        builder.AppendLine("    public static OperationDefinition OperationDefinition { get; } = CreateOperationDefinition();");
        builder.AppendLine("    public override string Name => OperationDefinition.Name;");
        builder.AppendLine("    public override OperationDefinition? Definition => OperationDefinition;");
        builder.AppendLine();
        builder.AppendLine("    private static OperationDefinition CreateOperationDefinition()");
        builder.AppendLine("    {");
        builder.AppendLine("        var operation = new OperationDefinitionBuilder(" + EmitterHelpers.ToCSharpStringLiteral(operation.Name) + ");");

        foreach (var operand in operation.Operands)
        {
            builder.AppendLine("        operation.Operand(" + EmitterHelpers.ToCSharpStringLiteral(operand.Name) + ");");
        }

        foreach (var result in operation.Results)
        {
            builder.AppendLine("        operation.Result(" + EmitterHelpers.ToCSharpStringLiteral(result.Name) + ");");
        }

        foreach (var attribute in operation.Attributes)
        {
            var expectedConstraint = EmitterHelpers.TryGetAttributeConstraint(operation, attribute.Name);
            var expectedConstraintExpr = !string.IsNullOrEmpty(expectedConstraint)
                ? resolver.TryResolveAttributeConstraintDefinitionExpression(expectedConstraint!)
                : null;
            var constraintSuffix = !string.IsNullOrEmpty(expectedConstraintExpr)
                ? ", " + expectedConstraintExpr
                : string.Empty;
            if (requiredVariables.Contains(attribute.Name))
            {
                builder.AppendLine("        operation.RequiredAttribute(" + EmitterHelpers.ToCSharpStringLiteral(attribute.Name) + constraintSuffix + ");");
            }
            else
            {
                builder.AppendLine("        operation.OptionalAttribute(" + EmitterHelpers.ToCSharpStringLiteral(attribute.Name) + constraintSuffix + ");");
            }
        }

        builder.AppendLine("        operation.WithFactory(static context => new " + className + "(context));");
        var assemblyFormatFactoryExpr = OperationAssemblyExtensionHelpers.GetAssemblyFormatFactoryExpression(operation, className);
        if (assemblyFormatFactoryExpr != null)
        {
            builder.AppendLine("        operation.WithAssemblyFormat(" + assemblyFormatFactoryExpr + ");");
        }

        builder.AppendLine("        return operation.Build();");
        builder.AppendLine("    }");
        builder.AppendLine();
    }
}
