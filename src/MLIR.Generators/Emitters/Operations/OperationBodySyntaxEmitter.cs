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
        builder.AppendLine("    public override void WriteTo(Text.SyntaxWriter writer)");
        builder.AppendLine("    {");
        foreach (var field in fields)
        {
            EmitterHelpers.AppendIndentedCode(builder, field.WriteToCode);
        }

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
}
