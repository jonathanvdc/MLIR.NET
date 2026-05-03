namespace MLIR.Generators.Emitters.AssemblyFormat;

using System.Linq;
using System.Text;
using MLIR.Generators.Emitters.Common;
using MLIR.ODS.Model;

/// <summary>
/// Generates the structured <c>TypeSyntax</c> subclass and the <c>ITypeAssemblyFormat</c>
/// implementation for a <c>TypeDef</c> with a declarative <c>assemblyFormat</c> string.
/// </summary>
internal static class TypeAssemblyFormatEmitter
{
    public static void EmitSyntaxClass(StringBuilder builder, TypeModel type, string className)
    {
        var format = type.AssemblyFormat!;
        var syntaxClassName = className + "Syntax";
        var lowered = AssemblyFormatLowerer.LowerType(type, format);
        new TypeSyntaxClassEmitter(type, syntaxClassName, lowered.Fields).Emit(builder);
    }

    public static void EmitAssemblyFormatClass(StringBuilder builder, TypeModel type, string className)
    {
        var format = type.AssemblyFormat!;
        var lowered = AssemblyFormatLowerer.LowerType(type, format);
        var fields = lowered.Fields;
        var syntaxClassName = className + "Syntax";
        var formatClassName = className + "AssemblyFormat";

        builder.AppendLine("internal sealed class " + formatClassName + " : BodyOnlyTypeAssemblyFormat");
        builder.AppendLine("{");
        builder.AppendLine("    public " + formatClassName + "()");
        builder.AppendLine("        : base(" + EmitterHelpers.ToCSharpStringLiteral(type.Name ?? string.Empty) + ")");
        builder.AppendLine("    {");
        builder.AppendLine("    }");
        builder.AppendLine();
        if (!lowered.IsSupported)
        {
            builder.AppendLine("    // This declarative format currently includes unsupported constructs for type/attr lowering.");
            builder.AppendLine("    // The generated format class is still emitted for API completeness, but parsing will fail fast.");
            builder.AppendLine();
        }
        builder.AppendLine("    protected override ParseResult<TypeSyntax> TryParseBody(TypeParsingContext context, DialectTypePrefix prefix)");
        builder.AppendLine("    {");
        if (!lowered.IsSupported)
        {
            builder.AppendLine("        return ParseResult<TypeSyntax>.Failure(new AssemblyDiagnostic(prefix.Location, \"Unsupported declarative assembly format construct for type body.\"));");
        }
        else
        {
            new TypeTryParseBodyEmitter(lowered, syntaxClassName).Emit(builder);
        }

        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    public static TypeReference BindValue(TypeSyntax syntax)");
        builder.AppendLine("    {");
        EmitBindValueBody(builder, type, fields, className, syntaxClassName);
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    public TypeReference Bind(TypeSyntax syntax, TypeDefinition definition, Binder binder)");
        builder.AppendLine("    {");
        builder.AppendLine("        return BindValue(syntax);");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    public TypeSyntax BuildCustomAssemblySyntax(TypeReference type, ConcreteSyntaxBuilderContext context)");
        builder.AppendLine("    {");
        EmitBuildCustomAssemblySyntaxBody(builder, type, fields, className, syntaxClassName);
        builder.AppendLine("    }");
        builder.AppendLine("}");
    }

    private static void EmitBuildCustomAssemblySyntaxBody(
        StringBuilder builder,
        TypeModel type,
        IReadOnlyList<AssemblyFormatSyntaxField> fields,
        string className,
        string syntaxClassName)
    {
        builder.AppendLine("        var typed = (" + className + ")type;");
        builder.AppendLine("        if (typed.Syntax is " + syntaxClassName + " existingSyntax)");
        builder.AppendLine("            return existingSyntax;");

        foreach (var field in fields.OfType<VariableSyntaxField>())
        {
            var propertyName = DialectGeneratorNaming.ToPascalCase(field.Name);
            var localSyntaxName = EmitterHelpers.LowerFirst(field.Name) + "Syntax";
            var buildExpr = BuildSyntaxFromPropertyExpression("typed." + propertyName, field.ParamModel);
            builder.AppendLine("        var " + localSyntaxName + " = " + buildExpr + ";");
        }

        builder.Append("        return new " + syntaxClassName + "(DialectTypePrefix.Synthetic(" + EmitterHelpers.ToCSharpStringLiteral(type.Name ?? string.Empty) + ")");
        foreach (var field in fields)
        {
            if (field is LiteralTokenField lit)
            {
                builder.Append(", " + (lit.IsKeyword
                    ? "TokenFactory.Identifier(" + EmitterHelpers.ToCSharpStringLiteral(lit.SyntheticText) + ")"
                    : "TokenFactory." + lit.KindExpr.Substring("TokenKind.".Length) + "()"));
            }
            else if (field is VariableSyntaxField v)
            {
                builder.Append(", " + EmitterHelpers.LowerFirst(v.Name) + "Syntax");
            }
        }

        builder.AppendLine(");");
    }

    private static void EmitBindValueBody(
        StringBuilder builder,
        TypeModel type,
        IReadOnlyList<AssemblyFormatSyntaxField> fields,
        string className,
        string syntaxClassName)
    {
        builder.AppendLine("        if (syntax is not " + syntaxClassName + " structured)");
        builder.AppendLine("            throw new global::System.InvalidOperationException(\"Expected the generated type syntax class.\");");

        var constructorArguments = new List<string>();
        foreach (var field in fields.OfType<VariableSyntaxField>())
        {
            var propertyName = DialectGeneratorNaming.ToPascalCase(field.Name);
            var localName = EmitterHelpers.LowerFirst(field.Name) + "Value";
            var syntaxExpr = "structured." + propertyName + "Syntax";
            var valueExpr = BuildValueFromSyntaxExpression(field.ParamModel, syntaxExpr, type.Name, field.Name);
            builder.AppendLine("        var " + localName + " = " + valueExpr + ";");
            constructorArguments.Add(localName);
        }

        builder.AppendLine("        return new " + className + "(" + string.Join(", ", constructorArguments) + ", syntax);");
    }

    private static string BuildSyntaxFromPropertyExpression(string propertyExpr, AttrOrTypeParameterModel? param)
    {
        var printerTemplate = param?.CsharpPrinterTemplate;
        if (printerTemplate is not null)
        {
            return printerTemplate.Render("self", propertyExpr);
        }

        return propertyExpr;
    }

    private static string BuildValueFromSyntaxExpression(
        AttrOrTypeParameterModel? param,
        string syntaxExpr,
        string ownerName,
        string parameterName)
    {
        var extractorTemplate = param?.CsharpExtractorTemplate;
        if (extractorTemplate is not null)
        {
            return extractorTemplate.Render("syntax", syntaxExpr);
        }

        if (!string.IsNullOrEmpty(param?.CsharpDefault))
        {
            return param!.CsharpDefault!;
        }

        var message = "Missing syntax for parameter '" + parameterName + "' on type '" + ownerName + "' and no C# extractor/default was defined.";
        return "throw new global::System.InvalidOperationException(" + EmitterHelpers.ToCSharpStringLiteral(message) + ")";
    }
}
