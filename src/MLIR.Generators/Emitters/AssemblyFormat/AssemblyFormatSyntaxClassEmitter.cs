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

    private static IEnumerable<OptionalSyntaxNode> EnumerateGroupClasses(IEnumerable<FormatNode> nodes)
    {
        var collector = new GroupClassCollector();
        foreach (var node in nodes)
        {
            node.Accept(collector);
        }

        return collector.Groups;
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
        var visitor = new WriteToNodeVisitor(builder, subject, indent);
        foreach (var node in nodes)
        {
            node.Accept(visitor);
        }
    }

    private sealed class GroupClassCollector : IFormatNodeVisitor
    {
        public List<OptionalSyntaxNode> Groups { get; } = [];

        public void VisitTrivia(TriviaNode trivia)
        {
        }

        public void VisitLiteralToken(LiteralTokenSlot slot) { }

        public void VisitAttributeValue(AttributeValueSlot slot) { }

        public void VisitType(TypeSlot slot) { }

        public void VisitSsaValue(SsaValueSlot slot) { }

        public void VisitSsaValueList(SsaValueListSlot slot) { }

        public void VisitAttrDict(AttrDictSlot slot) { }

        public void VisitAttrDictWithKeyword(AttrDictWithKeywordSlot slot) { }

        public void VisitOptionalSyntax(OptionalSyntaxNode optionalSyntax)
        {
            Groups.Add(optionalSyntax);

            foreach (var child in optionalSyntax.Nodes)
            {
                child.Accept(this);
            }
        }

        public void VisitOilist(OilistNode oilist)
        {
            foreach (var clause in oilist.Clauses)
            {
                VisitOptionalSyntax(clause);
            }
        }
    }

    private sealed class WriteToNodeVisitor : IFormatNodeVisitor
    {
        private readonly StringBuilder builder;
        private readonly FormatSubject? subject;
        private readonly string indent;
        private AssemblyFormatPrinterSpacing spacing = AssemblyFormatPrinterSpacing.Initial;

        public WriteToNodeVisitor(StringBuilder builder, FormatSubject? subject, string indent)
        {
            this.builder = builder;
            this.subject = subject;
            this.indent = indent;
        }

        public void VisitTrivia(TriviaNode trivia)
        {
            spacing.ApplyExplicitTrivia(trivia.Text);
        }

        public void VisitLiteralToken(LiteralTokenSlot slot)
        {
            builder.AppendLine(indent + "writer.WriteToken(" + slot.PropertyName + ", " + EmitterHelpers.ToCSharpStringLiteral(spacing.GetLeadingTrivia(slot, subject)) + ");");
            spacing.MarkEmitted(slot);
        }

        public void VisitAttributeValue(AttributeValueSlot slot)
            => EmitWriteSyntaxNode(slot);

        public void VisitType(TypeSlot slot)
            => EmitWriteSyntaxNode(slot);

        public void VisitSsaValue(SsaValueSlot slot)
        {
            builder.AppendLine(indent + "writer.WriteToken(" + slot.PropertyName + ", " + EmitterHelpers.ToCSharpStringLiteral(spacing.GetLeadingTrivia(slot, subject)) + ");");
            spacing.MarkEmitted(slot);
        }

        public void VisitSsaValueList(SsaValueListSlot slot)
        {
            builder.AppendLine(indent + "writer.SuggestTrivia(" + EmitterHelpers.ToCSharpStringLiteral(spacing.GetLeadingTrivia(slot, subject)) + ");");
            builder.AppendLine(indent + "writer.WriteSeparatedList(" + slot.PropertyName + ");");
            spacing.MarkEmitted(slot);
        }

        public void VisitAttrDict(AttrDictSlot slot)
        {
            builder.AppendLine(indent + "writer.WriteDelimitedList(" + slot.PropertyName + ", " + EmitterHelpers.ToCSharpStringLiteral(spacing.GetLeadingTrivia(slot, subject)) + ");");
            spacing.MarkEmitted(slot);
        }

        public void VisitAttrDictWithKeyword(AttrDictWithKeywordSlot slot)
        {
            builder.AppendLine(indent + "writer.SuggestTrivia(" + EmitterHelpers.ToCSharpStringLiteral(spacing.GetLeadingTrivia(slot, subject)) + ");");
            builder.AppendLine(indent + slot.PropertyName + ".WriteTo(writer);");
            spacing.MarkEmitted(slot);
        }

        public void VisitOptionalSyntax(OptionalSyntaxNode optionalSyntax)
        {
            EmitWriteOptionalNode(builder, optionalSyntax, subject, indent, ref spacing);
        }

        public void VisitOilist(OilistNode oilist)
        {
            foreach (var clause in oilist.Clauses)
            {
                VisitOptionalSyntax(clause);
            }
        }

        private void EmitWriteSyntaxNode(FormatSlot slot)
        {
            builder.AppendLine(indent + "writer.SuggestTrivia(" + EmitterHelpers.ToCSharpStringLiteral(spacing.GetLeadingTrivia(slot, subject)) + ");");
            builder.AppendLine(indent + slot.PropertyName + ".WriteTo(writer);");
            spacing.MarkEmitted(slot);
        }
    }

    private static void EmitWriteOptionalNode(
        StringBuilder builder,
        OptionalSyntaxNode group,
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
