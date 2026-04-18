namespace MLIR.Generators.Emitters;

using System.Text;
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

        // Typed constructor – optional syntax parameter preserves source provenance when the
        // attribute is produced by the parser; callers can omit it for synthetic values.
        builder.AppendLine("    public " + className + "(" + enumTypeName + " value, MLIR.Syntax.AttributeValueSyntax? syntax = null)");
        builder.AppendLine("        : base(syntax)");
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

        builder.AppendLine("}");
        builder.AppendLine();

        // Assembly format class – thin subclass of the shared runtime base.
        EmitEnumAssemblyFormatClass(builder, className, enumTypeName, enumModel);
    }

    private static void EmitEnumAssemblyFormatClass(StringBuilder builder, string attributeClassName, string enumTypeName, EnumModel enumModel)
    {
        var formatClassName = attributeClassName + "AssemblyFormat";
        var infoClassName = EnumEmitter.GetEnumInfoClassName(enumModel);

        // Choose the runtime base class depending on whether this is a flags enum.
        var baseTypeName = enumModel.IsBitEnum
            ? "global::MLIR.Dialects.Attributes.FlagsEnumAttributeAssemblyFormat<" + attributeClassName + ">"
            : "global::MLIR.Dialects.Attributes.SimpleEnumAttributeAssemblyFormat<" + attributeClassName + ">";

        // Bit enums use Optional angle brackets: the old bespoke parser accepted both
        // `<x,y>` and `x,y`, while printing always emitted `<xy>`. Optional matches that
        // behavior. Non-bit enums use Prohibited (no brackets in inline operation format).
        var angleBracketRequirement = enumModel.IsBitEnum
            ? "global::MLIR.Dialects.Attributes.EnumAngleBracketRequirement.Optional"
            : "global::MLIR.Dialects.Attributes.EnumAngleBracketRequirement.Prohibited";

        builder.AppendLine("internal sealed class " + formatClassName);
        builder.AppendLine("    : " + baseTypeName);
        builder.AppendLine("{");
        builder.AppendLine("    public " + formatClassName + "()");
        builder.AppendLine("        : base(" + infoClassName + ".NamesByInteger) { }");
        builder.AppendLine();
        builder.AppendLine("    public override int BitWidth => " + enumModel.Bitwidth + ";");
        builder.AppendLine("    public override global::MLIR.Dialects.Attributes.EnumAngleBracketRequirement AngleBracketRequirement");
        builder.AppendLine("        => " + angleBracketRequirement + ";");
        builder.AppendLine();

        if (enumModel.IsBitEnum)
        {
            var sepKind = EnumEmitter.GetSeparatorTokenKind(enumModel);
            builder.AppendLine("    public override global::MLIR.Text.TokenKind SeparatorTokenKind");
            builder.AppendLine("        => global::MLIR.Text." + sepKind + ";");
            builder.AppendLine();
        }

        // EnumFromInt – creates the typed attribute from a parsed integer and preserves syntax.
        builder.AppendLine("    public override " + attributeClassName + " EnumFromInt(global::MLIR.Numerics.ApInt value, global::MLIR.Syntax.AttributeValueSyntax syntax)");
        builder.AppendLine("        => new " + attributeClassName + "(" + infoClassName + ".FromInteger(value), syntax);");
        builder.AppendLine();

        // EnumToInt – returns the underlying integer for printing.
        builder.AppendLine("    public override global::MLIR.Numerics.ApInt EnumToInt(" + attributeClassName + " value)");
        builder.AppendLine("        => global::MLIR.Numerics.ApInt.FromUInt64(" + enumModel.Bitwidth + ", (ulong)value.Value);");
        builder.AppendLine("}");
    }
}
