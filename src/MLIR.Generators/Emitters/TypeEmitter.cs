namespace MLIR.Generators.Emitters;

using System.Text;
using MLIR.Generators.Emitters.AssemblyFormat;
using MLIR.Generators.Emitters.Common;
using MLIR.ODS.Model;

internal static class TypeEmitter
{
    public static void Emit(StringBuilder builder, TypeModel type)
    {
        var className = DialectGeneratorNaming.GetTypeClassName(type);

        if (type.Parameters.Count > 0)
        {
            if (type.AssemblyFormat != null)
            {
                EmitterHelpers.AppendXmlDocComment(builder, type.Summary, type.Description);
                TypeAssemblyFormatEmitter.EmitSyntaxClass(builder, type, className);
                builder.AppendLine();
                builder.AppendLine();
                EmitParametrisedTypeClass(builder, type, className);
                builder.AppendLine();
                TypeAssemblyFormatEmitter.EmitAssemblyFormatClass(builder, type, className);
                return;
            }

            if (!string.IsNullOrEmpty(type.CsharpName))
            {
                EmitterHelpers.AppendXmlDocComment(builder, type.Summary, type.Description);
                EmitParametrisedTypeClass(builder, type, className);
                return;
            }

            EmitterHelpers.AppendXmlDocComment(builder, type.Summary, type.Description);
            EmitPlainTypeClass(builder, type, className);
            return;
        }

        if (TryEmitBuiltinWrapper(builder, type, className))
        {
            return;
        }

        EmitterHelpers.AppendXmlDocComment(builder, type.Summary, type.Description);
        EmitPlainTypeClass(builder, type, className);
    }

    private static bool TryEmitBuiltinWrapper(StringBuilder builder, TypeModel type, string className)
    {
        return type.RecordName switch
        {
            "Builtin_Index" => EmitIndexBuiltinWrapper(builder, type, className),
            "Builtin_None" => EmitNoneBuiltinWrapper(builder, type, className),
            "Builtin_BFloat16" or "Builtin_Float16" or "Builtin_FloatTF32" or "Builtin_Float32" or "Builtin_Float64" or "Builtin_Float80" or "Builtin_Float128" or
            "Builtin_Float8E5M2" or "Builtin_Float8E4M3" or "Builtin_Float8E4M3FN" or "Builtin_Float8E5M2FNUZ" or "Builtin_Float8E4M3FNUZ" or
            "Builtin_Float8E4M3B11FNUZ" or "Builtin_Float8E3M4" or "Builtin_Float4E2M1FN" or "Builtin_Float6E2M3FN" or "Builtin_Float6E3M2FN" or
            "Builtin_Float8E8M0FNU" => EmitFloatBuiltinWrapper(builder, type, className),
            _ => false,
        };
    }

    private static bool EmitFloatBuiltinWrapper(StringBuilder builder, TypeModel type, string className)
    {
        EmitterHelpers.AppendXmlDocComment(builder, type.Summary, type.Description);
        builder.AppendLine("public sealed partial class " + className + " : FloatTypeReference");
        builder.AppendLine("{");
        builder.AppendLine("    public static TypeDefinition TypeDefinition { get; } =");

        var assemblyFormatExpression = type.CsharpAssemblyFormat;
        EmitterHelpers.AppendDefinitionConstructor(
            builder,
            "TypeDefinition",
            type.Name,
            assemblyFormatExpression);

        // Derive the scalar mnemonic from the canonical type name (e.g., "builtin.f32" -> "f32").
        // This ensures FloatTypeReference.Name carries the MLIR spelling, not the qualified registry key.
        var mnemonic = type.Name.StartsWith("builtin.", StringComparison.Ordinal)
            ? type.Name.Substring("builtin.".Length)
            : type.Name;

        builder.AppendLine();
        builder.AppendLine("    public " + className + "(BuiltinFloatTypeSyntax? syntax = null)");
        builder.AppendLine("        : base(" + EmitterHelpers.ToCSharpStringLiteral(mnemonic) + ", syntax, syntax?.Location ?? MLIR.Semantics.SourceLocation.Unknown)");
        builder.AppendLine("    {");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    public " + className + "(string name)");
        builder.AppendLine("        : base(name)");
        builder.AppendLine("    {");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    public override TypeDefinition? Definition => TypeDefinition;");
        builder.AppendLine("}");
        return true;
    }

