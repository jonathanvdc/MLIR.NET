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
        foreach (var slot in plan.SyntaxSlots)
        {
            if (!firstParameter)
            {
                builder.Append(", ");
            }

            firstParameter = false;
            builder.Append(slot.CsType + " " + slot.ParameterName);
        }

        builder.AppendLine(")");
        if (subject.HasPrefix)
        {
            builder.AppendLine("        : base(prefix)");
        }

        builder.AppendLine("    {");
        foreach (var slot in plan.SyntaxSlots)
        {
            builder.AppendLine("        " + slot.PropertyName + " = " + slot.ParameterName + ";");
        }

        builder.AppendLine("    }");
        builder.AppendLine();
    }

    private static void EmitProperties(StringBuilder builder, AssemblyFormatPlan plan)
    {
        foreach (var slot in plan.SyntaxSlots)
        {
            builder.AppendLine("    public " + slot.CsType + " " + slot.PropertyName + " { get; }");
        }

        if (plan.SyntaxSlots.Any())
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
        foreach (var slot in plan.Slots)
        {
            if (!slot.IsSyntaxSlot)
            {
                spacing.ApplyExplicitTrivia(slot.TriviaText ?? string.Empty);
                continue;
            }

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
                case FormatSlotKind.AttrDict:
                    builder.AppendLine("        writer.WriteDelimitedList(" + slot.PropertyName + ", " + EmitterHelpers.ToCSharpStringLiteral(trivia) + ");");
                    break;
            }

            spacing.MarkEmitted(slot);
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

        foreach (var slot in plan.SyntaxSlots)
        {
            if (needsComma)
            {
                builder.Append(", ");
            }

            needsComma = true;
            builder.Append(slot.RewriteExpression);
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

        foreach (var slot in plan.SyntaxSlots)
        {
            locations.Add(slot.LocationExpression);
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
}
