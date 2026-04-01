namespace MLIR.Generators.Emitters;

using System.Collections.Generic;
using System.Globalization;
using System.Text;
using MLIR.ODS.Model;

internal static class AssemblyFormatEmitter
{
    /// <summary>
    /// Returns an expression for reading <paramref name="fieldName"/> from <c>body</c> in
    /// a way that is safe regardless of whether the field is a nullable value type
    /// (<c>SyntaxToken?</c>) or a nullable reference type (<c>TypeSyntax?</c>, etc.).
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item>
    ///   <c>SyntaxToken?</c> — a nullable value type — cannot be implicitly passed where a
    ///   non-nullable <c>SyntaxToken</c> is expected (CS1503).  We unwrap with <c>?? default</c>.
    /// </item>
    /// <item>
    ///   Nullable reference types (<c>AttributeValueSyntax?</c>, <c>TypeSyntax?</c>) generate
    ///   a possible-null-reference warning (CS8604).  We suppress with the null-forgiving
    ///   operator (<c>!</c>).
    /// </item>
    /// </list>
    /// </remarks>
    private static string SafeFieldAccess(OperationBodySyntaxMetadata metadata, string fieldName)
    {
        foreach (var f in metadata.Fields)
        {
            if (f.Name != fieldName)
            {
                continue;
            }

            if (f.CsType == "SyntaxToken?")
            {
                // Nullable value type: unwrap with ?? default so the expression has type SyntaxToken.
                return "(body." + fieldName + " ?? default)";
            }

            if (f.CsType.EndsWith("?", System.StringComparison.Ordinal))
            {
                // Nullable reference type: use null-forgiving to satisfy the non-null parameter.
                return "body." + fieldName + "!";
            }

            break;
        }

        return "body." + fieldName;
    }

    private static string GetOperandBindExpression(OperationBodySyntaxConstructionPlan plan, OperationBodySyntaxMetadata metadata, string operandName, int operandIndex)
    {
        if (plan.OperandFields.TryGetValue(operandName, out var fieldName))
        {
            return "binder.BindValueReference(" + SafeFieldAccess(metadata, fieldName) + ")";
        }

        if (plan.OperandsField != null)
        {
            return "binder.BindValueReference(body." + plan.OperandsField + "[" + operandIndex.ToString(CultureInfo.InvariantCulture) + "])";
        }

        throw new InvalidOperationException("No body field was generated for operand '" + operandName + "'.");
    }

    private static string GetAttributeBindExpression(OperationBodySyntaxConstructionPlan plan, OperationBodySyntaxMetadata metadata, string attributeName)
    {
        if (plan.AttributeFields.TryGetValue(attributeName, out var fieldName))
        {
            return "new NamedAttribute(" + EmitterHelpers.ToCSharpStringLiteral(attributeName) + ", binder.BindAttributeValue(" + SafeFieldAccess(metadata, fieldName) + "))";
        }

        throw new InvalidOperationException("No body field was generated for attribute '" + attributeName + "'.");
    }

    private static string GetTypeBindExpression(OperationBodySyntaxConstructionPlan plan, OperationBodySyntaxMetadata metadata)
    {
        if (plan.TypeField == null)
        {
            return "null";
        }

        return "binder.BindTypeReference(" + SafeFieldAccess(metadata, plan.TypeField) + ")";
    }

    public static void Emit(StringBuilder builder, OperationModel operation, OperationBodySyntaxMetadata bodySyntaxMetadata)
    {
        var className = DialectGeneratorNaming.GetOperationClassName(operation);
        var syntaxDescriptor = OperationBodySyntaxDescriptor.Describe(bodySyntaxMetadata);

        builder.AppendLine("public sealed class " + className + "AssemblyFormat : IOperationAssemblyFormat");
        builder.AppendLine("{");
        TryParseEmitter.Emit(builder, operation, bodySyntaxMetadata);
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
            builder.AppendLine("            " + GetOperandBindExpression(syntaxDescriptor, bodySyntaxMetadata, operation.Operands[i], i) + ",");
        }

        for (var i = 0; i < operation.Results.Count; i++)
        {
            builder.AppendLine("            binder.BindValueReference(syntax.ResultTokens[" + i.ToString(CultureInfo.InvariantCulture) + "]),");
        }

        if (operation.Attributes.Count == 0)
        {
            builder.AppendLine("            NamedAttributeCollection.Empty,");
        }
        else
        {
            builder.Append("            NamedAttributeCollection.Create(");
            for (var i = 0; i < operation.Attributes.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append(", ");
                }

                builder.Append(GetAttributeBindExpression(syntaxDescriptor, bodySyntaxMetadata, operation.Attributes[i]));
            }

            builder.AppendLine("),");
        }

        builder.AppendLine("            " + GetTypeBindExpression(syntaxDescriptor, bodySyntaxMetadata) + ");");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    public OperationSyntax BuildCustomAssemblySyntax(Operation operation, ConcreteSyntaxBuilderContext context)");
        builder.AppendLine("    {");
        builder.AppendLine("        return context.RewriteOperation(operation, context.TransformGenericBody(operation));");
        builder.AppendLine("    }");
        builder.AppendLine("}");
    }
}
