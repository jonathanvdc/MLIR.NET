namespace MLIR.Generators.Emitters;

using System;
using System.Collections.Generic;
using System.Text;
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
        builder.AppendLine("    public override void WriteTo(Text.SyntaxWriter writer, int indentLevel)");
        builder.AppendLine("    {");
        foreach (var field in fields)
        {
            EmitterHelpers.AppendIndentedCode(builder, field.WriteToCode);
        }

        builder.AppendLine("    }");

        builder.AppendLine();
        EmitRewriteChildren(builder, metadata);

        builder.AppendLine("}");

        return metadata;
    }

    private static void EmitRewriteChildren(StringBuilder builder, OperationBodySyntaxMetadata metadata)
    {
        var fields = metadata.Fields;

        // Build a mapping from field name to component kind.
        var componentKindByField = new System.Collections.Generic.Dictionary<string, BodyComponentKind>(
            System.StringComparer.Ordinal);
        foreach (var comp in metadata.ComponentFields)
        {
            componentKindByField[comp.FieldName] = comp.Kind;
        }

        // Collect fields that can be rewritten (i.e., hold non-token children).
        var rewritableFields = new System.Collections.Generic.List<BodySyntaxField>();
        foreach (var field in fields)
        {
            if (componentKindByField.TryGetValue(field.Name, out var kind) && IsRewritableKind(kind))
            {
                rewritableFields.Add(field);
            }
        }

        builder.AppendLine("    public override OperationBodySyntax RewriteChildren(SyntaxRewriter rewriter)");
        builder.AppendLine("    {");

        if (rewritableFields.Count == 0)
        {
            builder.AppendLine("        return this;");
            builder.AppendLine("    }");
            return;
        }

        // Emit local variables for each rewritable field.
        foreach (var field in rewritableFields)
        {
            componentKindByField.TryGetValue(field.Name, out var kind);
            var varName = "new" + field.Name;
            var rewriteExpr = GetRewriteExpression(field, kind);
            builder.AppendLine("        var " + varName + " = " + rewriteExpr + ";");
        }

        // Emit identity check.
        builder.Append("        if (");
        for (int i = 0; i < rewritableFields.Count; i++)
        {
            if (i > 0)
            {
                builder.Append(" && ");
            }

            var field = rewritableFields[i];
            builder.Append("ReferenceEquals(new" + field.Name + ", " + field.Name + ")");
        }

        builder.AppendLine(")");
        builder.AppendLine("            return this;");

        // Emit constructor call passing rewritten values for rewritable fields and original for others.
        var rewritableSet = new System.Collections.Generic.HashSet<string>(
            System.StringComparer.Ordinal);
        foreach (var f in rewritableFields)
        {
            rewritableSet.Add(f.Name);
        }

        builder.Append("        return new " + metadata.BodyClassName + "(");
        for (int i = 0; i < fields.Count; i++)
        {
            if (i > 0)
            {
                builder.Append(", ");
            }

            var fieldName = fields[i].Name;
            builder.Append(rewritableSet.Contains(fieldName) ? "new" + fieldName : fieldName);
        }

        builder.AppendLine(");");
        builder.AppendLine("    }");
    }

    private static bool IsRewritableKind(BodyComponentKind kind)
    {
        return kind switch
        {
            BodyComponentKind.AttrDict => true,
            BodyComponentKind.AttrDictWithKeyword => true,
            BodyComponentKind.PropDict => true,
            BodyComponentKind.Regions => true,
            BodyComponentKind.Type => true,
            BodyComponentKind.Attribute => true,
            _ => false,
        };
    }

    private static string GetRewriteExpression(BodySyntaxField field, BodyComponentKind kind)
    {
        bool nullable = field.CsType.EndsWith("?", System.StringComparison.Ordinal);

        return kind switch
        {
            BodyComponentKind.AttrDict or BodyComponentKind.AttrDictWithKeyword or BodyComponentKind.PropDict =>
                "rewriter.VisitNamedAttributeList(" + field.Name + ")",
            BodyComponentKind.Regions =>
                "rewriter.VisitRegionList(" + field.Name + ")",
            BodyComponentKind.Type when nullable =>
                field.Name + " != null ? rewriter.VisitTypeSyntax(" + field.Name + ") : null",
            BodyComponentKind.Type =>
                "rewriter.VisitTypeSyntax(" + field.Name + ")",
            BodyComponentKind.Attribute when nullable =>
                field.Name + " != null ? rewriter.VisitAttributeValue(" + field.Name + ") : null",
            BodyComponentKind.Attribute =>
                "rewriter.VisitAttributeValue(" + field.Name + ")",
            _ => field.Name,
        };
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
