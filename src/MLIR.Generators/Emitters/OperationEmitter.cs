namespace MLIR.Generators.Emitters;

using System.Globalization;
using System.Text;
using MLIR.ODS.Model;

internal static class OperationEmitter
{
    public static void Emit(StringBuilder builder, OperationModel operation)
    {
        var className = DialectGeneratorNaming.GetOperationClassName(operation);
        var resultReferenceName = operation.Results.Count == 1 ? "ResultValue" : null;

        static string GetParameterName(string propertyName)
        {
            if (propertyName.Length == 0)
            {
                return propertyName;
            }

            return char.ToLowerInvariant(propertyName[0]) + propertyName.Substring(1);
        }

        static string FormatStringLiteral(string value)
        {
            return "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
        }

        static void AppendDerivedListProperty(
            StringBuilder builder,
            string itemType,
            string propertyName,
            IReadOnlyList<string> items,
            Func<string, string> elementExpression)
        {
            if (items.Count == 0)
            {
                builder.AppendLine("    public override IReadOnlyList<" + itemType + "> " + propertyName + " => global::System.Array.Empty<" + itemType + ">();");
                return;
            }

            builder.Append("    public override IReadOnlyList<" + itemType + "> " + propertyName + " => new " + itemType + "[] { ");
            for (var i = 0; i < items.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append(", ");
                }

                builder.Append(elementExpression(items[i]));
            }

            builder.AppendLine(" };");
        }

        EmitterHelpers.AppendXmlDocComment(builder, operation.Summary, operation.Description);
        builder.AppendLine("public sealed class " + className + " : Operation");
        builder.AppendLine("{");
        builder.AppendLine("    private readonly IReadOnlyList<Region> regions;");
        builder.AppendLine("    private readonly TypeReference? typeSignatureReference;");
        builder.AppendLine("    private readonly IReadOnlyList<BlockReference> successorReferences;");
        builder.AppendLine();

        for (var i = 0; i < operation.Operands.Count; i++)
        {
            builder.AppendLine(
                "    public ValueReference " + DialectGeneratorNaming.ToPascalCase(operation.Operands[i]) + " { get; }");
        }

        for (var i = 0; i < operation.Results.Count; i++)
        {
            var propertyName = operation.Results.Count == 1
                ? "ResultValue"
                : DialectGeneratorNaming.ToPascalCase(operation.Results[i]);
            builder.AppendLine("    public ValueReference " + propertyName + " { get; }");
        }

        if (resultReferenceName != null && operation.Results[0] != "result")
        {
            builder.AppendLine(
                "    public ValueReference " + DialectGeneratorNaming.ToPascalCase(operation.Results[0]) + " => " + resultReferenceName + ";");
        }

        for (var i = 0; i < operation.Attributes.Count; i++)
        {
            builder.AppendLine(
                "    public NamedAttribute " + DialectGeneratorNaming.ToPascalCase(operation.Attributes[i]) + " { get; }");
        }

        if (operation.Operands.Count > 0 || operation.Results.Count > 0 || operation.Attributes.Count > 0)
        {
            builder.AppendLine();
        }

        builder.AppendLine("    public " + className + "(OperationConstructionContext context)");
        builder.AppendLine("        : this(");
        builder.AppendLine("            syntax: context.Syntax,");
        builder.AppendLine("            name: context.Name,");
        builder.AppendLine("            definition: context.Definition,");

        for (var i = 0; i < operation.Operands.Count; i++)
        {
            builder.AppendLine(
                "            " + GetParameterName(DialectGeneratorNaming.ToPascalCase(operation.Operands[i])) + ": context.OperandValues[" + i.ToString(CultureInfo.InvariantCulture) + "],");
        }

        for (var i = 0; i < operation.Results.Count; i++)
        {
            var propertyName = operation.Results.Count == 1
                ? "ResultValue"
                : DialectGeneratorNaming.ToPascalCase(operation.Results[i]);
            builder.AppendLine(
                "            " + GetParameterName(propertyName) + ": context.ResultValues[" + i.ToString(CultureInfo.InvariantCulture) + "],");
        }

        for (var i = 0; i < operation.Attributes.Count; i++)
        {
            var attributeName = operation.Attributes[i];
            builder.AppendLine(
                "            " + GetParameterName(DialectGeneratorNaming.ToPascalCase(attributeName)) + ": global::System.Linq.Enumerable.Single(context.Attributes, static attribute => attribute.Name == " + FormatStringLiteral(attributeName) + "),");
        }

        builder.AppendLine("            typeSignatureReference: context.TypeSignatureReference)");
        builder.AppendLine("    {");
        builder.AppendLine("    }");
        builder.AppendLine();

        builder.AppendLine("    public " + className + "(");
        builder.AppendLine("        OperationSyntax? syntax,");
        builder.AppendLine("        string name,");
        builder.AppendLine("        OperationDefinition definition,");

        for (var i = 0; i < operation.Operands.Count; i++)
        {
            builder.AppendLine(
                "        ValueReference " + GetParameterName(DialectGeneratorNaming.ToPascalCase(operation.Operands[i])) + ",");
        }

