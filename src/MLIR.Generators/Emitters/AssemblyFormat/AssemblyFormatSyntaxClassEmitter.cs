namespace MLIR.Generators.Emitters.AssemblyFormat;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MLIR.Generators.Emitters.Common;

/// <summary>
/// Emits the generated CST node that captures the parsed slots of a declarative assembly format.
/// </summary>
internal static class AssemblyFormatSyntaxClassEmitter
{
    public static void Emit(StringBuilder builder, FormatSubject subject, AssemblyFormatPlan plan)
    {
        foreach (var group in plan.SyntaxNodes.OfType<OptionalGroupNode>())
        {
            EmitGroupClass(builder, group);
            builder.AppendLine();
        }

        builder.AppendLine("internal sealed class " + subject.SyntaxClassName + " : " + subject.SyntaxBaseType);
        builder.AppendLine("{");
        EmitConstructor(builder, subject, plan);
        EmitProperties(builder, plan);
        EmitLocationProperty(builder, subject, plan);
        builder.AppendLine();
        EmitWriteTo(builder, subject, plan);
        builder.AppendLine();
        EmitRewrite(builder, subject, plan);
        builder.AppendLine("}");
    }

    private static void EmitConstructor(StringBuilder builder, FormatSubject subject, AssemblyFormatPlan plan)
    {
        builder.Append("    public " + subject.SyntaxClassName + "(");
        if (subject.HasPrefix)
        {
            builder.Append(subject.PrefixType + " prefix");
        }

        var firstParameter = !subject.HasPrefix;
        foreach (var node in plan.SyntaxNodes)
        {
            if (!firstParameter)
            {
                builder.Append(", ");
            }

            firstParameter = false;
            builder.Append(node.CsType + " " + node.ParameterName);
        }

        builder.AppendLine(")");
        if (subject.HasPrefix)
        {
            builder.AppendLine("        : base(prefix)");
        }

        builder.AppendLine("    {");
        foreach (var node in plan.SyntaxNodes)
        {
            builder.AppendLine("        " + node.PropertyName + " = " + node.ParameterName + ";");
        }

        builder.AppendLine("    }");
        builder.AppendLine();
    }

    private static void EmitProperties(StringBuilder builder, AssemblyFormatPlan plan)
    {
        foreach (var node in plan.SyntaxNodes)
        {
            builder.AppendLine("    public " + node.CsType + " " + node.PropertyName + " { get; }");
        }

        if (plan.SyntaxNodes.Any())
        {
            builder.AppendLine();
        }
    }

