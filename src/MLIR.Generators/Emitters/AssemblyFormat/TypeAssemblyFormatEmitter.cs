namespace MLIR.Generators.Emitters.AssemblyFormat;

using System.Collections.Generic;
using System.Linq;
using System.Text;
using MLIR.Generators.Emitters.Common;
using MLIR.ODS.Model;
using MLIR.Text;

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
        var fields = lowered.Fields;

        builder.AppendLine("public sealed class " + syntaxClassName + " : DialectNamedTypeSyntax");
        builder.AppendLine("{");
        builder.AppendLine();

        builder.Append("    public " + syntaxClassName + "(DialectTypePrefix prefix");
        foreach (var field in fields)
        {
            if (field is LiteralTokenField lit)
            {
                builder.Append(", Token " + lit.LocalName);
            }
            else if (field is VariableSyntaxField v)
            {
                builder.Append(", " + v.SyntaxType + " " + EmitterHelpers.LowerFirst(v.Name) + "Syntax");
            }
        }

        builder.AppendLine(")");
        builder.AppendLine("        : base(prefix)");
        builder.AppendLine("    {");
        foreach (var field in fields)
        {
            if (field is LiteralTokenField lit)
            {
                builder.AppendLine("        " + EmitterHelpers.CapitalizeFirst(lit.LocalName) + " = " + lit.LocalName + ";");
            }
            else if (field is VariableSyntaxField v)
            {
                builder.AppendLine("        " + DialectGeneratorNaming.ToPascalCase(v.Name) + "Syntax = " + EmitterHelpers.LowerFirst(v.Name) + "Syntax;");
            }
        }
        builder.AppendLine("    }");
        builder.AppendLine();

        builder.Append("    public " + syntaxClassName + "(");
        var first = true;
        foreach (var field in fields)
        {
            if (field is VariableSyntaxField v)
            {
                if (!first)
                {
                    builder.Append(", ");
                }

                builder.Append(v.SyntaxType + " " + EmitterHelpers.LowerFirst(v.Name) + "Syntax");
                first = false;
            }
        }

        builder.AppendLine(")");
        builder.Append("        : this(DialectTypePrefix.Synthetic(" + EmitterHelpers.ToCSharpStringLiteral(type.Name) + ")");
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
        builder.AppendLine(") { }");

        if (fields.Any(static field => field is LiteralTokenField))
        {
            builder.AppendLine();
            foreach (var field in fields)
            {
                if (field is LiteralTokenField lit)
                {
                    builder.AppendLine("    public Token " + EmitterHelpers.CapitalizeFirst(lit.LocalName) + " { get; }");
                }
            }
        }

        var variableFields = fields.OfType<VariableSyntaxField>().ToArray();
        if (variableFields.Length > 0)
        {
            builder.AppendLine();
            foreach (var v in variableFields)
            {
                builder.AppendLine("    public " + v.SyntaxType + " " + DialectGeneratorNaming.ToPascalCase(v.Name) + "Syntax { get; }");
            }
        }

        builder.AppendLine();
        if (variableFields.Length > 0)
        {
            builder.AppendLine("    public override SourceLocation Location => SourceLocation.Merge(Prefix.Location, " + DialectGeneratorNaming.ToPascalCase(variableFields[0].Name) + "Syntax.Location);");
        }
        else
        {
            builder.AppendLine("    public override SourceLocation Location => Prefix.Location;");
        }

        builder.AppendLine();
        builder.AppendLine("    public override void WriteTo(Text.SyntaxWriter writer)");
        builder.AppendLine("    {");
        builder.AppendLine("        WritePrefix(writer);");
        EmitWriteToBody(builder, fields);
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    public override SyntaxNode Rewrite(SyntaxRewriter rewriter)");
        builder.AppendLine("    {");
        builder.Append("        return new " + syntaxClassName + "(new DialectTypePrefix(rewriter.VisitToken(Prefix.BangToken), rewriter.VisitToken(Prefix.NameToken))");
        foreach (var field in fields)
        {
            if (field is LiteralTokenField lit)
            {
                builder.Append(", rewriter.VisitToken(" + EmitterHelpers.CapitalizeFirst(lit.LocalName) + ")");
            }
            else if (field is VariableSyntaxField v)
            {
                builder.Append(", " + SyntaxValueShapeEmitter.GetRewriteExpression(v.Name, v.SyntaxType, v.SyntaxShape));
            }
        }

        builder.AppendLine(");");
        builder.AppendLine("    }");
        builder.AppendLine("}");
    }

    public static void EmitAssemblyFormatClass(StringBuilder builder, TypeModel type, string className)
    {
        var format = type.AssemblyFormat!;
        var lowered = AssemblyFormatLowerer.LowerType(type, format);
        var fields = lowered.Fields;
        var syntaxClassName = className + "Syntax";
        var formatClassName = className + "AssemblyFormat";

        builder.AppendLine("internal sealed class " + formatClassName + " : ITypeAssemblyFormat");
        builder.AppendLine("{");
        builder.AppendLine();
        if (!lowered.IsSupported)
        {
            builder.AppendLine("    // This declarative format currently includes unsupported constructs for type/attr lowering.");
            builder.AppendLine("    // The generated format class is still emitted for API completeness, but parsing will fail fast.");
            builder.AppendLine();
        }
        builder.AppendLine("    public ParseResult<TypeSyntax> TryParse(TypeParsingContext context)");
        builder.AppendLine("    {");
        if (!lowered.IsSupported)
        {
            builder.AppendLine("        return ParseResult<TypeSyntax>.Failure(new AssemblyDiagnostic(SourceLocation.Unknown, \"Unsupported declarative assembly format construct for type body.\"));");
        }
        else
        {
            EmitTryParseBody(builder, lowered, syntaxClassName);
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

    private static void EmitTryParseBody(
        StringBuilder builder,
        LoweredAssemblyFormat lowered,
        string syntaxClassName)
    {
        builder.AppendLine("        if (!context.TryMatch(TokenKind.Bang, out var bangToken))");
        builder.AppendLine("            return ParseResult<TypeSyntax>.NoMatch();");
        builder.AppendLine("        if (!context.TryMatch(TokenKind.Identifier, out var nameToken))");
        builder.AppendLine("            return ParseResult<TypeSyntax>.NoMatch();");

        foreach (var element in lowered.Elements)
        {
            foreach (var field in lowered.GetFields(element))
            {
                switch (field)
                {
                    case LiteralTokenField lit:
                        EmitLiteralTokenParse(builder, lit);
                        break;
                    case VariableSyntaxField v:
                        EmitVariableParse(builder, v, syntaxClassName);
                        break;
                }
            }
        }

        builder.Append("        return ParseResult<TypeSyntax>.Success(new " + syntaxClassName + "(new DialectTypePrefix(bangToken, nameToken)");
        foreach (var field in lowered.Fields)
        {
            if (field is LiteralTokenField lit)
            {
                builder.Append(", " + lit.LocalName);
            }
            else if (field is VariableSyntaxField v)
            {
                builder.Append(", " + EmitterHelpers.LowerFirst(v.Name) + "Syntax");
            }
        }

        builder.AppendLine("));");
    }

    private static void EmitLiteralTokenParse(StringBuilder builder, LiteralTokenField lit)
    {
        if (lit.IsKeyword)
        {
            builder.AppendLine("        if (!context.TryMatch(TokenKind.Identifier, out var " + lit.LocalName + ") || " + lit.LocalName + ".Text != " + EmitterHelpers.ToCSharpStringLiteral(lit.SyntheticText) + ")");
            builder.AppendLine("            return ParseResult<TypeSyntax>.NoMatch();");
        }
        else
        {
            builder.AppendLine("        var " + lit.LocalName + "Result = context.Expect(" + lit.KindExpr + ", \"Expected '" + EmitterHelpers.EscapeForStringLiteral(lit.SyntheticText, escapeSingleQuote: true) + "'.\");");
            builder.AppendLine("        if (!" + lit.LocalName + "Result.IsSuccess)");
            builder.AppendLine("            return ParseResult<TypeSyntax>.Failure(" + lit.LocalName + "Result.Diagnostic!);");
            builder.AppendLine("        var " + lit.LocalName + " = " + lit.LocalName + "Result.Value;");
        }
    }

    private static void EmitVariableParse(StringBuilder builder, VariableSyntaxField field, string syntaxClassName)
    {
        var varLocalName = EmitterHelpers.LowerFirst(field.Name) + "Syntax";
        var stopExpr = string.Empty;
        string parseExpr;

        var parserTemplate = field.ParamModel?.CsharpParserTemplate;
        if (parserTemplate is not null)
        {
            parseExpr = parserTemplate.Render("parser", "context");
        }
        else
        {
            parseExpr = "context.TryParseAttributeValueSyntax(" + stopExpr + ")";
        }

        builder.AppendLine("        var " + varLocalName + "Result = " + parseExpr + ";");
        builder.AppendLine("        if (!" + varLocalName + "Result.IsSuccess)");
        builder.AppendLine("            return ParseResult<TypeSyntax>.Failure(" + varLocalName + "Result.Diagnostic!);");
        if (string.Equals(field.SyntaxType, "TypeSyntax", System.StringComparison.Ordinal))
        {
            builder.AppendLine("        var " + varLocalName + " = (TypeSyntax)" + varLocalName + "Result.Value;");
        }
        else
        {
            builder.AppendLine("        var " + varLocalName + " = (" + field.SyntaxType + ")" + varLocalName + "Result.Value;");
        }
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

    private static void EmitWriteToBody(StringBuilder builder, IReadOnlyList<AssemblyFormatSyntaxField> fields)
    {
        foreach (var field in fields)
        {
            switch (field)
            {
                case LiteralTokenField lit:
                    builder.AppendLine("        writer.WriteToken(" + EmitterHelpers.CapitalizeFirst(lit.LocalName) + ");");
                    break;
                case VariableSyntaxField v:
                    builder.AppendLine("        " + DialectGeneratorNaming.ToPascalCase(v.Name) + "Syntax.WriteTo(writer);");
                    break;
            }
        }
    }

}
