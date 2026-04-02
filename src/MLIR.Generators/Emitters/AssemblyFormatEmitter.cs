namespace MLIR.Generators.Emitters;

using System.Collections.Generic;
using System.Globalization;
using System.Text;
using MLIR.ODS.Model;

internal static class AssemblyFormatEmitter
{
    /// <summary>
    /// Returns the C# type of the body field with the given name, or <c>null</c> if the field
    /// is not found in the metadata.
    /// </summary>
    private static string? GetFieldCsType(OperationBodySyntaxMetadata metadata, string fieldName)
    {
        foreach (var f in metadata.Fields)
        {
            if (f.Name == fieldName)
            {
                return f.CsType;
            }
        }

        return null;
    }

    /// <summary>
    /// Returns <c>true</c> when the body field with the given name has a nullable type (i.e., its
    /// C# type ends with <c>?</c>), meaning the field may be absent.
    /// </summary>
    private static bool IsNullableField(OperationBodySyntaxMetadata metadata, string fieldName)
    {
        var csType = GetFieldCsType(metadata, fieldName);
        return csType != null && csType.EndsWith("?", System.StringComparison.Ordinal);
    }

    /// <summary>
    /// Returns an expression for reading <paramref name="fieldName"/> from <c>body</c> in
    /// a way that is safe regardless of whether the field is a nullable value type
    /// (<c>SyntaxToken?</c>) or a nullable reference type (<c>TypeSyntax?</c>, etc.).
    /// This helper is used for directives that always expect a value (e.g. type directives).
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
        var csType = GetFieldCsType(metadata, fieldName);

        if (csType == "SyntaxToken?")
        {
            // Nullable value type: unwrap with ?? default so the expression has type SyntaxToken.
            return "(body." + fieldName + " ?? default)";
        }

        if (csType != null && csType.EndsWith("?", System.StringComparison.Ordinal))
        {
            // Nullable reference type: use null-forgiving to satisfy the non-null parameter.
            return "body." + fieldName + "!";
        }

        return "body." + fieldName;
    }

    private static string GetOperandBindExpression(OperationBodySyntaxConstructionPlan plan, OperationBodySyntaxMetadata metadata, string operandName, int operandIndex)
    {
        if (plan.OperandFields.TryGetValue(operandName, out var fieldName))
        {
            // When the body field is nullable (e.g. SyntaxToken? for an optional group operand),
            // emit a conditional expression that produces null when the operand is absent.
            if (IsNullableField(metadata, fieldName))
            {
                var access = "body." + fieldName;
                return access + ".HasValue ? (Value?)binder.BindValueReference(" + access + ".Value) : null";
            }

            return "binder.BindValueReference(body." + fieldName + ")";
        }

        if (plan.OperandsField != null)
        {
            return "binder.BindValueReference(body." + plan.OperandsField + "[" + operandIndex.ToString(CultureInfo.InvariantCulture) + "])";
        }

        throw new InvalidOperationException("No body field was generated for operand '" + operandName + "'.");
    }

    private static string GetAttributeBindExpression(
        OperationModel operation,
        OperationBodySyntaxConstructionPlan plan,
        OperationBodySyntaxMetadata metadata,
        string attributeName,
        DialectSymbolResolver resolver)
    {
        if (plan.AttributeFields.TryGetValue(attributeName, out var fieldName))
        {
            var quotedName = EmitterHelpers.ToCSharpStringLiteral(attributeName);
            var access = "body." + fieldName;
            var expectedConstraint = EmitterHelpers.TryGetAttributeConstraint(operation, attributeName);
            var expectedDefinitionExpr = !string.IsNullOrEmpty(expectedConstraint)
                ? resolver.TryResolveAttributeConstraintDefinitionExpression(expectedConstraint!)
                : null;
            var bindCall = "new NamedAttribute(" + quotedName + ", binder.BindAttributeValue(" + access;
            if (!string.IsNullOrEmpty(expectedDefinitionExpr))
            {
                bindCall += ", " + expectedDefinitionExpr;
            }

            bindCall += "))";

            // When the body field is nullable (e.g. AttributeValueSyntax? for an oilist clause),
            // emit a conditional expression that produces null when the attribute is absent.
            if (IsNullableField(metadata, fieldName))
            {
                return access + " is not null ? " + bindCall + " : (NamedAttribute?)null";
            }

            return bindCall;
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

    public static void Emit(StringBuilder builder, OperationModel operation, OperationBodySyntaxMetadata bodySyntaxMetadata, DialectSymbolResolver resolver)
    {
        var className = DialectGeneratorNaming.GetOperationClassName(operation);
        var syntaxDescriptor = OperationBodySyntaxDescriptor.Describe(bodySyntaxMetadata);

        builder.AppendLine("public sealed class " + className + "AssemblyFormat : IOperationAssemblyFormat");
        builder.AppendLine("{");
        TryParseEmitter.Emit(builder, operation, bodySyntaxMetadata, resolver);
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

        for (var i = 0; i < operation.Operands.Count; i++)
        {
            builder.AppendLine("            " + GetOperandBindExpression(syntaxDescriptor, bodySyntaxMetadata, operation.Operands[i], i) + ",");
        }

        for (var i = 0; i < operation.Results.Count; i++)
        {
            builder.AppendLine("            new OperationResult(syntax.ResultTokens[" + i.ToString(CultureInfo.InvariantCulture) + "]),");
        }

        if (operation.Attributes.Count == 0)
        {
            builder.AppendLine("            NamedAttributeCollection.Empty,");
        }
        else
        {
            // Determine whether any attribute field is optional (nullable body field).
            var hasOptionalAttributes = false;
            for (var i = 0; i < operation.Attributes.Count; i++)
            {
                if (syntaxDescriptor.AttributeFields.TryGetValue(operation.Attributes[i], out var fieldNameCheck) &&
                    IsNullableField(bodySyntaxMetadata, fieldNameCheck))
                {
                    hasOptionalAttributes = true;
                    break;
                }
            }

            if (hasOptionalAttributes)
            {
                // Some attributes may be absent: use a nullable array + LINQ filter so that
                // only the present attributes end up in the collection.
                builder.Append("            new NamedAttributeCollection(new NamedAttribute?[] { ");
                for (var i = 0; i < operation.Attributes.Count; i++)
                {
                    if (i > 0)
                    {
                        builder.Append(", ");
                    }

                    builder.Append(GetAttributeBindExpression(operation, syntaxDescriptor, bodySyntaxMetadata, operation.Attributes[i], resolver));
                }

                builder.AppendLine(" }.Where(a => a is not null).Select(a => a!)),");
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

                    builder.Append(GetAttributeBindExpression(operation, syntaxDescriptor, bodySyntaxMetadata, operation.Attributes[i], resolver));
                }

                builder.AppendLine("),");
            }
        }

        builder.AppendLine("            " + GetTypeBindExpression(syntaxDescriptor, bodySyntaxMetadata) + ");");
        builder.AppendLine("    }");
        builder.AppendLine();
        BuildCustomAssemblySyntaxEmitter.Emit(builder, operation, bodySyntaxMetadata);
        builder.AppendLine("}");
    }
}