    private static void EmitWriteTo(StringBuilder builder, FormatSubject subject, AssemblyFormatPlan plan)
    {
        builder.AppendLine("    public override void WriteTo(global::MLIR.Text.SyntaxWriter writer)");
        builder.AppendLine("    {");
        if (subject.HasPrefix)
        {
            builder.AppendLine("        WritePrefix(writer);");
        }

        var spacing = AssemblyFormatPrinterSpacing.Initial;
        foreach (var node in plan.Nodes)
        {
            if (node is OilistNode oilist)
            {
                foreach (var clause in oilist.Clauses)
                {
                    builder.AppendLine("        if (" + clause.PropertyName + " != null)");
                    builder.AppendLine("        {");
                    builder.AppendLine("            writer.SuggestTrivia(" + EmitterHelpers.ToCSharpStringLiteral(spacing.GetLeadingTrivia(clause, subject)) + ");");
                    builder.AppendLine("            " + clause.PropertyName + ".WriteTo(writer);");
                    builder.AppendLine("        }");
                    spacing.MarkEmitted(clause);
                }

                continue;
            }

            if (!node.IsSyntaxNode)
            {
                if (node is FormatSlot triviaSlot)
                {
                    spacing.ApplyExplicitTrivia(triviaSlot.TriviaText ?? string.Empty);
                }
                continue;
            }

            if (node is OptionalGroupNode group)
            {
                builder.AppendLine("        if (" + group.PropertyName + " != null)");
                builder.AppendLine("        {");
                builder.AppendLine("            writer.SuggestTrivia(" + EmitterHelpers.ToCSharpStringLiteral(spacing.GetLeadingTrivia(group, subject)) + ");");
                builder.AppendLine("            " + group.PropertyName + ".WriteTo(writer);");
                builder.AppendLine("        }");
                spacing.MarkEmitted(group);
                continue;
            }

            var slot = (FormatSlot)node;
            var trivia = spacing.GetLeadingTrivia(slot, subject);
            switch (slot.Kind)
            {
                case FormatSlotKind.LiteralToken:
                    builder.AppendLine("        writer.WriteToken(" + slot.PropertyName + ", " + EmitterHelpers.ToCSharpStringLiteral(trivia) + ");");
                    break;
                case FormatSlotKind.AttributeValue:
                    builder.AppendLine("        writer.SuggestTrivia(" + EmitterHelpers.ToCSharpStringLiteral(trivia) + ");");
                    builder.AppendLine("        " + slot.PropertyName + ".WriteTo(writer);");
                    break;
                case FormatSlotKind.Type:
                    builder.AppendLine("        writer.SuggestTrivia(" + EmitterHelpers.ToCSharpStringLiteral(trivia) + ");");
                    builder.AppendLine("        " + slot.PropertyName + ".WriteTo(writer);");
                    break;
                case FormatSlotKind.SsaValue:
                    builder.AppendLine("        writer.WriteToken(" + slot.PropertyName + ", " + EmitterHelpers.ToCSharpStringLiteral(trivia) + ");");
                    break;
                case FormatSlotKind.SsaValueList:
                    builder.AppendLine("        writer.SuggestTrivia(" + EmitterHelpers.ToCSharpStringLiteral(trivia) + ");");
                    builder.AppendLine("        writer.WriteSeparatedList(" + slot.PropertyName + ");");
                    break;
                case FormatSlotKind.AttrDict:
                    builder.AppendLine("        writer.WriteDelimitedList(" + slot.PropertyName + ", " + EmitterHelpers.ToCSharpStringLiteral(trivia) + ");");
                    break;
            }

            spacing.MarkEmitted(node);
        }

        builder.AppendLine("    }");
    }

    private static void EmitRewrite(StringBuilder builder, FormatSubject subject, AssemblyFormatPlan plan)
    {
        builder.AppendLine("    public override global::MLIR.Syntax.SyntaxNode Rewrite(global::MLIR.Syntax.SyntaxRewriter rewriter)");
        builder.AppendLine("    {");
        builder.Append("        return new " + subject.SyntaxClassName + "(");
        var needsComma = false;
        if (subject.HasPrefix)
        {
            builder.Append("Prefix");
            needsComma = true;
        }

        foreach (var node in plan.SyntaxNodes)
        {
            if (needsComma)
            {
                builder.Append(", ");
            }

            needsComma = true;
            builder.Append(node.RewriteExpression);
        }

        builder.AppendLine(");");
        builder.AppendLine("    }");
    }

    private static void EmitLocationProperty(StringBuilder builder, FormatSubject subject, AssemblyFormatPlan plan)
    {
        var locations = new List<string>();
        if (subject.HasPrefix)
        {
            locations.Add("Prefix.Location");
        }

        foreach (var node in plan.SyntaxNodes)
        {
            locations.Add(node.LocationExpression);
        }

        builder.AppendLine("    public override SourceLocation Location");
        builder.AppendLine("    {");
        builder.AppendLine("        get");
        builder.AppendLine("        {");
        if (locations.Count == 0)
        {
            builder.AppendLine("            return SourceLocation.Unknown;");
        }
        else
        {
            builder.AppendLine("            var result = " + locations[0] + ";");
            foreach (var location in locations.Skip(1))
            {
                builder.AppendLine("            result = SourceLocation.Merge(result, " + location + ");");
            }

            builder.AppendLine("            return result;");
        }

        builder.AppendLine("        }");
        builder.AppendLine("    }");
    }

