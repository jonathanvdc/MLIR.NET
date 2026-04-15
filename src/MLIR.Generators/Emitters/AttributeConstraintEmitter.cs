namespace MLIR.Generators.Emitters;

using System.Text;
using MLIR.Generators.Emitters.Common;
using MLIR.ODS.Model;

internal static class AttributeConstraintEmitter
{
    public static void Emit(StringBuilder builder, AttributeConstraintModel attributeConstraint, DialectSymbolResolver resolver)
    {
        var strategy = resolver.TryResolveAttributeConstraintStrategy(attributeConstraint.RecordName);
        if (strategy.EmissionKind != AttributeConstraintEmissionKind.StaticDefinition)
        {
            throw new System.InvalidOperationException(
                "Unsupported attribute constraint emission kind '"
                + strategy.EmissionKind
                + "' for constraint '"
                + attributeConstraint.RecordName
                + "'.");
        }

        EmitStaticConstraintDefinition(builder, attributeConstraint, strategy);
        if (attributeConstraint.EnumModel != null)
        {
            builder.AppendLine();
            EmitEnumConstraintAssemblyFormat(builder, attributeConstraint, attributeConstraint.EnumModel);
        }
    }

    /// <summary>
    /// Emits a minimal static class that carries only the
    /// <c>AttributeConstraintDefinition</c> for constraints that bind to existing
    /// semantic storage types and do not need generated wrapper classes.
    /// </summary>
    private static void EmitStaticConstraintDefinition(
        StringBuilder builder,
        AttributeConstraintModel attributeConstraint,
        AttributeConstraintCodeStrategy strategy)
    {
        var className = DialectGeneratorNaming.GetAttributeConstraintClassName(attributeConstraint);
        builder.AppendLine("public static class " + className);
        builder.AppendLine("{");
        builder.AppendLine("    public static AttributeConstraintDefinition AttributeConstraintDefinition { get; } =");
        AppendAttributeConstraintDefinition(builder, attributeConstraint, strategy, "        ");
        builder.AppendLine("}");
    }

    private static void AppendAttributeConstraintDefinition(
        StringBuilder builder,
        AttributeConstraintModel attributeConstraint,
        AttributeConstraintCodeStrategy strategy,
        string indent)
    {
        AppendAttributeConstraintDefinition(
            builder,
            attributeConstraint.Name,
            GetAssemblyFormatExpression(attributeConstraint.RecordName, strategy),
            indent);
    }

    private static void AppendAttributeConstraintDefinition(
        StringBuilder builder,
        string constraintName,
        string? assemblyFormatExpression,
        string indent)
    {
        builder.Append(indent);
        builder.Append("new AttributeConstraintDefinition(");
        builder.Append(EmitterHelpers.ToCSharpStringLiteral(constraintName));
        if (assemblyFormatExpression != null)
        {
            builder.Append(", ");
            builder.Append(assemblyFormatExpression);
        }

        builder.AppendLine(");");
    }

    private static string? GetAssemblyFormatExpression(string recordName, AttributeConstraintCodeStrategy strategy)
    {
        var assemblyFormatExpression = strategy.GetAssemblyFormatConstructionExpression(recordName);
        if (assemblyFormatExpression != null)
        {
            return assemblyFormatExpression;
        }

        var assemblyFormatType = strategy.GetAssemblyFormatType(recordName);
        return assemblyFormatType != null ? "new " + assemblyFormatType + "()" : null;
    }

