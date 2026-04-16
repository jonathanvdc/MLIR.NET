namespace MLIR.Generators.Emitters;

using System.Text;
using MLIR.Generators.Emitters;
using MLIR.Generators.Emitters.AssemblyFormat;
using MLIR.Generators.Emitters.Common;
using MLIR.ODS.Model;

internal static class AttributeEmitter
{
    public static void Emit(StringBuilder builder, AttributeModel attribute)
    {
        var className = DialectGeneratorNaming.GetAttributeClassName(attribute);

        if (attribute.EnumModel != null)
        {
            EmitEnumAttributeClass(builder, attribute, className);
        }
        else if (attribute.AssemblyFormat != null)
        {
            // Parametrised attribute with a declarative assembly format: emit the structured
            // syntax class, the typed attribute-value class, and the assembly format class.
            AttributeAssemblyFormatEmitter.EmitSyntaxClass(builder, attribute, className);
            builder.AppendLine();
            EmitTypedAttributeClass(builder, attribute, className);
            builder.AppendLine();
            AttributeAssemblyFormatEmitter.EmitAssemblyFormatClass(builder, attribute, className);
        }
        else
        {
            // Parametrised attribute without declarative syntax: emit the typed
            // attribute-value class, but do not invent a binding factory.
            EmitTypedAttributeClass(builder, attribute, className);
        }
    }

    /// <summary>
    /// Emits the typed <c>AttributeValue</c> subclass for a parametrised attribute.
    /// Declarative assembly format support is optional: when present, the typed class
    /// binds against the generated structured syntax class; when absent, it binds directly
    /// against the concrete syntax nodes produced by the parser.
    /// </summary>
    private static void EmitTypedAttributeClass(
        StringBuilder builder,
        AttributeModel attribute,
        string className)
    {
        var parameters = attribute.Parameters;
        var hasAssemblyFormat = attribute.AssemblyFormat != null;
        var generatedFormatClassName = hasAssemblyFormat ? className + "AssemblyFormat" : null;
        var assemblyFormatExpression = !string.IsNullOrEmpty(attribute.CsharpAssemblyFormat)
            ? attribute.CsharpAssemblyFormat
            : generatedFormatClassName != null
                ? "new " + generatedFormatClassName + "()"
                : null;

        builder.AppendLine("public sealed class " + className + " : AttributeValue");
        builder.AppendLine("{");

        EmitAttributeDefinition(builder, attribute, className, parameters, assemblyFormatExpression);
        builder.AppendLine();

        EmitTypedAttributeConstructor(builder, className, parameters);
        builder.AppendLine();

        EmitParameterProperties(builder, parameters);

        builder.AppendLine();
        builder.AppendLine("    public override string? Name => AttributeDefinition.Name;");
        builder.AppendLine("    public override AttributeConstraintDefinition? Definition => AttributeDefinition;");
        builder.AppendLine();

        builder.AppendLine("}");
    }

    private static void EmitAttributeDefinition(
        StringBuilder builder,
        AttributeModel attribute,
        string className,
        IReadOnlyList<AttrOrTypeParameterModel> parameters,
        string? assemblyFormatExpression)
    {
        builder.AppendLine("    public static AttributeDefinition AttributeDefinition { get; } =");
        EmitterHelpers.AppendDefinitionConstructor(
            builder,
            "AttributeDefinition",
            attribute.Name,
            assemblyFormatExpression);
    }

    private static void EmitTypedAttributeConstructor(
        StringBuilder builder,
        string className,
        IReadOnlyList<AttrOrTypeParameterModel> parameters)
    {
        // The optional syntax node preserves source provenance when the attribute is parsed
        // from text, but callers can omit it when constructing synthetic values directly.
        builder.Append("    public " + className + "(");
        for (var i = 0; i < parameters.Count; i++)
        {
            if (i > 0)
            {
                builder.Append(", ");
            }

            var csharpType = AttributeAssemblyFormatEmitter.GetResolvedCSharpType(parameters[i]);
            builder.Append(csharpType + " " + EmitterHelpers.LowerFirst(parameters[i].Name));
        }

        if (parameters.Count > 0)
        {
            builder.Append(", ");
        }

        builder.AppendLine("MLIR.Syntax.AttributeValueSyntax? syntax = null)");
        builder.AppendLine("        : base(syntax)");
        builder.AppendLine("    {");
        foreach (var param in parameters)
        {
            var propertyName = DialectGeneratorNaming.ToPascalCase(param.Name);
            builder.AppendLine("        " + propertyName + " = " + EmitterHelpers.LowerFirst(param.Name) + ";");
        }

        builder.AppendLine("    }");
    }

