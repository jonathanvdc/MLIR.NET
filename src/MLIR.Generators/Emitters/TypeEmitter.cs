namespace MLIR.Generators.Emitters;

using System.Collections.Generic;
using System.Text;
using MLIR.Generators.Emitters.AssemblyFormat;
using MLIR.Generators.Emitters.Common;
using MLIR.ODS.Model;

internal static class TypeEmitter
{
    public static void Emit(StringBuilder builder, TypeModel type)
    {
        var className = DialectGeneratorNaming.GetTypeClassName(type);

        if (TryEmitBuiltinWrapper(builder, type, className))
        {
            return;
        }

        if (type.AssemblyFormat != null && type.Parameters.Count > 0)
        {
            EmitterHelpers.AppendXmlDocComment(builder, type.Summary, type.Description);
            TypeAssemblyFormatEmitter.EmitSyntaxClass(builder, type, className);
            builder.AppendLine();
            EmitParametrisedTypeClass(builder, type, className);
            builder.AppendLine();
            TypeAssemblyFormatEmitter.EmitAssemblyFormatClass(builder, type, className);
        }
        else
        {
            EmitterHelpers.AppendXmlDocComment(builder, type.Summary, type.Description);
            EmitPlainTypeClass(builder, type, className);
        }
    }

    private static bool TryEmitBuiltinWrapper(StringBuilder builder, TypeModel type, string className)
    {
        return type.RecordName switch
        {
            "Builtin_Integer" => EmitIntegerBuiltinWrapper(builder, type, className),
            "Builtin_Index" => EmitIndexBuiltinWrapper(builder, type, className),
            "Builtin_None" => EmitNoneBuiltinWrapper(builder, type, className),
            "Builtin_BFloat16" or "Builtin_Float16" or "Builtin_FloatTF32" or "Builtin_Float32" or "Builtin_Float64" or "Builtin_Float80" or "Builtin_Float128" or
            "Builtin_Float8E5M2" or "Builtin_Float8E4M3" or "Builtin_Float8E4M3FN" or "Builtin_Float8E5M2FNUZ" or "Builtin_Float8E4M3FNUZ" or
            "Builtin_Float8E4M3B11FNUZ" or "Builtin_Float8E3M4" or "Builtin_Float4E2M1FN" or "Builtin_Float6E2M3FN" or "Builtin_Float6E3M2FN" or
            "Builtin_Float8E8M0FNU" => EmitFloatBuiltinWrapper(builder, type, className),
            _ => false,
        };
    }

    private static bool EmitIntegerBuiltinWrapper(StringBuilder builder, TypeModel type, string className)
    {
        EmitterHelpers.AppendXmlDocComment(builder, type.Summary, type.Description);
        builder.AppendLine("public sealed class " + className + " : IntegerTypeReference");
        builder.AppendLine("{");
        builder.AppendLine("    public new static TypeDefinition TypeDefinition { get; } =");
        builder.AppendLine("        new TypeDefinition(" + EmitterHelpers.ToCSharpStringLiteral(type.Name) + ", factory: static context => new " + className + "(context));");
        builder.AppendLine();
        builder.AppendLine("    public " + className + "(TypeReferenceConstructionContext context)");
        builder.AppendLine("        : base((BuiltinIntegerTypeSyntax)context.Syntax!)");
        builder.AppendLine("    {");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    public " + className + "(IntegerTypeSignedness signedness, int width)");
        builder.AppendLine("        : base(signedness, width)");
        builder.AppendLine("    {");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    public override TypeDefinition? Definition => TypeDefinition;");
        builder.AppendLine("}");
        return true;
    }

    private static bool EmitFloatBuiltinWrapper(StringBuilder builder, TypeModel type, string className)
    {
        EmitterHelpers.AppendXmlDocComment(builder, type.Summary, type.Description);
        builder.AppendLine("public sealed class " + className + " : FloatTypeReference");
        builder.AppendLine("{");
        builder.AppendLine("    public new static TypeDefinition TypeDefinition { get; } =");
        builder.AppendLine("        new TypeDefinition(" + EmitterHelpers.ToCSharpStringLiteral(type.Name) + ", factory: static context => new " + className + "(context));");
        builder.AppendLine();
        builder.AppendLine("    public " + className + "(TypeReferenceConstructionContext context)");
        builder.AppendLine("        : base((BuiltinFloatTypeSyntax)context.Syntax!)");
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
        builder.AppendLine("public sealed class " + className + " : IndexTypeReference");
        builder.AppendLine("{");
        builder.AppendLine("    public new static TypeDefinition TypeDefinition { get; } =");
        builder.AppendLine("        new TypeDefinition(" + EmitterHelpers.ToCSharpStringLiteral(type.Name) + ", factory: static context => new " + className + "(context));");
        builder.AppendLine();
        builder.AppendLine("    public " + className + "(TypeReferenceConstructionContext context)");
        builder.AppendLine("        : base((BuiltinIndexTypeSyntax)context.Syntax!)");
        builder.AppendLine("    {");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    public " + className + "()");
        builder.AppendLine("        : base()");
        builder.AppendLine("    {");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    public override TypeDefinition? Definition => TypeDefinition;");
        builder.AppendLine("}");
        return true;
    }

