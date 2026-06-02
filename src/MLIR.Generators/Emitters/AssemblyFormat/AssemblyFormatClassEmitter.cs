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
            node.Accept(new ParseNodeVisitor(builder, subject, "        "));
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

    private sealed class ParseNodeVisitor : IFormatNodeVisitor
    {
        private readonly StringBuilder builder;
        private readonly FormatSubject subject;
        private readonly string indent;

        public ParseNodeVisitor(StringBuilder builder, FormatSubject subject, string indent)
        {
            this.builder = builder;
            this.subject = subject;
            this.indent = indent;
        }

        public void VisitTrivia(TriviaNode trivia)
        {
        }

        public void VisitLiteralToken(LiteralTokenSlot slot)
            => EmitParseSlot(builder, subject, slot, indent);

        public void VisitAttributeValue(AttributeValueSlot slot)
            => EmitParseSlot(builder, subject, slot, indent);

        public void VisitType(TypeSlot slot)
            => EmitParseSlot(builder, subject, slot, indent);

        public void VisitTypeList(TypeListSlot slot)
            => EmitParseSlot(builder, subject, slot, indent);

        public void VisitSsaValue(SsaValueSlot slot)
            => EmitParseSlot(builder, subject, slot, indent);

        public void VisitSsaValueList(SsaValueListSlot slot)
            => EmitParseSlot(builder, subject, slot, indent);

        public void VisitAttrDict(AttrDictSlot slot)
        {
            EmitParseSlot(builder, subject, slot, indent);
        }

        public void VisitAttrDictWithKeyword(AttrDictWithKeywordSlot slot)
        {
            EmitParseSlot(builder, subject, slot, indent);
        }

        public void VisitRegion(RegionSlot slot)
        {
            EmitParseSlot(builder, subject, slot, indent);
        }

        public void VisitOptionalSyntax(OptionalSyntaxNode optionalSyntax)
        {
            builder.AppendLine(indent + optionalSyntax.CsType + " " + optionalSyntax.ParameterName + " = null;");
            builder.AppendLine(indent + "if (" + optionalSyntax.CanStartExpression + ")");
            builder.AppendLine(indent + "{");
            EmitParseNodeBody(builder, subject, optionalSyntax, indent + "    ");
            builder.AppendLine(indent + "}");
        }

        public void VisitOilist(OilistNode oilist)
        {
            EmitParseOilist(builder, subject, oilist, indent);
        }
    }

    private static void EmitParseOilist(StringBuilder builder, FormatSubject subject, OilistNode oilist, string indent)
    {
        foreach (var clause in oilist.Clauses)
        {
            builder.AppendLine(indent + clause.CsType + " " + clause.ParameterName + " = null;");
        }

        if (oilist.Clauses.Count == 0)
        {
            return;
        }

        builder.AppendLine(indent + "while (" + string.Join(" || ", oilist.Clauses.Select(static clause => clause.CanStartExpression)) + ")");
        builder.AppendLine(indent + "{");
        for (var i = 0; i < oilist.Clauses.Count; i++)
        {
            var clause = oilist.Clauses[i];
            builder.AppendLine((i == 0 ? indent + "    if (" : indent + "    else if (") + clause.CanStartExpression + ")");
            builder.AppendLine(indent + "    {");
            builder.AppendLine(indent + "        if (" + clause.ParameterName + " != null)");
            builder.AppendLine(indent + "            return global::MLIR.Text.ParseResult<" + subject.SyntaxReturnType + ">.Failure(context.CreateDiagnostic(\"Duplicate oilist clause '" + clause.Name + "'.\"));");
            EmitParseNodeBody(builder, subject, clause, indent + "        ");
            builder.AppendLine(indent + "    }");
        }

        builder.AppendLine(indent + "    else");
        builder.AppendLine(indent + "    {");
        builder.AppendLine(indent + "        break;");
        builder.AppendLine(indent + "    }");
        builder.AppendLine(indent + "}");
    }

    private static void EmitParseNodeBody(StringBuilder builder, FormatSubject subject, OptionalSyntaxNode group, string indent)
    {
        var syntaxNodes = group.Nodes.Where(static child => child.IsSyntaxNode).ToArray();
        foreach (var child in syntaxNodes)
        {
            child.Accept(new ParseNodeVisitor(builder, subject, indent));
        }

        builder.Append(indent + group.ParameterName + " = new " + group.SyntaxClassName + "(");
        for (var i = 0; i < syntaxNodes.Length; i++)
        {
            if (i > 0)
            {
                builder.Append(", ");
            }

            builder.Append(syntaxNodes[i].ParameterName);
        }

        builder.AppendLine(");");
    }

    private static void EmitParseSlot(StringBuilder builder, FormatSubject subject, FormatSlot slot, string indent)
    {
        builder.AppendLine(indent + "var " + slot.ParameterName + "Result = " + slot.ParseExpression + ";");
        builder.AppendLine(indent + "if (!" + slot.ParameterName + "Result.IsSuccess)");
        builder.AppendLine(indent + "    return global::MLIR.Text.ParseResult<" + subject.SyntaxReturnType + ">.Failure(" + slot.ParameterName + "Result.Diagnostic!);");
        builder.AppendLine(indent + "var " + slot.ParameterName + " = " + slot.ParseValueExpression + ";");
    }
}