        for (var i = 0; i < operation.Results.Count; i++)
        {
            var propertyName = operation.Results.Count == 1
                ? "ResultValue"
                : DialectGeneratorNaming.ToPascalCase(operation.Results[i]);
            builder.AppendLine("        ValueReference " + GetParameterName(propertyName) + ",");
        }

        for (var i = 0; i < operation.Attributes.Count; i++)
        {
            builder.AppendLine(
                "        NamedAttribute " + GetParameterName(DialectGeneratorNaming.ToPascalCase(operation.Attributes[i])) + ",");
        }

        builder.AppendLine("        TypeReference? typeSignatureReference)");
        builder.AppendLine("        : base(syntax, name, definition)");
        builder.AppendLine("    {");
        builder.AppendLine("        this.regions = global::System.Array.Empty<Region>();");
        builder.AppendLine("        this.typeSignatureReference = typeSignatureReference;");
        builder.AppendLine("        this.successorReferences = global::System.Array.Empty<BlockReference>();");

        for (var i = 0; i < operation.Operands.Count; i++)
        {
            var propertyName = DialectGeneratorNaming.ToPascalCase(operation.Operands[i]);
            builder.AppendLine("        " + propertyName + " = " + GetParameterName(propertyName) + ";");
        }

        for (var i = 0; i < operation.Results.Count; i++)
        {
            var propertyName = operation.Results.Count == 1
                ? "ResultValue"
                : DialectGeneratorNaming.ToPascalCase(operation.Results[i]);
            builder.AppendLine("        " + propertyName + " = " + GetParameterName(propertyName) + ";");
        }

        for (var i = 0; i < operation.Attributes.Count; i++)
        {
            var propertyName = DialectGeneratorNaming.ToPascalCase(operation.Attributes[i]);
            builder.AppendLine("        " + propertyName + " = " + GetParameterName(propertyName) + ";");
        }

        builder.AppendLine("    }");
        builder.AppendLine();

        builder.AppendLine("    public " + className + "(");
        builder.AppendLine("        string name,");
        builder.AppendLine("        OperationDefinition definition,");

        for (var i = 0; i < operation.Operands.Count; i++)
        {
            builder.AppendLine(
                "        ValueReference " + GetParameterName(DialectGeneratorNaming.ToPascalCase(operation.Operands[i])) + ",");
        }

        for (var i = 0; i < operation.Results.Count; i++)
        {
            var propertyName = operation.Results.Count == 1
                ? "ResultValue"
                : DialectGeneratorNaming.ToPascalCase(operation.Results[i]);
            builder.AppendLine("        ValueReference " + GetParameterName(propertyName) + ",");
        }

        for (var i = 0; i < operation.Attributes.Count; i++)
        {
            builder.AppendLine(
                "        NamedAttribute " + GetParameterName(DialectGeneratorNaming.ToPascalCase(operation.Attributes[i])) + ",");
        }

        builder.AppendLine("        TypeReference? typeSignatureReference)");
        builder.AppendLine("        : this(");
        builder.AppendLine("            syntax: null,");
        builder.AppendLine("            name: name,");
        builder.AppendLine("            definition: definition,");

        for (var i = 0; i < operation.Operands.Count; i++)
        {
            builder.AppendLine("            " + GetParameterName(DialectGeneratorNaming.ToPascalCase(operation.Operands[i])) + ": " + GetParameterName(DialectGeneratorNaming.ToPascalCase(operation.Operands[i])) + ",");
        }

        for (var i = 0; i < operation.Results.Count; i++)
        {
            var propertyName = operation.Results.Count == 1
                ? "ResultValue"
                : DialectGeneratorNaming.ToPascalCase(operation.Results[i]);
            builder.AppendLine("            " + GetParameterName(propertyName) + ": " + GetParameterName(propertyName) + ",");
        }

        for (var i = 0; i < operation.Attributes.Count; i++)
        {
            builder.AppendLine("            " + GetParameterName(DialectGeneratorNaming.ToPascalCase(operation.Attributes[i])) + ": " + GetParameterName(DialectGeneratorNaming.ToPascalCase(operation.Attributes[i])) + ",");
        }

        builder.AppendLine("            typeSignatureReference: typeSignatureReference)");
        builder.AppendLine("    {");
        builder.AppendLine("    }");
        builder.AppendLine();

        builder.AppendLine("    public override IReadOnlyList<Region> Regions => regions;");
        AppendDerivedListProperty(builder, "NamedAttribute", "Attributes", operation.Attributes, DialectGeneratorNaming.ToPascalCase);
        builder.AppendLine("    public override TypeReference? TypeSignatureReference => typeSignatureReference;");
        AppendDerivedListProperty(builder, "ValueReference", "ResultValues", operation.Results, resultName =>
        {
            return operation.Results.Count == 1
                ? "ResultValue"
                : DialectGeneratorNaming.ToPascalCase(resultName);
        });
        AppendDerivedListProperty(builder, "ValueReference", "OperandValues", operation.Operands, DialectGeneratorNaming.ToPascalCase);
        builder.AppendLine("    public override IReadOnlyList<BlockReference> SuccessorReferences => successorReferences;");
        builder.AppendLine("}");
    }
}