    private static bool EmitNoneBuiltinWrapper(StringBuilder builder, TypeModel type, string className)
    {
        EmitterHelpers.AppendXmlDocComment(builder, type.Summary, type.Description);
        builder.AppendLine("public sealed class " + className + " : NoneTypeReference");
        builder.AppendLine("{");
        builder.AppendLine("    public new static TypeDefinition TypeDefinition { get; } =");
        builder.AppendLine("        new TypeDefinition(" + EmitterHelpers.ToCSharpStringLiteral(type.Name) + ", factory: static context => new " + className + "(context));");
        builder.AppendLine();
        builder.AppendLine("    public " + className + "(TypeReferenceConstructionContext context)");
        builder.AppendLine("        : base((BuiltinNoneTypeSyntax)context.Syntax!)");
        builder.AppendLine("    {");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    public " + className + "()");
        builder.AppendLine("        : base()");
        builder.AppendLine("    {");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    public override TypeDefinition? Definition => TypeDefinition;");
        builder.AppendLine("}");
        return true;
    }

    private static void EmitPlainTypeClass(StringBuilder builder, TypeModel type, string className)
    {
        builder.AppendLine("public sealed class " + className + " : TypeReference");
        builder.AppendLine("{");
        builder.AppendLine("    public static TypeDefinition TypeDefinition { get; } =");
        builder.AppendLine("        new TypeDefinition(" + EmitterHelpers.ToCSharpStringLiteral(type.Name) + ", factory: static context => new " + className + "(context));");
        builder.AppendLine();
        builder.AppendLine("    public " + className + "(TypeReferenceConstructionContext context)");
        builder.AppendLine("        : base(context.Syntax, context.Location)");
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
        var syntaxClassName = className + "Syntax";
        var formatClassName = className + "AssemblyFormat";

        builder.AppendLine("public sealed class " + className + " : TypeReference");
        builder.AppendLine("{");
        builder.AppendLine("    public static TypeDefinition TypeDefinition { get; } =");
        builder.AppendLine("        new TypeDefinition(" + EmitterHelpers.ToCSharpStringLiteral(type.Name) + ", new " + formatClassName + "(), factory: static context => new " + className + "(context));");
        builder.AppendLine();

        builder.AppendLine("    public " + className + "(TypeReferenceConstructionContext context)");
        builder.AppendLine("        : base(context.Syntax, context.Location)");
        builder.AppendLine("    {");
        foreach (var param in parameters)
        {
            var propertyName = DialectGeneratorNaming.ToPascalCase(param.Name);
            builder.AppendLine("        " + propertyName + " = Bind" + propertyName + "Param(context.Syntax);");
        }

        builder.AppendLine("    }");
        builder.AppendLine();

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

        builder.AppendLine(")");
        builder.AppendLine("        : base(null, MLIR.Semantics.SourceLocation.Unknown)");
        builder.AppendLine("    {");
        foreach (var param in parameters)
        {
            var propertyName = DialectGeneratorNaming.ToPascalCase(param.Name);
            builder.AppendLine("        " + propertyName + " = " + EmitterHelpers.LowerFirst(param.Name) + ";");
        }

        builder.AppendLine("    }");
        builder.AppendLine();

        foreach (var param in parameters)
        {
            var csharpType = TypeAssemblyFormatEmitter.GetResolvedCSharpType(param);
            var propertyName = DialectGeneratorNaming.ToPascalCase(param.Name);
            builder.AppendLine("    public " + csharpType + " " + propertyName + " { get; }");
        }

        builder.AppendLine();
        builder.AppendLine("    public override string? Name => TypeDefinition.Name;");
        builder.AppendLine("    public override TypeDefinition? Definition => TypeDefinition;");
        builder.AppendLine();
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
        builder.AppendLine();

        foreach (var param in parameters)
        {
            EmitBindParamHelper(builder, type, param, syntaxClassName);
        }

        builder.AppendLine("}");
    }

    private static void EmitBindParamHelper(
        StringBuilder builder,
        TypeModel type,
        AttrOrTypeParameterModel param,
        string syntaxClassName)
    {
        var csharpType = TypeAssemblyFormatEmitter.GetResolvedCSharpType(param);
        var propertyName = DialectGeneratorNaming.ToPascalCase(param.Name);
        var helperName = "Bind" + propertyName + "Param";

        builder.AppendLine("    private static " + csharpType + " " + helperName + "(MLIR.Syntax.TypeSyntax? syntax)");
        builder.AppendLine("    {");
        builder.AppendLine("        if (syntax is " + syntaxClassName + " structured)");
        builder.AppendLine("        {");
        var accessExpr = "structured." + propertyName + "Syntax";
        var extractExpr = BuildExtractValueExpression(param, accessExpr);
        builder.AppendLine("            return " + extractExpr + ";");
        builder.AppendLine("        }");
        builder.AppendLine();
        var fallbackExpr = BuildFallbackExtractExpression(csharpType, param);
        builder.AppendLine("        return " + fallbackExpr + ";");
        builder.AppendLine("    }");
        builder.AppendLine();
    }

    private static string BuildExtractValueExpression(AttrOrTypeParameterModel param, string syntaxExpr)
    {
        if (!string.IsNullOrEmpty(param.CsharpExtractor))
        {
            return param.CsharpExtractor!.Replace("$_syntax", syntaxExpr);
        }

        return syntaxExpr;
    }

    private static string BuildFallbackExtractExpression(string csharpType, AttrOrTypeParameterModel param)
    {
        if (!string.IsNullOrEmpty(param.CsharpDefault))
        {
            return param.CsharpDefault!;
        }

        return "default!";
    }
}
