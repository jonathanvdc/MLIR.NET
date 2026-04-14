namespace MLIR.Generators.Emitters;

using System.Collections.Generic;
using System.Text;
using MLIR.Generators.Emitters.Common;
using MLIR.ODS.Model;

internal static class AttributeConstraintEmitter
{
    public static void Emit(StringBuilder builder, AttributeConstraintModel attributeConstraint, DialectSymbolResolver resolver)
    {
        var strategy = resolver.TryResolveAttributeConstraintStrategy(attributeConstraint.RecordName);

        if (strategy.IsEnum && attributeConstraint.EnumModel != null)
        {
            EmitEnumConstraint(builder, attributeConstraint, attributeConstraint.EnumModel);
        }
        else if (strategy.IsTypedArray)
        {
            EmitTypedArrayConstraint(builder, attributeConstraint, resolver);
        }
        else if (strategy.IsDenseCollection)
        {
            EmitDenseCollectionConstraintDefinition(builder, attributeConstraint, strategy, resolver);
        }
        else if (strategy.GetFactoryExpression(attributeConstraint.RecordName) != null)
        {
            // Storage-backed constraint: emit only the static AttributeConstraintDefinition.
            // No constraint wrapper class is generated.
            EmitPrimitiveConstraintDefinition(builder, attributeConstraint, strategy);
        }
        else
        {
            EmitStandardConstraint(builder, attributeConstraint, strategy);
        }
    }

    /// <summary>
    /// Emits a minimal static class that carries only the
    /// <c>AttributeConstraintDefinition</c> for a factory-backed constraint.
    /// The factory lambda creates the storage attr directly, so no constraint wrapper
    /// class is needed.
    /// </summary>
    private static void EmitPrimitiveConstraintDefinition(
        StringBuilder builder,
        AttributeConstraintModel attributeConstraint,
        AttributeConstraintCodeStrategy strategy)
    {
        var className = DialectGeneratorNaming.GetAttributeConstraintClassName(attributeConstraint);
        builder.AppendLine("public static class " + className);
        builder.AppendLine("{");
        builder.AppendLine("    public static AttributeConstraintDefinition AttributeConstraintDefinition { get; } =");
        builder.Append("        new AttributeConstraintDefinition(");
        builder.Append(EmitterHelpers.ToCSharpStringLiteral(attributeConstraint.Name));
        var assemblyFormatExpression = strategy.GetAssemblyFormatConstructionExpression(attributeConstraint.RecordName);
        if (assemblyFormatExpression != null)
        {
            builder.Append(", " + assemblyFormatExpression);
        }
        else
        {
            var assemblyFormatType = strategy.GetAssemblyFormatType(attributeConstraint.RecordName);
            if (assemblyFormatType != null)
            {
                builder.Append(", new " + assemblyFormatType + "()");
            }
        }

        builder.AppendLine(");");
        builder.AppendLine("}");
    }

    private static void EmitDenseCollectionConstraintDefinition(
        StringBuilder builder,
        AttributeConstraintModel attributeConstraint,
        AttributeConstraintCodeStrategy strategy,
        DialectSymbolResolver resolver)
    {
        var className = DialectGeneratorNaming.GetAttributeConstraintClassName(attributeConstraint);

        builder.AppendLine("public static class " + className);
        builder.AppendLine("{");
        builder.AppendLine("    public static AttributeConstraintDefinition AttributeConstraintDefinition { get; } =");
        builder.Append("        new AttributeConstraintDefinition(");
        builder.Append(EmitterHelpers.ToCSharpStringLiteral(attributeConstraint.Name));
        var assemblyFormatExpression = strategy.GetAssemblyFormatConstructionExpression(attributeConstraint.RecordName);
        if (assemblyFormatExpression != null)
        {
            builder.Append(", " + assemblyFormatExpression);
        }
        else
        {
            var assemblyFormatType = strategy.GetAssemblyFormatType(attributeConstraint.RecordName);
            if (assemblyFormatType != null)
            {
                builder.Append(", new " + assemblyFormatType + "()");
            }
        }

        builder.AppendLine(");");
        builder.AppendLine("}");
    }