    private static void EmitGroupClass(StringBuilder builder, OptionalGroupNode group)
    {
        builder.AppendLine("internal sealed class " + group.SyntaxClassName + " : global::MLIR.Syntax.SyntaxNode");
        builder.AppendLine("{");
        builder.Append("    public " + group.SyntaxClassName + "(");
        var first = true;
        foreach (var node in group.Nodes.Where(static node => node.IsSyntaxNode))
        {
            if (!first)
            {
                builder.Append(", ");
            }

            first = false;
            builder.Append(node.CsType + " " + node.ParameterName);
        }

        builder.AppendLine(")");
        builder.AppendLine("    {");
        foreach (var node in group.Nodes.Where(static node => node.IsSyntaxNode))
        {
            builder.AppendLine("        " + node.PropertyName + " = " + node.ParameterName + ";");
        }

        builder.AppendLine("    }");
        builder.AppendLine();
        foreach (var node in group.Nodes.Where(static node => node.IsSyntaxNode))
        {
            builder.AppendLine("    public " + node.CsType + " " + node.PropertyName + " { get; }");
        }

        builder.AppendLine();
        builder.AppendLine("    public override SourceLocation Location");
        builder.AppendLine("    {");
        builder.AppendLine("        get");
        builder.AppendLine("        {");
        var locations = group.Nodes.Where(static node => node.IsSyntaxNode).Select(static node => node.LocationExpression).ToArray();
        if (locations.Length == 0)
        {
            builder.AppendLine("            return SourceLocation.Unknown;");
        }
        else
        {
            builder.AppendLine("            var result = " + locations[0] + ";");
            foreach (var location in locations.Skip(1))
            {
                builder.AppendLine("            result = SourceLocation.Merge(result, " + location + ");");
            }

            builder.AppendLine("            return result;");
        }

        builder.AppendLine("        }");
        builder.AppendLine("    }");
        builder.AppendLine();
        EmitGroupWriteTo(builder, group);
        builder.AppendLine();
        builder.AppendLine("    public override global::MLIR.Syntax.SyntaxNode Rewrite(global::MLIR.Syntax.SyntaxRewriter rewriter)");
        builder.AppendLine("    {");
        builder.Append("        return new " + group.SyntaxClassName + "(");
        first = true;
        foreach (var node in group.Nodes.Where(static node => node.IsSyntaxNode))
        {
            if (!first)
            {
                builder.Append(", ");
            }

            first = false;
            builder.Append(node.RewriteExpression);
        }

        builder.AppendLine(");");
        builder.AppendLine("    }");
        builder.AppendLine("}");
    }

    private static void EmitGroupWriteTo(StringBuilder builder, OptionalGroupNode group)
    {
        builder.AppendLine("    public override void WriteTo(global::MLIR.Text.SyntaxWriter writer)");
        builder.AppendLine("    {");
        var spacing = AssemblyFormatPrinterSpacing.Initial;
        foreach (var node in group.Nodes)
        {
            if (!node.IsSyntaxNode)
            {
                if (node is FormatSlot triviaSlot)
                {
                    spacing.ApplyExplicitTrivia(triviaSlot.TriviaText ?? string.Empty);
                }
                continue;
            }

            var slot = (FormatSlot)node;
            var trivia = spacing.GetLeadingTrivia(slot, null);
            switch (slot.Kind)
            {
                case FormatSlotKind.LiteralToken:
                    builder.AppendLine("        writer.WriteToken(" + slot.PropertyName + ", " + EmitterHelpers.ToCSharpStringLiteral(trivia) + ");");
                    break;
                case FormatSlotKind.AttributeValue:
                    builder.AppendLine("        writer.SuggestTrivia(" + EmitterHelpers.ToCSharpStringLiteral(trivia) + ");");
                    builder.AppendLine("        " + slot.PropertyName + ".WriteTo(writer);");
                    break;
                case FormatSlotKind.Type:
                    builder.AppendLine("        writer.SuggestTrivia(" + EmitterHelpers.ToCSharpStringLiteral(trivia) + ");");
                    builder.AppendLine("        " + slot.PropertyName + ".WriteTo(writer);");
                    break;
                case FormatSlotKind.SsaValue:
                    builder.AppendLine("        writer.WriteToken(" + slot.PropertyName + ", " + EmitterHelpers.ToCSharpStringLiteral(trivia) + ");");
                    break;
                case FormatSlotKind.SsaValueList:
                    builder.AppendLine("        writer.SuggestTrivia(" + EmitterHelpers.ToCSharpStringLiteral(trivia) + ");");
                    builder.AppendLine("        writer.WriteSeparatedList(" + slot.PropertyName + ");");
                    break;
                case FormatSlotKind.AttrDict:
                    builder.AppendLine("        writer.WriteDelimitedList(" + slot.PropertyName + ", " + EmitterHelpers.ToCSharpStringLiteral(trivia) + ");");
                    break;
            }

            spacing.MarkEmitted(node);
        }

        builder.AppendLine("    }");
    }
}
