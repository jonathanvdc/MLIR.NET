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
        var slots = lowered.Slots;

        builder.AppendLine("public sealed class " + syntaxClassName + " : DialectNamedTypeSyntax");
        builder.AppendLine("{");
        builder.AppendLine();

        builder.Append("    public " + syntaxClassName + "(DialectTypePrefix prefix");
        foreach (var slot in slots)
        {
            if (slot is LiteralTokenSlot lit)
            {
                builder.Append(", Token " + lit.LocalName);
            }
            else if (slot is VariableSlot v)
            {
                builder.Append(", " + v.SyntaxType + " " + EmitterHelpers.LowerFirst(v.Name) + "Syntax");
            }
        }

        builder.AppendLine(")");
        builder.AppendLine("        : base(prefix)");
        builder.AppendLine("    {");
        foreach (var slot in slots)
        {
            if (slot is LiteralTokenSlot lit)
            {
                builder.AppendLine("        " + EmitterHelpers.CapitalizeFirst(lit.LocalName) + " = " + lit.LocalName + ";");
            }
            else if (slot is VariableSlot v)
            {
                builder.AppendLine("        " + DialectGeneratorNaming.ToPascalCase(v.Name) + "Syntax = " + EmitterHelpers.LowerFirst(v.Name) + "Syntax;");
            }
        }
        builder.AppendLine("    }");
        builder.AppendLine();

        builder.Append("    public " + syntaxClassName + "(");
        var first = true;
        foreach (var slot in slots)
        {
            if (slot is VariableSlot v)
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
        foreach (var slot in slots)
        {
            if (slot is LiteralTokenSlot lit)
            {
                builder.Append(", " + (lit.IsKeyword
                    ? "TokenFactory.Identifier(" + EmitterHelpers.ToCSharpStringLiteral(lit.SyntheticText) + ")"
                    : "TokenFactory." + lit.KindExpr.Substring("TokenKind.".Length) + "()"));
            }
            else if (slot is VariableSlot v)
            {
                builder.Append(", " + EmitterHelpers.LowerFirst(v.Name) + "Syntax");
            }
        }
        builder.AppendLine(") { }");

        if (slots.Any(static slot => slot is LiteralTokenSlot))
        {
            builder.AppendLine();
            foreach (var slot in slots)
            {
                if (slot is LiteralTokenSlot lit)
                {
                    builder.AppendLine("    public Token " + EmitterHelpers.CapitalizeFirst(lit.LocalName) + " { get; }");
                }
            }
        }

        var variableSlots = slots.OfType<VariableSlot>().ToArray();
        if (variableSlots.Length > 0)
        {
            builder.AppendLine();
            foreach (var v in variableSlots)
            {
                builder.AppendLine("    public " + v.SyntaxType + " " + DialectGeneratorNaming.ToPascalCase(v.Name) + "Syntax { get; }");
            }
        }

        builder.AppendLine();
        if (variableSlots.Length > 0)
        {
            builder.AppendLine("    public override SourceLocation Location => SourceLocation.Merge(Prefix.Location, " + DialectGeneratorNaming.ToPascalCase(variableSlots[0].Name) + "Syntax.Location);");
        }
        else
        {
            builder.AppendLine("    public override SourceLocation Location => Prefix.Location;");
        }

        builder.AppendLine();
        builder.AppendLine("    public override void WriteTo(Text.SyntaxWriter writer)");
        builder.AppendLine("    {");
        builder.AppendLine("        WritePrefix(writer);");
        EmitWriteToBody(builder, slots);
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    public override SyntaxNode Rewrite(SyntaxRewriter rewriter)");
        builder.AppendLine("    {");
        builder.Append("        return new " + syntaxClassName + "(new DialectTypePrefix(rewriter.VisitToken(Prefix.BangToken), rewriter.VisitToken(Prefix.NameToken))");
        foreach (var slot in slots)
        {
            if (slot is LiteralTokenSlot lit)
            {
                builder.Append(", rewriter.VisitToken(" + EmitterHelpers.CapitalizeFirst(lit.LocalName) + ")");
            }
            else if (slot is VariableSlot v)
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
        var slots = lowered.Slots;
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
        EmitBindValueBody(builder, type, slots, className, syntaxClassName);
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    public TypeReference Bind(TypeSyntax syntax, TypeDefinition definition, Binder binder)");
        builder.AppendLine("    {");
        builder.AppendLine("        return BindValue(syntax);");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    public TypeSyntax BuildCustomAssemblySyntax(TypeReference type, ConcreteSyntaxBuilderContext context)");
        builder.AppendLine("    {");
        EmitBuildCustomAssemblySyntaxBody(builder, type, slots, className, syntaxClassName);
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
            foreach (var slot in lowered.GetSlots(element))
            {
                switch (slot)
                {
                    case LiteralTokenSlot lit:
                        EmitLiteralTokenParse(builder, lit);
                        break;
                    case VariableSlot v:
                        EmitVariableParse(builder, v, syntaxClassName);
                        break;
                }
            }
        }

        builder.Append("        return ParseResult<TypeSyntax>.Success(new " + syntaxClassName + "(new DialectTypePrefix(bangToken, nameToken)");
        foreach (var slot in lowered.Slots)
        {
            if (slot is LiteralTokenSlot lit)
            {
                builder.Append(", " + lit.LocalName);
            }
            else if (slot is VariableSlot v)
            {
                builder.Append(", " + EmitterHelpers.LowerFirst(v.Name) + "Syntax");
            }
        }

        builder.AppendLine("));");
    }

    private static void EmitLiteralTokenParse(StringBuilder builder, LiteralTokenSlot lit)
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

    private static void EmitVariableParse(StringBuilder builder, VariableSlot slot, string syntaxClassName)
    {
        var varLocalName = EmitterHelpers.LowerFirst(slot.Name) + "Syntax";
        var stopExpr = string.Empty;
        string parseExpr;

        var parserTemplate = slot.ParamModel?.CsharpParserTemplate;
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
        if (string.Equals(slot.SyntaxType, "TypeSyntax", System.StringComparison.Ordinal))
        {
            builder.AppendLine("        var " + varLocalName + " = (TypeSyntax)" + varLocalName + "Result.Value;");
        }
        else
        {
            builder.AppendLine("        var " + varLocalName + " = (" + slot.SyntaxType + ")" + varLocalName + "Result.Value;");
        }
    }

    private static void EmitBuildCustomAssemblySyntaxBody(
        StringBuilder builder,
        TypeModel type,
        IReadOnlyList<FormatSlot> slots,
        string className,
        string syntaxClassName)
    {
        builder.AppendLine("        var typed = (" + className + ")type;");
        builder.AppendLine("        if (typed.Syntax is " + syntaxClassName + " existingSyntax)");
        builder.AppendLine("            return existingSyntax;");

        foreach (var slot in slots.OfType<VariableSlot>())
        {
            var propertyName = DialectGeneratorNaming.ToPascalCase(slot.Name);
            var localSyntaxName = EmitterHelpers.LowerFirst(slot.Name) + "Syntax";
            var buildExpr = BuildSyntaxFromPropertyExpression("typed." + propertyName, slot.ParamModel);
            builder.AppendLine("        var " + localSyntaxName + " = " + buildExpr + ";");
        }

        builder.Append("        return new " + syntaxClassName + "(DialectTypePrefix.Synthetic(" + EmitterHelpers.ToCSharpStringLiteral(type.Name ?? string.Empty) + ")");
        foreach (var slot in slots)
        {
            if (slot is LiteralTokenSlot lit)
            {
                builder.Append(", " + (lit.IsKeyword
                    ? "TokenFactory.Identifier(" + EmitterHelpers.ToCSharpStringLiteral(lit.SyntheticText) + ")"
                    : "TokenFactory." + lit.KindExpr.Substring("TokenKind.".Length) + "()"));
            }
            else if (slot is VariableSlot v)
            {
                builder.Append(", " + EmitterHelpers.LowerFirst(v.Name) + "Syntax");
            }
        }

        builder.AppendLine(");");
    }

    private static void EmitBindValueBody(
        StringBuilder builder,
        TypeModel type,
        IReadOnlyList<FormatSlot> slots,
        string className,
        string syntaxClassName)
    {
        builder.AppendLine("        if (syntax is not " + syntaxClassName + " structured)");
        builder.AppendLine("            throw new global::System.InvalidOperationException(\"Expected the generated type syntax class.\");");

        var constructorArguments = new List<string>();
        foreach (var slot in slots.OfType<VariableSlot>())
        {
            var propertyName = DialectGeneratorNaming.ToPascalCase(slot.Name);
            var localName = EmitterHelpers.LowerFirst(slot.Name) + "Value";
            var syntaxExpr = "structured." + propertyName + "Syntax";
            var valueExpr = BuildValueFromSyntaxExpression(slot.ParamModel, syntaxExpr, type.Name, slot.Name);
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

    private static void EmitWriteToBody(StringBuilder builder, IReadOnlyList<FormatSlot> slots)
    {
        foreach (var slot in slots)
        {
            switch (slot)
            {
                case LiteralTokenSlot lit:
                    builder.AppendLine("        writer.WriteToken(" + EmitterHelpers.CapitalizeFirst(lit.LocalName) + ");");
                    break;
                case VariableSlot v:
                    builder.AppendLine("        " + DialectGeneratorNaming.ToPascalCase(v.Name) + "Syntax.WriteTo(writer);");
                    break;
            }
        }
    }

}
