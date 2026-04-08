namespace MLIR.Generators.Emitters.Operation;

using System;
using System.Collections.Generic;
using System.Text;
using MLIR.Generators.Emitters;
using MLIR.Generators.Emitters.Common;
using MLIR.ODS.Model;
using MLIR.ODS.Model.AssemblyFormat;

internal static class OperationBodySyntaxEmitter
{
    public static OperationBodySyntaxMetadata Emit(StringBuilder builder, OperationModel operation)
    {
        var className = DialectGeneratorNaming.GetOperationClassName(operation);
        var assemblyFormat = operation.AssemblyFormat!;
        var metadata = ComputeBodySyntaxMetadata(operation, assemblyFormat, className);
        var fields = metadata.Fields;

        builder.AppendLine("public sealed class " + className + "BodySyntax : OperationBodySyntax");
        builder.AppendLine("{");

        builder.Append("    public " + className + "BodySyntax(");
        for (var i = 0; i < fields.Count; i++)
        {
            if (i > 0)
            {
                builder.Append(", ");
            }

            builder.Append(fields[i].CsType + " " + EmitterHelpers.LowerFirst(fields[i].Name));
        }

        builder.AppendLine(")");
        builder.AppendLine("    {");
        foreach (var field in fields)
        {
            builder.AppendLine("        " + field.Name + " = " + EmitterHelpers.LowerFirst(field.Name) + ";");
        }

        builder.AppendLine("    }");

        if (fields.Count > 0)
        {
            builder.AppendLine();
            foreach (var field in fields)
            {
                builder.AppendLine("    public " + field.CsType + " " + field.Name + " { get; }");
            }
        }

        builder.AppendLine();
        builder.AppendLine("    public override SourceLocation Location");
        builder.AppendLine("    {");
        builder.AppendLine("        get");
        builder.AppendLine("        {");
        builder.AppendLine("            var result = SourceLocation.Unknown;");
        foreach (var field in fields)
        {
            var locationCode = EmitterHelpers.GetLocationMergeCode(field);
            foreach (var codeLine in locationCode.Split('\n'))
            {
                builder.Append("            ");
                builder.AppendLine(codeLine);
            }
        }

        builder.AppendLine("            return result;");
        builder.AppendLine("        }");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    public override void WriteTo(Text.SyntaxWriter writer)");
        builder.AppendLine("    {");
        foreach (var field in fields)
        {
            EmitterHelpers.AppendIndentedCode(builder, field.WriteToCode);
        }

        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    public override SyntaxNode Rewrite(SyntaxRewriter rewriter)");
        builder.AppendLine("    {");
        builder.Append("        return new " + metadata.BodyClassName + "(");
        for (var i = 0; i < fields.Count; i++)
        {
            if (i > 0)
            {
                builder.Append(", ");
            }

            builder.Append(GetRewriteExpression(fields[i]));
        }

        builder.AppendLine(");");
        builder.AppendLine("    }");
        builder.AppendLine("}");

        return metadata;
    }

    private static OperationBodySyntaxMetadata ComputeBodySyntaxMetadata(OperationModel operation, AssemblyFormatModel assemblyFormat, string operationClassName)
    {
        var metadata = new OperationBodySyntaxMetadata(operationClassName);
        var usedNames = new HashSet<string>(StringComparer.Ordinal);

        foreach (var element in assemblyFormat.Elements)
        {
            AppendBodySyntaxFields(usedNames, element, operation, metadata);
        }

        return metadata;
    }

    private static void AppendBodySyntaxFields(HashSet<string> usedNames, Element element, OperationModel operation, OperationBodySyntaxMetadata metadata)
    {
        // Implementation moved to helper so OperationBodySyntaxEmitter uses helper methods for readability.
        EmitterHelpers.AppendBodySyntaxFields(usedNames, element, operation, metadata);
    }

    private static string GetRewriteExpression(BodySyntaxField field)
    {
        var name = field.Name;
        var type = field.CsType;

        if (string.Equals(type, "Token", StringComparison.Ordinal) ||
            string.Equals(type, "Token?", StringComparison.Ordinal))
        {
            return "rewriter.VisitToken(" + name + ")";
        }

        if (string.Equals(type, "RawSyntaxText", StringComparison.Ordinal))
        {
            return "rewriter.VisitRawText(" + name + ")";
        }

        if (type.EndsWith("?", StringComparison.Ordinal))
        {
            var innerType = type.Substring(0, type.Length - 1);
            if (innerType.EndsWith("Syntax", StringComparison.Ordinal))
            {
                return name + " != null ? (" + innerType + ")rewriter.Visit(" + name + ") : null";
            }
        }

        if (type.StartsWith("DelimitedSyntaxList<", StringComparison.Ordinal))
        {
            return type.Contains("Token", StringComparison.Ordinal)
                ? "rewriter.VisitDelimitedTokenList(" + name + ")"
                : "rewriter.VisitDelimitedList(" + name + ")";
        }

        if (type.StartsWith("SeparatedSyntaxList<", StringComparison.Ordinal))
        {
            return type.Contains("Token", StringComparison.Ordinal)
                ? "rewriter.VisitSeparatedTokenList(" + name + ")"
                : "rewriter.VisitSeparatedList(" + name + ")";
        }

        if (type.StartsWith("IReadOnlyList<", StringComparison.Ordinal))
        {
            if (type.Contains("Token", StringComparison.Ordinal))
            {
                return "rewriter.VisitTokenList(" + name + ")";
            }

            if (type.Contains("RawSyntaxText", StringComparison.Ordinal))
            {
                return "rewriter.VisitRawTextList(" + name + ")";
            }

            return "rewriter.VisitList(" + name + ")";
        }

        if (type.EndsWith("Syntax", StringComparison.Ordinal))
        {
            return "(" + type + ")rewriter.Visit(" + name + ")";
        }

        return name;
    }
}