    private static bool EmitIndexBuiltinWrapper(StringBuilder builder, TypeModel type, string className)
    {
        EmitterHelpers.AppendXmlDocComment(builder, type.Summary, type.Description);
        builder.AppendLine("public partial class " + className + " : TypeReference");
        builder.AppendLine("{");
        builder.AppendLine("    public static TypeDefinition TypeDefinition { get; } =");

        var assemblyFormatExpression = type.CsharpAssemblyFormat;
        EmitterHelpers.AppendDefinitionConstructor(
            builder,
            "TypeDefinition",
            type.Name,
            assemblyFormatExpression);

        builder.AppendLine();
        builder.AppendLine("    public " + className + "(TypeSyntax? syntax = null)");
        builder.AppendLine("        : base(syntax, syntax?.Location ?? MLIR.Semantics.SourceLocation.Unknown)");
        builder.AppendLine("    {");
        builder.AppendLine("    }");
        builder.AppendLine("}");
        return true;
    }

    private static bool EmitNoneBuiltinWrapper(StringBuilder builder, TypeModel type, string className)
    {
        EmitterHelpers.AppendXmlDocComment(builder, type.Summary, type.Description);
        builder.AppendLine("public partial class " + className + " : TypeReference");
        builder.AppendLine("{");
        builder.AppendLine("    public static TypeDefinition TypeDefinition { get; } =");

        var assemblyFormatExpression = type.CsharpAssemblyFormat;
        EmitterHelpers.AppendDefinitionConstructor(
            builder,
            "TypeDefinition",
            type.Name,
            assemblyFormatExpression);

        builder.AppendLine();
        builder.AppendLine("    public " + className + "(TypeSyntax? syntax = null)");
        builder.AppendLine("        : base(syntax, syntax?.Location ?? MLIR.Semantics.SourceLocation.Unknown)");
        builder.AppendLine("    {");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    public override string? Name => TypeDefinition.Name;");
        builder.AppendLine("    public override TypeDefinition? Definition => TypeDefinition;");
        builder.AppendLine("}");
        return true;
    }

    private static void EmitPlainTypeClass(StringBuilder builder, TypeModel type, string className)
    {
        builder.AppendLine("public sealed partial class " + className + " : TypeReference");
        builder.AppendLine("{");
        builder.AppendLine("    public static TypeDefinition TypeDefinition { get; } =");
        EmitterHelpers.AppendDefinitionConstructor(
            builder,
            "TypeDefinition",
            type.Name);
        builder.AppendLine();
        builder.AppendLine("    public " + className + "(TypeSyntax? syntax = null)");
        builder.AppendLine("        : base(syntax, syntax?.Location ?? MLIR.Semantics.SourceLocation.Unknown)");
        builder.AppendLine("    {");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    public override string? Name => TypeDefinition.Name;");
        builder.AppendLine("    public override TypeDefinition? Definition => TypeDefinition;");
        builder.AppendLine("}");
    }

    private static void EmitParametrisedTypeClass(StringBuilder builder, TypeModel type, string className)
    {
        var parameters = type.Parameters;
        var hasAssemblyFormat = type.AssemblyFormat != null;
        var formatClassName = hasAssemblyFormat ? className + "AssemblyFormat" : null;
        var assemblyFormatExpression = !string.IsNullOrEmpty(type.CsharpAssemblyFormat)
            ? type.CsharpAssemblyFormat
            : formatClassName != null
                ? "new " + formatClassName + "()"
                : null;

        builder.AppendLine("public partial class " + className + " : TypeReference");
        builder.AppendLine("{");
        EmitTypeDefinition(builder, type, assemblyFormatExpression);
        builder.AppendLine();

        EmitTypeConstructor(builder, className, parameters);
        builder.AppendLine();

        EmitTypeParameterProperties(builder, parameters);

        builder.AppendLine();
        builder.AppendLine("    public override string? Name => " + (string.IsNullOrEmpty(type.CsharpName) ? "TypeDefinition.Name" : type.CsharpName) + ";");
        builder.AppendLine();
        builder.AppendLine("    public override TypeDefinition? Definition => TypeDefinition;");
        builder.AppendLine();

        EmitTypeSemanticEquality(builder, className, parameters);
        builder.AppendLine();

        builder.AppendLine("}");
    }

