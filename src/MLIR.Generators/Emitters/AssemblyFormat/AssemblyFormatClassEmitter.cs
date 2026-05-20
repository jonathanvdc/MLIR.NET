namespace MLIR.Generators.Emitters.AssemblyFormat;

using System;
using System.Linq;
using System.Text;
using MLIR.Generators.Emitters.Common;

/// <summary>
/// Emits the runtime assembly-format hook that parses, binds, and rebuilds custom syntax.
/// </summary>
internal static class AssemblyFormatClassEmitter
{
    public static void Emit(StringBuilder builder, FormatSubject subject, AssemblyFormatPlan plan)
    {
        builder.AppendLine("internal sealed class " + subject.FormatClassName + " : " + subject.FormatBaseType);
        builder.AppendLine("{");
        builder.AppendLine("    public " + subject.FormatClassName + "()");
        if (subject.HasFormatMnemonicConstructor)
        {
            builder.AppendLine("        : base(" + EmitterHelpers.ToCSharpStringLiteral(subject.FormatMnemonic) + ")");
        }

        builder.AppendLine("    {");
        builder.AppendLine("    }");
        builder.AppendLine();
        subject.EmitTryParseSignature(builder);
        builder.AppendLine("    {");
        if (!plan.IsSupported)
        {
            EmitUnsupportedParseFailure(builder, subject, plan);
        }
        else
        {
            EmitTryParseBody(builder, subject, plan);
        }

        builder.AppendLine("    }");
        builder.AppendLine();
        subject.EmitBindMethod(builder, plan);
        builder.AppendLine();
        subject.EmitBuildMethod(builder, plan);
        builder.AppendLine("}");
    }

    private static void EmitUnsupportedParseFailure(StringBuilder builder, FormatSubject subject, AssemblyFormatPlan plan)
    {
        var message = "Unsupported declarative assembly format construct: " + plan.UnsupportedFeatures[0] + ".";
        builder.AppendLine("        return global::MLIR.Text.ParseResult<" + subject.SyntaxReturnType + ">.Failure(context.CreateDiagnostic(" + EmitterHelpers.ToCSharpStringLiteral(message) + "));");
    }

    private static void EmitTryParseBody(StringBuilder builder, FormatSubject subject, AssemblyFormatPlan plan)
    {
        foreach (var node in plan.Nodes)
        {
            EmitParseNode(builder, subject, node);
        }

        builder.Append("        return global::MLIR.Text.ParseResult<" + subject.SyntaxReturnType + ">.Success(new " + subject.SyntaxClassName + "(");
        var needsComma = false;
        if (subject.HasPrefix)
        {
            builder.Append("prefix");
            needsComma = true;
        }

        foreach (var node in plan.SyntaxNodes)
        {
            if (needsComma)
            {
                builder.Append(", ");
            }

            needsComma = true;
            builder.Append(node.ParameterName);
        }

        builder.AppendLine("));");
    }

    private static void EmitParseNode(StringBuilder builder, FormatSubject subject, FormatNode node)
    {
        if (!node.IsSyntaxNode && node is not OilistNode)
        {
            return;
        }

        if (node is FormatSlot slot)
        {
            EmitParseSlot(builder, subject, slot);
            return;
        }

        if (node is OptionalGroupNode group)
        {
            builder.AppendLine("        " + group.CsType + " " + group.ParameterName + " = null;");
            builder.AppendLine("        if (" + GetCanStartExpression(group) + ")");
            builder.AppendLine("        {");
            foreach (var child in group.Nodes.Where(static child => child.IsSyntaxNode))
            {
                EmitParseNode(builder, subject, child);
            }

            builder.Append("            " + group.ParameterName + " = new " + group.SyntaxClassName + "(");
            var needsComma = false;
            foreach (var child in group.Nodes.Where(static child => child.IsSyntaxNode))
            {
                if (needsComma)
                {
                    builder.Append(", ");
                }

                needsComma = true;
                builder.Append(child.ParameterName);
            }

            builder.AppendLine(");");
            builder.AppendLine("        }");
            return;
        }

        if (node is OilistNode oilist)
        {
            EmitParseOilist(builder, subject, oilist);
            return;
        }

        throw new InvalidOperationException("Unsupported format node '" + node.GetType().Name + "'.");
    }

