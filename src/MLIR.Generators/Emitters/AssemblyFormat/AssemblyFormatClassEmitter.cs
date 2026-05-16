namespace MLIR.Generators.Emitters.AssemblyFormat;

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
        foreach (var slot in plan.SyntaxSlots)
        {
            builder.AppendLine("        var " + slot.ParameterName + "Result = " + slot.ParseExpression + ";");
            builder.AppendLine("        if (!" + slot.ParameterName + "Result.IsSuccess)");
            builder.AppendLine("            return global::MLIR.Text.ParseResult<" + subject.SyntaxReturnType + ">.Failure(" + slot.ParameterName + "Result.Diagnostic!);");
            builder.AppendLine("        var " + slot.ParameterName + " = " + slot.ParseValueExpression + ";");
        }

        builder.Append("        return global::MLIR.Text.ParseResult<" + subject.SyntaxReturnType + ">.Success(new " + subject.SyntaxClassName + "(");
        var needsComma = false;
        if (subject.HasPrefix)
        {
            builder.Append("prefix");
            needsComma = true;
        }

        foreach (var slot in plan.SyntaxSlots)
        {
            if (needsComma)
            {
                builder.Append(", ");
            }

            needsComma = true;
            builder.Append(slot.ParameterName);
        }

        builder.AppendLine("));");
    }
}