    private static void EmitTypeDefinition(
        StringBuilder builder,
        TypeModel type,
        string? assemblyFormatExpression)
    {
        builder.AppendLine("    public static TypeDefinition TypeDefinition { get; } =");
        EmitterHelpers.AppendDefinitionConstructor(builder, "TypeDefinition", type.Name, assemblyFormatExpression);
    }

    private static void EmitTypeConstructor(StringBuilder builder, string className, IReadOnlyList<AttrOrTypeParameterModel> parameters)
    {
        builder.Append("    public " + className + "(");
        for (var i = 0; i < parameters.Count; i++)
        {
            if (i > 0)
            {
                builder.Append(", ");
            }

            var csharpType = TypeAssemblyFormatEmitter.GetResolvedCSharpType(parameters[i]);
            builder.Append(csharpType + " " + EmitterHelpers.LowerFirst(parameters[i].Name));
        }

        if (parameters.Count > 0)
        {
            builder.Append(", ");
        }

        builder.AppendLine("TypeSyntax? syntax = null)");
        builder.AppendLine("        : base(syntax, syntax?.Location ?? MLIR.Semantics.SourceLocation.Unknown)");
        builder.AppendLine("    {");
        foreach (var param in parameters)
        {
            var propertyName = DialectGeneratorNaming.ToPascalCase(param.Name);
            builder.AppendLine("        " + propertyName + " = " + EmitterHelpers.LowerFirst(param.Name) + ";");
        }

        builder.AppendLine("    }");
    }

    private static void EmitTypeParameterProperties(StringBuilder builder, IReadOnlyList<AttrOrTypeParameterModel> parameters)
    {
        foreach (var param in parameters)
        {
            var csharpType = TypeAssemblyFormatEmitter.GetResolvedCSharpType(param);
            var propertyName = DialectGeneratorNaming.ToPascalCase(param.Name);
            builder.AppendLine("    public " + csharpType + " " + propertyName + " { get; }");
        }
    }

    private static void EmitTypeSemanticEquality(StringBuilder builder, string className, IReadOnlyList<AttrOrTypeParameterModel> parameters)
    {
        builder.AppendLine("    protected override Type SemanticFamily => typeof(" + className + ");");
        builder.AppendLine();
        builder.AppendLine("    protected override bool SemanticEqualsValue(TypeReference other)");
        builder.AppendLine("    {");
        builder.AppendLine("        var typedOther = (" + className + ")other;");
        builder.Append("        return ");
        for (var i = 0; i < parameters.Count; i++)
        {
            if (i > 0)
            {
                builder.AppendLine(" &&");
                builder.Append("               ");
            }

            var propertyName = DialectGeneratorNaming.ToPascalCase(parameters[i].Name);
            var csharpType = TypeAssemblyFormatEmitter.GetResolvedCSharpType(parameters[i]);
            builder.Append("global::System.Collections.Generic.EqualityComparer<" + csharpType + ">.Default.Equals(" + propertyName + ", typedOther." + propertyName + ")");
        }

        if (parameters.Count == 0)
        {
            builder.Append("true");
        }

        builder.AppendLine(";");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    protected override int GetSemanticHashCodeValue()");
        builder.AppendLine("    {");
        builder.AppendLine("        unchecked");
        builder.AppendLine("        {");
        builder.AppendLine("            var hash = 17;");
        foreach (var param in parameters)
        {
            var propertyName = DialectGeneratorNaming.ToPascalCase(param.Name);
            builder.AppendLine("            hash = (hash * 31) + global::System.Collections.Generic.EqualityComparer<" + TypeAssemblyFormatEmitter.GetResolvedCSharpType(param) + ">.Default.GetHashCode(" + propertyName + ");");
        }

        builder.AppendLine("            return hash;");
        builder.AppendLine("        }");
        builder.AppendLine("    }");
    }

}
