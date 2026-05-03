namespace MLIR.Generators.Emitters.AssemblyFormat;

using System.Collections.Generic;
using System.Linq;
using System.Text;
using MLIR.Generators.Emitters.Common;
using MLIR.ODS.Model.AssemblyFormat;
using MLIR.Text;

internal abstract class AttrOrTypeTryParseBodyEmitter
{
    private readonly LoweredAssemblyFormat lowered;
    private readonly string syntaxClassName;

    protected AttrOrTypeTryParseBodyEmitter(LoweredAssemblyFormat lowered, string syntaxClassName)
    {
        this.lowered = lowered;
        this.syntaxClassName = syntaxClassName;
    }

    protected abstract string ResultSyntaxType { get; }

    protected virtual bool FirstLiteralCanNoMatch => true;

    protected string ParseResultType => "ParseResult<" + ResultSyntaxType + ">";

    public void Emit(StringBuilder builder)
    {
        var elements = lowered.Elements.Select(static element => element.Source).ToArray();
        var isFirst = true;

        foreach (var element in lowered.Elements)
        {
            foreach (var field in lowered.GetFields(element))
            {
                switch (field)
                {
                    case LiteralTokenField lit:
                        EmitLiteralTokenParse(builder, lit, ref isFirst);
                        break;

                    case VariableSyntaxField variable:
                    {
                        var stopTokens = AssemblyFormatTraversal.FindStopTokensForVariable(elements, element.ElementIndex);
                        EmitVariableParse(builder, variable, stopTokens);
                        isFirst = false;
                        break;
                    }
                }
            }
        }

        builder.Append("        return " + ParseResultType + ".Success(new " + syntaxClassName + "(prefix");
        foreach (var field in lowered.Fields)
        {
            if (field is LiteralTokenField lit)
            {
                builder.Append(", " + lit.LocalName);
            }
            else if (field is VariableSyntaxField variable)
            {
                builder.Append(", " + GetVariableSyntaxLocalName(variable));
            }
        }

        builder.AppendLine("));");
    }

    private void EmitLiteralTokenParse(StringBuilder builder, LiteralTokenField lit, ref bool isFirst)
    {
        if (lit.IsKeyword)
        {
            EmitKeywordLiteralParse(builder, lit, ref isFirst);
        }
        else
        {
            EmitPunctuationLiteralParse(builder, lit, ref isFirst);
        }
    }

    private void EmitKeywordLiteralParse(StringBuilder builder, LiteralTokenField lit, ref bool isFirst)
    {
        if (isFirst && FirstLiteralCanNoMatch)
        {
            builder.AppendLine("        if (!context.TryMatch(TokenKind.Identifier, out var " + lit.LocalName + ") || " + lit.LocalName + ".Text != " + EmitterHelpers.ToCSharpStringLiteral(lit.SyntheticText) + ")");
            builder.AppendLine("        {");
            builder.AppendLine("            return " + ParseResultType + ".NoMatch();");
            builder.AppendLine("        }");
            isFirst = false;
            return;
        }

        builder.AppendLine("        var " + lit.LocalName + "Result = context.Expect(TokenKind.Identifier, \"Expected keyword '" + EmitterHelpers.EscapeForStringLiteral(lit.SyntheticText, escapeSingleQuote: true) + "'.\");");
        builder.AppendLine("        if (!" + lit.LocalName + "Result.IsSuccess)");
        builder.AppendLine("            return " + ParseResultType + ".Failure(" + lit.LocalName + "Result.Diagnostic!);");
        builder.AppendLine("        var " + lit.LocalName + " = " + lit.LocalName + "Result.Value;");
    }