    private static void EmitEnumConstraint(StringBuilder builder, AttributeConstraintModel attributeConstraint, EnumModel enumModel)
    {
        var className = DialectGeneratorNaming.GetAttributeConstraintClassName(attributeConstraint);
        var enumTypeName = EnumHelpers.GetCSharpEnumTypeName(enumModel);

        // Constraint class
        var assemblyFormatType = className + "AssemblyFormat";
        builder.AppendLine("public sealed class " + className + " : IntegerAttributeValue");
        builder.AppendLine("{");
        builder.AppendLine("    public static AttributeConstraintDefinition AttributeConstraintDefinition { get; } =");
        builder.AppendLine("        new AttributeConstraintDefinition(");
        builder.Append("            " + EmitterHelpers.ToCSharpStringLiteral(attributeConstraint.Name));
        builder.AppendLine(", new " + assemblyFormatType + "());");
        builder.AppendLine();
        builder.AppendLine("    public " + className + "(AttributeValueConstructionContext context)");
        builder.AppendLine("        : base(context, ParseValue(context.Syntax))");
        builder.AppendLine("    {");
        builder.AppendLine("        EnumValue = ParseEnumValue(context.Syntax);");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    public " + className + "(" + enumTypeName + " value)");
        builder.AppendLine("        : base(global::MLIR.Numerics.ApInt.FromUInt64(64, (ulong)value))");
        builder.AppendLine("    {");
        builder.AppendLine("        EnumValue = value;");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    public " + enumTypeName + " EnumValue { get; }");
        builder.AppendLine("    public " + enumTypeName + " TypedValue => EnumValue;");
        builder.AppendLine();
        builder.AppendLine("    public override string? Name => AttributeConstraintDefinition.Name;");
        builder.AppendLine("    public override AttributeConstraintDefinition? Definition => AttributeConstraintDefinition;");
        builder.AppendLine();

        // Enum value parser
        builder.AppendLine("    internal static " + enumTypeName + " ParseEnumValue(MLIR.Syntax.AttributeValueSyntax? syntax)");
        builder.AppendLine("    {");
        builder.AppendLine("        if (syntax == null) return default;");
        builder.AppendLine("        if (syntax is MLIR.Syntax.Attributes.Primitives.IntegerAttributeValueSyntax integerSyntax)");
        builder.AppendLine("        {");
        builder.AppendLine("            return " + EnumEmitter.GetEnumInfoClassName(enumModel) + ".TryFromInteger(integerSyntax.Value, out var integerValue) ? integerValue : default;");
        builder.AppendLine("        }");
        builder.AppendLine("        var raw = syntax.ToString();");
        EnumEmitter.EmitParseExpression(builder, enumModel, enumTypeName, "raw", "        ");
        builder.AppendLine("    }");
        builder.AppendLine();

        // Integer value parser (to feed IntegerAttributeValue base constructor)
        builder.AppendLine("    private static global::MLIR.Numerics.ApInt ParseValue(MLIR.Syntax.AttributeValueSyntax? syntax)");
        builder.AppendLine("    {");
        builder.AppendLine("        return global::MLIR.Numerics.ApInt.FromUInt64(64, global::System.Convert.ToUInt64(ParseEnumValue(syntax)));");
        builder.AppendLine("    }");
        builder.AppendLine();

        // Print helper
        builder.AppendLine("    internal string PrintEnumValue(" + enumTypeName + " value)");
        builder.AppendLine("    {");
        EnumEmitter.EmitFormatExpression(builder, enumModel, "value", "        ");
        builder.AppendLine("    }");
        builder.AppendLine("}");
        builder.AppendLine();

        // Assembly format class for enum constraint
        builder.AppendLine("internal sealed class " + assemblyFormatType + " : IAttributeAssemblyFormat");
        builder.AppendLine("{");
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
        builder.AppendLine("        return new " + className + "(" + className + ".ParseEnumValue(syntax));");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    public AttributeValueSyntax BuildCustomAssemblySyntax(AttributeValue attribute, ConcreteSyntaxBuilderContext context)");
        builder.AppendLine("    {");
        builder.AppendLine("        var enumAttr = (" + className + ")attribute;");
        builder.AppendLine("        var text = enumAttr.PrintEnumValue(enumAttr.EnumValue);");
        builder.AppendLine("        return new MLIR.Syntax.RawAttributeValueSyntax(new MLIR.Syntax.RawSyntaxText(text));");
        builder.AppendLine("    }");
        builder.AppendLine("}");
    }

