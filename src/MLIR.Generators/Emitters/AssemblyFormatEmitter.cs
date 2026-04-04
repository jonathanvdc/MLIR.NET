namespace MLIR.Generators.Emitters;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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

    private static string GetOperandBindExpression(
        OperationModel operation,
        OperationBodySyntaxConstructionPlan plan,
        OperationBodySyntaxMetadata metadata,
        string operandName,
        int operandIndex)
    {
        if (plan.OperandFields.TryGetValue(operandName, out var fieldName))
        {
            // Check if this operand is variadic by inspecting the body field type.
            // Variadic fields have type IReadOnlyList<SyntaxToken>.
            var field = metadata.Fields.FirstOrDefault(f => f.Name == fieldName);
            if (field != null && field.CsType.Contains("IReadOnlyList", StringComparison.Ordinal))
            {
                // Produce a list of bound values from the list of SSA tokens.
                return "body." + fieldName + ".Select(t => (Value)binder.BindValueReference(t)).ToList()";
            }

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

        throw new InvalidOperationException(
            "No body field was generated for operand '" + operandName + "' while generating operation '" + operation.Name +
            "'. The assembly format and generated body syntax may be out of sync.");
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

        // The attribute is not referenced explicitly in the assembly format, meaning it lives
        // entirely in the attr-dict.  Extract it by name at binding time so it still ends up
        // in the typed attribute collection.  The result is always nullable because the
        // attribute may not appear in a given instance.
        var attrDictFieldName = plan.AttrDictField ?? plan.AttrDictWithKeywordField ?? plan.PropDictField;
        if (attrDictFieldName != null)
        {
            var quotedNameForAttrDict = EmitterHelpers.ToCSharpStringLiteral(attributeName);
            return "body." + attrDictFieldName + ".Where(a => a.Name == " + quotedNameForAttrDict +
                   ").Select(a => (NamedAttribute?)binder.BindNamedAttribute(a)).FirstOrDefault()";
        }

        throw new InvalidOperationException(
            "No body field was generated for attribute '" + attributeName + "' while generating operation '" + operation.Name +
            "'. The assembly format and generated body syntax may be out of sync.");
    }

    private static string GetTypeBindExpression(OperationBodySyntaxConstructionPlan plan, OperationBodySyntaxMetadata metadata)
    {
        if (plan.TypeField == null)
        {
            return "null";
        }

        if (GetFieldCsType(metadata, plan.TypeField) == "IReadOnlyList<TypeSyntax>")
        {
            // type($variadic) stores several surface-level type syntax nodes and does not
            // correspond to a single semantic TypeReference on the operation.
            return "null";
        }

        // When the type field is nullable (e.g. inside an optional group), emit a conditional
        // expression that produces null when the type syntax is absent rather than passing
        // null directly to binder.BindTypeReference which requires a non-null argument.
        if (IsNullableField(metadata, plan.TypeField))
        {
            return "body." + plan.TypeField + " is not null ? (TypeReference?)binder.BindTypeReference(body." + plan.TypeField + "!) : null";
        }

        return "binder.BindTypeReference(body." + plan.TypeField + ")";
    }

    private static string GetRegionBindExpression(OperationBodySyntaxMetadata metadata, string fieldName, bool isVariadic)
    {
        if (isVariadic)
        {
            return "body." + fieldName + ".Select(region => binder.BindRegion(region)).ToList()";
        }

        if (IsNullableField(metadata, fieldName))
        {
            return "body." + fieldName + " is not null ? (Region?)binder.BindRegion(body." + fieldName + "!) : null";
        }

        return "binder.BindRegion(body." + fieldName + ")";
    }

    private static string GetRegionsBindExpression(OperationModel operation, OperationBodySyntaxConstructionPlan plan, OperationBodySyntaxMetadata metadata)
    {
        var parts = new List<string>();
        for (var i = 0; i < operation.Regions.Count; i++)
        {
            var fieldName = plan.RegionFields[i];
            parts.Add(GetRegionBindExpression(metadata, fieldName, operation.Regions[i].IsVariadic));
        }

        if (parts.Count == 1)
        {
            return "new global::System.Collections.Generic.List<Region> { " + parts[0] + " }";
        }

        return "new global::System.Collections.Generic.List<Region>(" + parts.Count.ToString(CultureInfo.InvariantCulture) + ") { " + string.Join(", ", parts) + " }";
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
        var hasVariadicResults = operation.Results.Any(static result => result.IsVariadic);
        var minimumResultCount = 0;
        foreach (var result in operation.Results)
        {
            if (result.IsVariadic)
            {
                break;
            }

            minimumResultCount++;
        }

        var resultCountCondition = hasVariadicResults
            ? "syntax.ResultList.Count < " + minimumResultCount.ToString(CultureInfo.InvariantCulture)
            : "syntax.ResultList.Count != " + operation.Results.Count.ToString(CultureInfo.InvariantCulture);
        var resultCountMessage = hasVariadicResults
            ? "\"Expected at least " + minimumResultCount.ToString(CultureInfo.InvariantCulture) + " result(s) but found \" + syntax.ResultList.Count + \".\""
            : "\"Expected exactly " + operation.Results.Count.ToString(CultureInfo.InvariantCulture) + " result(s) but found \" + syntax.ResultList.Count + \".\"";
        builder.AppendLine("        if (" + resultCountCondition + ")");
        builder.AppendLine("        {");
        builder.AppendLine("            binder.Report(new AssemblyDiagnostic(syntax.Location, " + resultCountMessage + "));");
        builder.AppendLine("            return new UninterpretedOperation(syntax, definition.Name);");
        builder.AppendLine("        }");
        builder.AppendLine("        return new " + className + "(");
        builder.AppendLine("            syntax,");

        if (operation.Regions.Count > 0)
        {
            builder.AppendLine("            " + GetRegionsBindExpression(operation, syntaxDescriptor, bodySyntaxMetadata) + ",");
        }

        for (var i = 0; i < operation.Operands.Count; i++)
        {
            builder.AppendLine("            " + GetOperandBindExpression(operation, syntaxDescriptor, bodySyntaxMetadata, operation.Operands[i].Name, i) + ",");
        }

        var resultSlotIndex = 0;
        for (var i = 0; i < operation.Results.Count; i++)
        {
            var result = operation.Results[i];
            if (result.IsVariadic)
            {
                // Variadic result: collect all remaining result tokens as a list.
                var skip = resultSlotIndex.ToString(CultureInfo.InvariantCulture);
                builder.AppendLine("            syntax.ResultList.Skip(" + skip + ").Select(static t => new OperationResult(t)).ToList(),");
                resultSlotIndex = -1;
            }
            else
            {
                builder.AppendLine("            new OperationResult(syntax.ResultList[" + resultSlotIndex.ToString(CultureInfo.InvariantCulture) + "]),");
                resultSlotIndex++;
            }
        }

        if (operation.Attributes.Count == 0)
        {
            builder.AppendLine("            NamedAttributeCollection.Empty,");
        }
        else
        {
            // Determine whether any attribute field is optional (nullable body field) or is only
            // present in attr-dict (which makes it implicitly optional).
            var hasOptionalAttributes = false;
            for (var i = 0; i < operation.Attributes.Count; i++)
            {
                if (syntaxDescriptor.AttributeFields.TryGetValue(operation.Attributes[i].Name, out var fieldNameCheck) &&
                    IsNullableField(bodySyntaxMetadata, fieldNameCheck))
                {
                    hasOptionalAttributes = true;
                    break;
                }

                // Attributes that live exclusively in attr-dict (no explicit body field) are
                // always optional at bind time because they may or may not appear.
                if (!syntaxDescriptor.AttributeFields.ContainsKey(operation.Attributes[i].Name) &&
                    (syntaxDescriptor.AttrDictField ?? syntaxDescriptor.AttrDictWithKeywordField ?? syntaxDescriptor.PropDictField) != null)
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

                    builder.Append(GetAttributeBindExpression(operation, syntaxDescriptor, bodySyntaxMetadata, operation.Attributes[i].Name, resolver));
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

                    builder.Append(GetAttributeBindExpression(operation, syntaxDescriptor, bodySyntaxMetadata, operation.Attributes[i].Name, resolver));
                }

                builder.AppendLine("),");
            }
        }

        builder.AppendLine("            " + GetTypeBindExpression(syntaxDescriptor, bodySyntaxMetadata) + ");");
        builder.AppendLine("    }");
        builder.AppendLine();
        BuildCustomAssemblySyntaxEmitter.Emit(builder, operation, bodySyntaxMetadata, resolver);
        builder.AppendLine("}");
    }
}
