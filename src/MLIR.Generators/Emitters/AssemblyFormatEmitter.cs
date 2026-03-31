namespace MLIR.Generators.Emitters;

using System.Collections.Generic;
using System.Globalization;
using System.Text;
using MLIR.ODS.Model;

internal static class AssemblyFormatEmitter
{
    private static string GetOperandBindExpression(OperationBodySyntaxConstructionPlan plan, string operandName, int operandIndex)
    {
        if (plan.OperandFields.TryGetValue(operandName, out var fieldName))
        {
            return "binder.BindValueReference(body." + fieldName + ")";
        }

        if (plan.OperandsField != null)
        {
            return "binder.BindValueReference(body." + plan.OperandsField + "[" + operandIndex.ToString(CultureInfo.InvariantCulture) + "])";
        }

        throw new InvalidOperationException("No body field was generated for operand '" + operandName + "'.");
    }

    private static string GetAttributeBindExpression(OperationBodySyntaxConstructionPlan plan, string attributeName)
    {
        if (plan.AttributeFields.TryGetValue(attributeName, out var fieldName))
        {
            return "new NamedAttribute(" + EmitterHelpers.ToCSharpStringLiteral(attributeName) + ", binder.BindAttributeValue(body." + fieldName + "))";
        }

        throw new InvalidOperationException("No body field was generated for attribute '" + attributeName + "'.");
    }

    private static string GetTypeBindExpression(OperationBodySyntaxConstructionPlan plan)
    {
        if (plan.TypeField == null)
        {
            return "null";
        }

        return "binder.BindTypeReference(body." + plan.TypeField + ")";
    }

    public static void Emit(StringBuilder builder, OperationModel operation, OperationBodySyntaxMetadata bodySyntaxMetadata)
    {
        var className = DialectGeneratorNaming.GetOperationClassName(operation);
        var syntaxDescriptor = OperationBodySyntaxDescriptor.Describe(bodySyntaxMetadata);

        builder.AppendLine("public sealed class " + className + "AssemblyFormat : IOperationAssemblyFormat");
        builder.AppendLine("{");
        builder.AppendLine("    public bool TryParse(SyntaxToken nameToken, IReadOnlyList<SyntaxToken> resultTokens, IReadOnlyList<SyntaxToken> resultCommaTokens, SyntaxToken? equalsToken, OperationParsingContext context, out OperationBodySyntax? body)");
        builder.AppendLine("    {");
        builder.AppendLine("        body = null;");
        builder.AppendLine("        return false;");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    public Operation Bind(OperationSyntax syntax, OperationDefinition definition, Binder binder)");
        builder.AppendLine("    {");
        builder.AppendLine("        if (syntax.Body is not " + className + "BodySyntax body)");
        builder.AppendLine("        {");
        builder.AppendLine("            binder.Report(new AssemblyDiagnostic(syntax.Location, \"Expected a " + className + "BodySyntax but found \" + syntax.Body.GetType().Name + \".\"));");
        builder.AppendLine("            return new UninterpretedOperation(syntax, definition.Name);");
        builder.AppendLine("        }");
        builder.AppendLine("        if (syntax.ResultTokens.Count != " + operation.Results.Count.ToString(CultureInfo.InvariantCulture) + ")");
        builder.AppendLine("        {");
        builder.AppendLine("            binder.Report(new AssemblyDiagnostic(syntax.Location, \"Expected exactly " + operation.Results.Count.ToString(CultureInfo.InvariantCulture) + " result(s) but found \" + syntax.ResultTokens.Count + \".\"));");
        builder.AppendLine("            return new UninterpretedOperation(syntax, definition.Name);");
        builder.AppendLine("        }");
        builder.AppendLine("        return new " + className + "(");
        builder.AppendLine("            syntax,");
        builder.AppendLine("            definition.Name,");
        builder.AppendLine("            definition,");

        for (var i = 0; i < operation.Operands.Count; i++)
        {
            builder.AppendLine("            " + GetOperandBindExpression(syntaxDescriptor, operation.Operands[i], i) + ",");
        }

        for (var i = 0; i < operation.Results.Count; i++)
        {
            builder.AppendLine("            binder.BindValueReference(syntax.ResultTokens[" + i.ToString(CultureInfo.InvariantCulture) + "]),");
        }

        for (var i = 0; i < operation.Attributes.Count; i++)
        {
            builder.AppendLine("            " + GetAttributeBindExpression(syntaxDescriptor, operation.Attributes[i]) + ",");
        }

        builder.AppendLine("            " + GetTypeBindExpression(syntaxDescriptor) + ");");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    public OperationSyntax BuildCustomAssemblySyntax(Operation operation, ConcreteSyntaxBuilderContext context)");
        builder.AppendLine("    {");
        builder.AppendLine("        return context.RewriteOperation(operation, context.TransformGenericBody(operation));");
        builder.AppendLine("    }");
        builder.AppendLine("}");
    }
}
