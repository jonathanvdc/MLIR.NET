namespace MLIR.Generators.Emitters.AssemblyFormat;

using System.Collections.Generic;
using System.Linq;
using System.Text;
using MLIR.Generators.Emitters.Common;

/// <summary>
/// Emits the generated CST nodes that capture parsed declarative assembly-format subtrees.
/// Parent operation/type/attribute bodies, optional groups, and oilist clauses are all emitted
/// through the same node-shape path so nested format structure stays regular.
/// </summary>
internal static class AssemblyFormatSyntaxClassEmitter
{
    public static void Emit(StringBuilder builder, FormatSubject subject, AssemblyFormatPlan plan)
    {
        foreach (var group in EnumerateGroupClasses(plan.Nodes))
        {
            EmitSyntaxClass(
                builder,
                group.SyntaxClassName,
                "global::MLIR.Syntax.SyntaxNode",
                hasPrefix: false,
                prefixType: null,
                nodes: group.Nodes,
                subject: null);
            builder.AppendLine();
        }

        EmitSyntaxClass(
            builder,
            subject.SyntaxClassName,
            subject.SyntaxBaseType,
            subject.HasPrefix,
            subject.HasPrefix ? subject.PrefixType : null,
            plan.Nodes,
            subject);
    }

    private static IEnumerable<OptionalGroupNode> EnumerateGroupClasses(IEnumerable<FormatNode> nodes)
    {
        foreach (var node in nodes)
        {
            if (node is OptionalGroupNode group)
            {
                yield return group;
                foreach (var childGroup in EnumerateGroupClasses(group.Nodes))
                {
                    yield return childGroup;
                }
            }
            else if (node is OilistNode oilist)
            {
                foreach (var clause in oilist.Clauses)
                {
                    yield return clause;
                    foreach (var childGroup in EnumerateGroupClasses(clause.Nodes))
                    {
                        yield return childGroup;
                    }
                }
            }
        }
    }

    private static void EmitSyntaxClass(
        StringBuilder builder,
        string className,
        string baseType,
        bool hasPrefix,
        string? prefixType,
        IReadOnlyList<FormatNode> nodes,
        FormatSubject? subject)
    {
        var syntaxNodes = nodes.DescendantSyntaxNodes().ToArray();
        builder.AppendLine("internal sealed class " + className + " : " + baseType);
        builder.AppendLine("{");
        EmitConstructor(builder, className, hasPrefix, prefixType, syntaxNodes);
        EmitProperties(builder, syntaxNodes);
        EmitLocationProperty(builder, hasPrefix, syntaxNodes);
        builder.AppendLine();
        EmitWriteTo(builder, hasPrefix, nodes, subject);
        builder.AppendLine();
        EmitRewrite(builder, className, hasPrefix, syntaxNodes);
        builder.AppendLine("}");
    }

    private static void EmitConstructor(
        StringBuilder builder,
        string className,
        bool hasPrefix,
        string? prefixType,
        IReadOnlyList<FormatNode> syntaxNodes)
    {
        builder.Append("    public " + className + "(");
        if (hasPrefix)
        {
            builder.Append(prefixType + " prefix");
        }

        var firstParameter = !hasPrefix;
        foreach (var node in syntaxNodes)
        {
            if (!firstParameter)
            {
                builder.Append(", ");
            }

            firstParameter = false;
            builder.Append(node.CsType + " " + node.ParameterName);
        }

        builder.AppendLine(")");
        if (hasPrefix)
        {
            builder.AppendLine("        : base(prefix)");
        }

        builder.AppendLine("    {");
        foreach (var node in syntaxNodes)
        {
            builder.AppendLine("        " + node.PropertyName + " = " + node.ParameterName + ";");
        }

        builder.AppendLine("    }");
        builder.AppendLine();
    }

    private static void EmitProperties(StringBuilder builder, IReadOnlyList<FormatNode> syntaxNodes)
    {
        foreach (var node in syntaxNodes)
        {
            builder.AppendLine("    public " + node.CsType + " " + node.PropertyName + " { get; }");
        }

        if (syntaxNodes.Count > 0)
        {
            builder.AppendLine();
        }
    }