    private void EmitPunctuationLiteralParse(StringBuilder builder, LiteralTokenField lit, ref bool isFirst)
    {
        if (isFirst && FirstLiteralCanNoMatch)
        {
            builder.AppendLine("        if (!context.TryMatch(" + lit.KindExpr + ", out var " + lit.LocalName + "))");
            builder.AppendLine("        {");
            builder.AppendLine("            return " + ParseResultType + ".NoMatch();");
            builder.AppendLine("        }");
            isFirst = false;
            return;
        }

        builder.AppendLine("        var " + lit.LocalName + "Result = context.Expect(" + lit.KindExpr + ", \"Expected '" + EmitterHelpers.EscapeForStringLiteral(lit.SyntheticText, escapeSingleQuote: true) + "'.\");");
        builder.AppendLine("        if (!" + lit.LocalName + "Result.IsSuccess)");
        builder.AppendLine("            return " + ParseResultType + ".Failure(" + lit.LocalName + "Result.Diagnostic!);");
        builder.AppendLine("        var " + lit.LocalName + " = " + lit.LocalName + "Result.Value;");
    }

    private void EmitVariableParse(
        StringBuilder builder,
        VariableSyntaxField field,
        IReadOnlyList<TokenKind> stopTokens)
    {
        var varLocalName = GetVariableSyntaxLocalName(field);
        var parserTemplate = field.ParamModel?.CsharpParserTemplate;
        var parseExpr = parserTemplate is not null
            ? parserTemplate.Render("parser", "context")
            : "context.TryParseAttributeValueSyntax(" + BuildStopTokensExpression(stopTokens) + ")";

        builder.AppendLine("        var " + varLocalName + "Result = " + parseExpr + ";");
        builder.AppendLine("        if (!" + varLocalName + "Result.IsSuccess)");
        builder.AppendLine("            return " + ParseResultType + ".Failure(" + varLocalName + "Result.Diagnostic!);");
        EmitVariableValueAssignment(builder, field, varLocalName);
    }

    protected abstract void EmitVariableValueAssignment(
        StringBuilder builder,
        VariableSyntaxField field,
        string varLocalName);

    protected static string GetVariableSyntaxLocalName(VariableSyntaxField field)
    {
        return EmitterHelpers.LowerFirst(field.Name) + "Syntax";
    }

    private static string BuildStopTokensExpression(IReadOnlyList<TokenKind> stopTokens)
    {
        if (stopTokens.Count == 0)
        {
            return string.Empty;
        }

        var parts = new List<string>(stopTokens.Count);
        foreach (var kind in stopTokens)
        {
            parts.Add("TokenKind." + kind);
        }

        return string.Join(", ", parts);
    }
}

internal sealed class TypeTryParseBodyEmitter : AttrOrTypeTryParseBodyEmitter
{
    public TypeTryParseBodyEmitter(LoweredAssemblyFormat lowered, string syntaxClassName)
        : base(lowered, syntaxClassName)
    {
    }

    protected override string ResultSyntaxType => "TypeSyntax";

    protected override void EmitVariableValueAssignment(
        StringBuilder builder,
        VariableSyntaxField field,
        string varLocalName)
    {
        builder.AppendLine("        var " + varLocalName + " = (" + field.SyntaxType + ")" + varLocalName + "Result.Value;");
    }
}

internal sealed class AttributeTryParseBodyEmitter : AttrOrTypeTryParseBodyEmitter
{
    public AttributeTryParseBodyEmitter(LoweredAssemblyFormat lowered, string syntaxClassName)
        : base(lowered, syntaxClassName)
    {
    }

    protected override string ResultSyntaxType => "AttributeValueSyntax";

    protected override void EmitVariableValueAssignment(
        StringBuilder builder,
        VariableSyntaxField field,
        string varLocalName)
    {
        if (string.Equals(field.SyntaxType, "AttributeValueSyntax", System.StringComparison.Ordinal))
        {
            builder.AppendLine("        var " + varLocalName + " = " + varLocalName + "Result.Value;");
        }
        else
        {
            builder.AppendLine("        var " + varLocalName + " = (" + field.SyntaxType + ")" + varLocalName + "Result.Value;");
        }
    }
}