    private static void EmitParseOilist(StringBuilder builder, FormatSubject subject, OilistNode oilist)
    {
        foreach (var clause in oilist.Clauses)
        {
            builder.AppendLine("        " + clause.CsType + " " + clause.ParameterName + " = null;");
        }

        if (oilist.Clauses.Count == 0)
        {
            return;
        }

        builder.AppendLine("        while (" + string.Join(" || ", oilist.Clauses.Select(GetCanStartExpression)) + ")");
        builder.AppendLine("        {");
        for (var i = 0; i < oilist.Clauses.Count; i++)
        {
            var clause = oilist.Clauses[i];
            builder.AppendLine((i == 0 ? "            if (" : "            else if (") + GetCanStartExpression(clause) + ")");
            builder.AppendLine("            {");
            builder.AppendLine("                if (" + clause.ParameterName + " != null)");
            builder.AppendLine("                    return global::MLIR.Text.ParseResult<" + subject.SyntaxReturnType + ">.Failure(context.CreateDiagnostic(\"Duplicate oilist clause '" + clause.Name + "'.\"));");
            foreach (var child in clause.Nodes.Where(static child => child.IsSyntaxNode))
            {
                EmitParseNode(builder, subject, child);
            }

            builder.Append("                " + clause.ParameterName + " = new " + clause.SyntaxClassName + "(");
            var needsComma = false;
            foreach (var child in clause.Nodes.Where(static child => child.IsSyntaxNode))
            {
                if (needsComma)
                {
                    builder.Append(", ");
                }

                needsComma = true;
                builder.Append(child.ParameterName);
            }

            builder.AppendLine(");");
            builder.AppendLine("            }");
        }

        builder.AppendLine("            else");
        builder.AppendLine("            {");
        builder.AppendLine("                break;");
        builder.AppendLine("            }");
        builder.AppendLine("        }");
    }

    private static void EmitParseSlot(StringBuilder builder, FormatSubject subject, FormatSlot slot)
    {
        builder.AppendLine("        var " + slot.ParameterName + "Result = " + slot.ParseExpression + ";");
        builder.AppendLine("        if (!" + slot.ParameterName + "Result.IsSuccess)");
        builder.AppendLine("            return global::MLIR.Text.ParseResult<" + subject.SyntaxReturnType + ">.Failure(" + slot.ParameterName + "Result.Diagnostic!);");
        builder.AppendLine("        var " + slot.ParameterName + " = " + slot.ParseValueExpression + ";");
    }

    private static string GetCanStartExpression(OptionalGroupNode group)
    {
        var first = group.Nodes.FirstOrDefault(static node => node.IsSyntaxNode);
        if (first is FormatSlot { Kind: FormatSlotKind.LiteralToken, IsKeyword: true } keyword)
        {
            return "context.IsKeyword(" + EmitterHelpers.ToCSharpStringLiteral(keyword.TokenText ?? string.Empty) + ")";
        }

        if (first is FormatSlot { Kind: FormatSlotKind.LiteralToken } literal)
        {
            return "context.Is(" + literal.TokenKindExpression + ")";
        }

        if (first is FormatSlot { Kind: FormatSlotKind.SsaValue })
        {
            return "context.Is(global::MLIR.Text.TokenKind.SsaName)";
        }

        if (first is FormatSlot { Kind: FormatSlotKind.SsaValueList })
        {
            return "context.Is(global::MLIR.Text.TokenKind.SsaName)";
        }

        return "false";
    }
}