    private static void EmitLocationProperty(StringBuilder builder, bool hasPrefix, IReadOnlyList<FormatNode> syntaxNodes)
    {
        var locations = new List<string>();
        if (hasPrefix)
        {
            locations.Add("Prefix.Location");
        }

        foreach (var node in syntaxNodes)
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

    private static void EmitWriteTo(
        StringBuilder builder,
        bool hasPrefix,
        IReadOnlyList<FormatNode> nodes,
        FormatSubject? subject)
    {
        builder.AppendLine("    public override void WriteTo(global::MLIR.Text.SyntaxWriter writer)");
        builder.AppendLine("    {");
        if (hasPrefix)
        {
            builder.AppendLine("        WritePrefix(writer);");
        }

        EmitWriteNodeSequence(builder, nodes, subject, "        ");
        builder.AppendLine("    }");
    }

    private static void EmitWriteNodeSequence(
        StringBuilder builder,
        IReadOnlyList<FormatNode> nodes,
        FormatSubject? subject,
        string indent)
    {
        var spacing = AssemblyFormatPrinterSpacing.Initial;
        foreach (var node in nodes)
        {
            if (node is OilistNode oilist)
            {
                foreach (var clause in oilist.Clauses)
                {
                    EmitWriteOptionalNode(builder, clause, subject, indent, ref spacing);
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
                EmitWriteOptionalNode(builder, group, subject, indent, ref spacing);
                continue;
            }

            var slot = (FormatSlot)node;
            EmitWriteSlot(builder, slot, spacing.GetLeadingTrivia(slot, subject), indent);
            spacing.MarkEmitted(slot);
        }
    }

    private static void EmitWriteOptionalNode(
        StringBuilder builder,
        OptionalGroupNode group,
        FormatSubject? subject,
        string indent,
        ref AssemblyFormatPrinterSpacing spacing)
    {
        builder.AppendLine(indent + "if (" + group.PropertyName + " != null)");
        builder.AppendLine(indent + "{");
        builder.AppendLine(indent + "    writer.SuggestTrivia(" + EmitterHelpers.ToCSharpStringLiteral(spacing.GetLeadingTrivia(group, subject)) + ");");
        builder.AppendLine(indent + "    " + group.PropertyName + ".WriteTo(writer);");
        builder.AppendLine(indent + "}");
        spacing.MarkEmitted(group);
    }

    private static void EmitWriteSlot(StringBuilder builder, FormatSlot slot, string trivia, string indent)
    {
        switch (slot.Kind)
        {
            case FormatSlotKind.LiteralToken:
                builder.AppendLine(indent + "writer.WriteToken(" + slot.PropertyName + ", " + EmitterHelpers.ToCSharpStringLiteral(trivia) + ");");
                break;
            case FormatSlotKind.AttributeValue:
                builder.AppendLine(indent + "writer.SuggestTrivia(" + EmitterHelpers.ToCSharpStringLiteral(trivia) + ");");
                builder.AppendLine(indent + slot.PropertyName + ".WriteTo(writer);");
                break;
            case FormatSlotKind.Type:
                builder.AppendLine(indent + "writer.SuggestTrivia(" + EmitterHelpers.ToCSharpStringLiteral(trivia) + ");");
                builder.AppendLine(indent + slot.PropertyName + ".WriteTo(writer);");
                break;
            case FormatSlotKind.SsaValue:
                builder.AppendLine(indent + "writer.WriteToken(" + slot.PropertyName + ", " + EmitterHelpers.ToCSharpStringLiteral(trivia) + ");");
                break;
            case FormatSlotKind.SsaValueList:
                builder.AppendLine(indent + "writer.SuggestTrivia(" + EmitterHelpers.ToCSharpStringLiteral(trivia) + ");");
                builder.AppendLine(indent + "writer.WriteSeparatedList(" + slot.PropertyName + ");");
                break;
            case FormatSlotKind.AttrDict:
                builder.AppendLine(indent + "writer.WriteDelimitedList(" + slot.PropertyName + ", " + EmitterHelpers.ToCSharpStringLiteral(trivia) + ");");
                break;
        }
    }

    private static void EmitRewrite(
        StringBuilder builder,
        string className,
        bool hasPrefix,
        IReadOnlyList<FormatNode> syntaxNodes)
    {
        builder.AppendLine("    public override global::MLIR.Syntax.SyntaxNode Rewrite(global::MLIR.Syntax.SyntaxRewriter rewriter)");
        builder.AppendLine("    {");
        builder.Append("        return new " + className + "(");
        var needsComma = false;
        if (hasPrefix)
        {
            builder.Append("Prefix");
            needsComma = true;
        }

        foreach (var node in syntaxNodes)
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
}
