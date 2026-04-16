namespace MLIR.Generators.Emitters.AssemblyFormat;

using System.Collections.Generic;
using System.Linq;
using System.Text;
using MLIR.Generators.Emitters.Common;
using MLIR.ODS.Model;
using MLIR.ODS.Model.AssemblyFormat;
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
        var slots = BuildFormatSlots(type, format);

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
                builder.Append(", " + GetVariableRewriteExpression(v));
            }
        }

        builder.AppendLine(");");
        builder.AppendLine("    }");
        builder.AppendLine("}");
    }

    public static void EmitAssemblyFormatClass(StringBuilder builder, TypeModel type, string className)
    {
        var format = type.AssemblyFormat!;
        var slots = BuildFormatSlots(type, format);
        var syntaxClassName = className + "Syntax";
        var formatClassName = className + "AssemblyFormat";

        builder.AppendLine("internal sealed class " + formatClassName + " : ITypeAssemblyFormat");
        builder.AppendLine("{");
        builder.AppendLine();
        builder.AppendLine("    public ParseResult<TypeSyntax> TryParse(TypeParsingContext context)");
        builder.AppendLine("    {");
        EmitTryParseBody(builder, type, format, slots, syntaxClassName);
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
        TypeModel type,
        AssemblyFormatModel format,
        IReadOnlyList<FormatSlot> slots,
        string syntaxClassName)
    {
        builder.AppendLine("        if (!context.TryMatch(TokenKind.Bang, out var bangToken))");
        builder.AppendLine("            return ParseResult<TypeSyntax>.NoMatch();");
        builder.AppendLine("        if (!context.TryMatch(TokenKind.Identifier, out var nameToken))");
        builder.AppendLine("            return ParseResult<TypeSyntax>.NoMatch();");
        foreach (var slot in slots)
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

        builder.Append("        return ParseResult<TypeSyntax>.Success(new " + syntaxClassName + "(new DialectTypePrefix(bangToken, nameToken)");
        foreach (var slot in slots)
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

    private static string GetVariableRewriteExpression(VariableSlot slot)
    {
        var propertyExpr = EmitterHelpers.CapitalizeFirst(slot.Name) + "Syntax";
        var syntaxType = slot.SyntaxType;

        if (string.Equals(syntaxType, "Token", System.StringComparison.Ordinal) ||
            string.Equals(syntaxType, "Token?", System.StringComparison.Ordinal))
        {
            return "rewriter.VisitToken(" + propertyExpr + ")";
        }

        if (string.Equals(syntaxType, "RawSyntaxText", System.StringComparison.Ordinal))
        {
            return "rewriter.VisitRawText(" + propertyExpr + ")";
        }

        if (syntaxType.EndsWith("?", System.StringComparison.Ordinal))
        {
            var innerType = syntaxType.Substring(0, syntaxType.Length - 1);
            if (innerType.EndsWith("Syntax", System.StringComparison.Ordinal))
            {
                return propertyExpr + " != null ? (" + innerType + ")rewriter.Visit(" + propertyExpr + ") : null";
            }
        }

        if (syntaxType.StartsWith("DelimitedSyntaxList<", System.StringComparison.Ordinal))
        {
            return syntaxType.Contains("Token", System.StringComparison.Ordinal)
                ? "rewriter.VisitDelimitedTokenList(" + propertyExpr + ")"
                : "rewriter.VisitDelimitedList(" + propertyExpr + ")";
        }

        if (syntaxType.StartsWith("SeparatedSyntaxList<", System.StringComparison.Ordinal))
        {
            return syntaxType.Contains("Token", System.StringComparison.Ordinal)
                ? "rewriter.VisitSeparatedTokenList(" + propertyExpr + ")"
                : "rewriter.VisitSeparatedList(" + propertyExpr + ")";
        }

        if (syntaxType.StartsWith("IReadOnlyList<", System.StringComparison.Ordinal))
        {
            if (syntaxType.Contains("Token", System.StringComparison.Ordinal))
            {
                return "rewriter.VisitTokenList(" + propertyExpr + ")";
            }

            if (syntaxType.Contains("RawSyntaxText", System.StringComparison.Ordinal))
            {
                return "rewriter.VisitRawTextList(" + propertyExpr + ")";
            }

            return "rewriter.VisitList(" + propertyExpr + ")";
        }

        if (syntaxType.EndsWith("Syntax", System.StringComparison.Ordinal))
        {
            return "(" + syntaxType + ")rewriter.Visit(" + propertyExpr + ")";
        }

        return propertyExpr;
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

    private abstract class FormatSlot { }

    private sealed class LiteralTokenSlot : FormatSlot
    {
        public string LocalName { get; set; } = string.Empty;
        public string SyntheticText { get; set; } = string.Empty;
        public string KindExpr { get; set; } = string.Empty;
        public bool IsKeyword { get; set; }
    }

    private sealed class VariableSlot : FormatSlot
    {
        public string Name { get; set; } = string.Empty;
        public string SyntaxType { get; set; } = "AttributeValueSyntax";
        public AttrOrTypeParameterModel? ParamModel { get; set; }
    }

    private static IReadOnlyList<FormatSlot> BuildFormatSlots(TypeModel type, AssemblyFormatModel format)
    {
        var slots = new List<FormatSlot>();
        var literalIndex = 0;

        AssemblyFormatTraversal.VisitElements(
            format.Elements,
            onLiteral: literal =>
            {
                foreach (var lit in literal.Value)
                {
                    switch (lit)
                    {
                        case PunctuationLiteral punc:
                            slots.Add(new LiteralTokenSlot
                            {
                                LocalName = "literal" + literalIndex + "Token",
                                SyntheticText = EmitterHelpers.GetPunctuationText(punc.TokenKind),
                                KindExpr = "TokenKind." + punc.TokenKind,
                            });
                            literalIndex++;
                            break;

                        case KeywordLiteral kw:
                            slots.Add(new LiteralTokenSlot
                            {
                                LocalName = "literal" + literalIndex + "Token",
                                SyntheticText = kw.Spelling,
                                KindExpr = "TokenKind.Identifier",
                                IsKeyword = true,
                            });
                            literalIndex++;
                            break;
                    }
                }
            },
            onVariable: variable =>
            {
                var paramModel = FindParameter(type, variable.Name);
                slots.Add(new VariableSlot
                {
                    Name = variable.Name,
                    SyntaxType = GetResolvedCSharpSyntaxType(paramModel),
                    ParamModel = paramModel,
                });
            });

        return slots;
    }

    internal static AttrOrTypeParameterModel? FindParameter(TypeModel type, string variableName)
    {
        foreach (var param in type.Parameters)
        {
            if (string.Equals(param.Name, variableName, System.StringComparison.Ordinal))
            {
                return param;
            }
        }

        return null;
    }

    internal static string GetResolvedCSharpType(AttrOrTypeParameterModel? param)
    {
        if (param == null)
        {
            return "AttributeValueSyntax";
        }

        if (!string.IsNullOrEmpty(param.CsharpType))
        {
            return param.CsharpType!;
        }

        return "AttributeValueSyntax";
    }

    private static string GetResolvedCSharpSyntaxType(AttrOrTypeParameterModel? param)
    {
        if (!string.IsNullOrEmpty(param?.CsharpSyntaxType))
        {
            return param!.CsharpSyntaxType!;
        }

        return "AttributeValueSyntax";
    }
}