    private static void EmitTypedArrayConstraint(StringBuilder builder, AttributeConstraintModel attributeConstraint, DialectSymbolResolver resolver)
    {
        var className = DialectGeneratorNaming.GetAttributeConstraintClassName(attributeConstraint);
        var elementRecordName = attributeConstraint.ElementConstraintRecordName;
        var elementStrategy = string.IsNullOrEmpty(elementRecordName)
            ? FallbackAttributeConstraintCodeStrategy.Instance
            : resolver.TryResolveAttributeConstraintStrategy(elementRecordName!);
        var elementTypeName = GetTypedArrayElementTypeName(elementRecordName, elementStrategy, resolver);
        var assemblyFormatType = className + "AssemblyFormat";

        builder.AppendLine("public static class " + className);
        builder.AppendLine("{");
        builder.AppendLine("    public static AttributeConstraintDefinition AttributeConstraintDefinition { get; } =");
        builder.AppendLine("        new AttributeConstraintDefinition(");
        builder.Append("            " + EmitterHelpers.ToCSharpStringLiteral(attributeConstraint.Name));
        builder.AppendLine(", new " + assemblyFormatType + "());");
        builder.AppendLine("}");
        builder.AppendLine();

        builder.AppendLine("internal sealed class " + assemblyFormatType + " : TypedArrayAttributeAssemblyFormat<" + elementTypeName + ">");
        builder.AppendLine("{");
        builder.AppendLine("}");
    }

    private static string GetTypedArrayElementTypeName(string? elementRecordName, AttributeConstraintCodeStrategy elementStrategy, DialectSymbolResolver resolver)
    {
        if (string.IsNullOrEmpty(elementRecordName) || elementStrategy.IsGenericTypedArrayElement)
        {
            return "global::MLIR.Semantics.AttributeValue";
        }

        var nonNullRecordName = elementRecordName!;
        return elementStrategy.GetAttributeValueTypeName(nonNullRecordName, resolver)
            ?? "global::MLIR.Semantics.AttributeValue";
    }

    private static void EmitStandardConstraint(StringBuilder builder, AttributeConstraintModel attributeConstraint, AttributeConstraintCodeStrategy strategy)
    {
        var className = DialectGeneratorNaming.GetAttributeConstraintClassName(attributeConstraint);
        var baseType = strategy.GetBaseType(attributeConstraint.RecordName);
        builder.AppendLine("public sealed class " + className + " : " + baseType);
        builder.AppendLine("{");
        builder.AppendLine("    public static AttributeConstraintDefinition AttributeConstraintDefinition { get; } =");
        builder.Append("        new AttributeConstraintDefinition(");
        builder.Append(EmitterHelpers.ToCSharpStringLiteral(attributeConstraint.Name));
        var assemblyFormatExpression = strategy.GetAssemblyFormatConstructionExpression(attributeConstraint.RecordName);
        if (assemblyFormatExpression != null)
        {
            builder.Append(", " + assemblyFormatExpression);
        }
        else
        {
            var assemblyFormatType = strategy.GetAssemblyFormatType(attributeConstraint.RecordName);
            if (assemblyFormatType != null)
            {
                builder.Append(", new " + assemblyFormatType + "()");
            }
        }

        builder.AppendLine(");");
        builder.AppendLine();
        builder.AppendLine("    public " + className + "(AttributeValueConstructionContext context)");
        var primitiveBaseConstructor = strategy.GetPrimitiveBaseConstructor(attributeConstraint.RecordName);
        if (primitiveBaseConstructor != null)
        {
            builder.AppendLine("        : base(" + primitiveBaseConstructor + ")");
        }
        else
        {
            builder.AppendLine("        : base(context.Syntax, context.Location)");
        }
        builder.AppendLine("    {");
        builder.AppendLine("    }");

        var valueConstructorParam = strategy.GetValueConstructorParameter(attributeConstraint.RecordName);
        if (valueConstructorParam != null)
        {
            builder.AppendLine();
            builder.AppendLine("    public " + className + "(" + valueConstructorParam + " value)");
            builder.AppendLine("        : base(value)");
            builder.AppendLine("    {");
            builder.AppendLine("    }");
        }

        strategy?.EmitInnerHelpers(builder, className);

        builder.AppendLine();
        builder.AppendLine("    public override string? Name => AttributeConstraintDefinition.Name;");
        builder.AppendLine("    public override AttributeConstraintDefinition? Definition => AttributeConstraintDefinition;");
        builder.AppendLine("}");
    }
}