    private static void EmitParameterProperties(StringBuilder builder, IReadOnlyList<AttrOrTypeParameterModel> parameters)
    {
        foreach (var param in parameters)
        {
            var csharpType = AttributeAssemblyFormatEmitter.GetResolvedCSharpType(param);
            var propertyName = DialectGeneratorNaming.ToPascalCase(param.Name);
            builder.AppendLine("    public " + csharpType + " " + propertyName + " { get; }");
        }
    }

    private static void EmitEnumAttributeClass(StringBuilder builder, AttributeModel attribute, string className)
    {
        var enumModel = attribute.EnumModel!;
        var enumTypeName = EnumHelpers.GetCSharpEnumTypeName(enumModel);

        builder.AppendLine("public sealed class " + className + " : AttributeValue");
        builder.AppendLine("{");

        // AttributeDefinition
        builder.AppendLine("    public static AttributeDefinition AttributeDefinition { get; } =");
        EmitterHelpers.AppendDefinitionConstructor(
            builder,
            "AttributeDefinition",
            attribute.Name,
            "new " + className + "AssemblyFormat()");
        builder.AppendLine();

        // Typed constructor
        builder.AppendLine("    public " + className + "(" + enumTypeName + " value)");
        builder.AppendLine("        : base(null)");
        builder.AppendLine("    {");
        builder.AppendLine("        Value = value;");
        builder.AppendLine("    }");
        builder.AppendLine();

        // Value property
        builder.AppendLine("    public " + enumTypeName + " Value { get; }");
        builder.AppendLine("    public " + enumTypeName + " TypedValue => Value;");
        builder.AppendLine();

        // Name and Definition properties
        builder.AppendLine("    public override string? Name => AttributeDefinition.Name;");
        builder.AppendLine("    public override AttributeConstraintDefinition? Definition => AttributeDefinition;");
        builder.AppendLine();

        // ParseEnumValue helper
        EnumEmitter.EmitParseEnumValueHelperMethod(
            builder,
            enumModel,
            enumTypeName,
            "    ",
            "public static",
            includeIntegerLiteralSyntaxFallback: false,
            allowBitEnumAngleBrackets: enumModel.IsBitEnum);

        // PrintEnumValue helper
        EnumEmitter.EmitPrintEnumValueHelperMethod(
            builder,
            enumModel,
            enumTypeName,
            "    ",
            "internal");

        builder.AppendLine("}");
        builder.AppendLine();

        // Assembly format class
        EmitEnumAssemblyFormatClass(builder, className, enumTypeName, enumModel);
    }

    private static void EmitEnumAssemblyFormatClass(StringBuilder builder, string attributeClassName, string enumTypeName, EnumModel enumModel)
    {
        var formatClassName = attributeClassName + "AssemblyFormat";
        builder.AppendLine("internal sealed class " + formatClassName + " : IAttributeAssemblyFormat");
        builder.AppendLine("{");
        EnumEmitter.EmitAssemblyFormatTryParseMethod(
            builder,
            enumModel,
            "    ",
            allowBitEnumAngleBrackets: enumModel.IsBitEnum);
        builder.AppendLine("    public static AttributeValue BindValue(AttributeValueSyntax syntax)");
        builder.AppendLine("    {");
        builder.AppendLine("        return new " + attributeClassName + "(" + attributeClassName + ".ParseEnumValue(syntax));");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    public AttributeValue Bind(AttributeValueSyntax syntax, AttributeConstraintDefinition definition, Binder binder)");
        builder.AppendLine("    {");
        builder.AppendLine("        return BindValue(syntax);");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    public AttributeValueSyntax BuildCustomAssemblySyntax(AttributeValue attribute, ConcreteSyntaxBuilderContext context)");
        builder.AppendLine("    {");
        builder.AppendLine("        var enumAttr = (" + attributeClassName + ")attribute;");
        builder.AppendLine("        var text = enumAttr.PrintEnumValue(enumAttr.Value);");
        if (enumModel.IsBitEnum)
        {
            builder.AppendLine("        text = \"<\" + text + \">\";");
        }

        builder.AppendLine("        return new MLIR.Syntax.RawAttributeValueSyntax(new MLIR.Syntax.RawSyntaxText(text));");
        builder.AppendLine("    }");
        builder.AppendLine("}");
    }
}