    private static void EmitEnumConstraintAssemblyFormat(StringBuilder builder, AttributeConstraintModel attributeConstraint, EnumModel enumModel)
    {
        var enumTypeName = EnumHelpers.GetCSharpEnumTypeName(enumModel);

        // Assembly format class for enum constraint
        builder.AppendLine("internal sealed class " + EnumEmitter.GetEnumConstraintAssemblyFormatTypeName(attributeConstraint.RecordName) + " : IAttributeAssemblyFormat");
        builder.AppendLine("{");
        builder.AppendLine("    private static " + enumTypeName + " ParseEnumValue(MLIR.Syntax.AttributeValueSyntax? syntax)");
        builder.AppendLine("    {");
        builder.AppendLine("        if (syntax == null) return default;");
        builder.AppendLine("        if (syntax is MLIR.Syntax.Attributes.Primitives.IntegerAttributeValueSyntax integerSyntax)");
        builder.AppendLine("        {");
        builder.AppendLine("            return " + EnumEmitter.GetIntegerToEnumExpression(enumModel, "integerSyntax.Value", "default") + ";");
        builder.AppendLine("        }");
        builder.AppendLine("        var raw = syntax.ToString();");
        EnumEmitter.EmitParseExpression(builder, enumModel, enumTypeName, "raw", "        ");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    public ParseResult<AttributeValueSyntax> TryParse(AttributeParsingContext context)");
        builder.AppendLine("    {");
        builder.AppendLine("        if (!context.TryMatch(MLIR.Text.TokenKind.Identifier, out var firstToken)");
        builder.AppendLine("            && !context.TryMatch(MLIR.Text.TokenKind.StringLiteral, out firstToken))");
        builder.AppendLine("        {");
        builder.AppendLine("            return ParseResult<AttributeValueSyntax>.NoMatch();");
        builder.AppendLine("        }");
        builder.AppendLine();
        builder.AppendLine("        var rawText = firstToken.Text;");
        if (enumModel.IsBitEnum)
        {
            var sepKind = EnumEmitter.GetSeparatorTokenKind(enumModel);
            builder.AppendLine("        while (context.TryMatch(MLIR.Text." + sepKind + ", out _))");
            builder.AppendLine("        {");
            builder.AppendLine("            if (!context.TryMatch(MLIR.Text.TokenKind.Identifier, out var nextToken)");
            builder.AppendLine("                && !context.TryMatch(MLIR.Text.TokenKind.StringLiteral, out nextToken))");
            builder.AppendLine("            {");
            builder.AppendLine("                break;");
            builder.AppendLine("            }");
            builder.AppendLine();
            builder.AppendLine("            rawText += " + EmitterHelpers.ToCSharpStringLiteral(enumModel.Separator) + " + nextToken.Text;");
            builder.AppendLine("        }");
        }

        builder.AppendLine();
        builder.AppendLine("        return ParseResult<AttributeValueSyntax>.Success(new MLIR.Syntax.RawAttributeValueSyntax(new MLIR.Syntax.RawSyntaxText(rawText)));");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    public AttributeValue Bind(AttributeValueSyntax syntax, AttributeConstraintDefinition definition, Binder binder)");
        builder.AppendLine("    {");
        builder.AppendLine("        return " + EnumEmitter.GetEnumToIntegerAttrExpression(enumModel, "ParseEnumValue(syntax)", "syntax") + ";");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    public AttributeValueSyntax BuildCustomAssemblySyntax(AttributeValue attribute, ConcreteSyntaxBuilderContext context)");
        builder.AppendLine("    {");
        builder.AppendLine("        if (attribute is global::MLIR.IntegerAttr integerAttr");
        builder.AppendLine("            && " + EnumEmitter.GetEnumInfoClassName(enumModel) + ".TryFromInteger(integerAttr.Value, out var enumValue))");
        builder.AppendLine("        {");
        builder.AppendLine("            var text = " + EnumEmitter.GetEnumInfoClassName(enumModel) + ".Format(enumValue);");
        builder.AppendLine("            return new MLIR.Syntax.RawAttributeValueSyntax(new MLIR.Syntax.RawSyntaxText(text));");
        builder.AppendLine("        }");
        builder.AppendLine();
        builder.AppendLine("        if (attribute.Syntax != null)");
        builder.AppendLine("        {");
        builder.AppendLine("            return attribute.Syntax;");
        builder.AppendLine("        }");
        builder.AppendLine();
        builder.AppendLine("        if (attribute is global::MLIR.IntegerAttr fallbackIntegerAttr)");
        builder.AppendLine("        {");
        builder.AppendLine("            return context.BuildAttributeValueSyntax(fallbackIntegerAttr);");
        builder.AppendLine("        }");
        builder.AppendLine();
        builder.AppendLine("        throw new global::System.InvalidOperationException(\"Enum constraints require IntegerAttr storage for custom assembly emission, but received \" + attribute.GetType().FullName + \".\");");
        builder.AppendLine("    }");
        builder.AppendLine("}");
    }

}
