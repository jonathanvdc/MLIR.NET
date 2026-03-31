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

        EmitterHelpers.AppendXmlDocComment(builder, operation.Summary, operation.Description);
        builder.AppendLine("public sealed class " + className + " : Operation");
        builder.AppendLine("{");
        builder.AppendLine("    private readonly IReadOnlyList<Region> regions;");
        builder.AppendLine("    private readonly IReadOnlyList<NamedAttribute> attributes;");
        builder.AppendLine("    private readonly TypeReference? typeSignatureReference;");
        builder.AppendLine("    private readonly IReadOnlyList<ValueReference> resultValues;");
        builder.AppendLine("    private readonly IReadOnlyList<ValueReference> operandValues;");
        builder.AppendLine("    private readonly IReadOnlyList<BlockReference> successorReferences;");
        builder.AppendLine();
        builder.AppendLine("    public " + className + "(OperationConstructionContext context)");
        builder.AppendLine("        : base(context.Syntax, context.Name, context.Definition)");
        builder.AppendLine("    {");
        builder.AppendLine("        regions = context.Regions;");
        builder.AppendLine("        attributes = context.Attributes;");
        builder.AppendLine("        typeSignatureReference = context.TypeSignatureReference;");
        builder.AppendLine("        resultValues = context.ResultValues;");
        builder.AppendLine("        operandValues = context.OperandValues;");
        builder.AppendLine("        successorReferences = context.SuccessorReferences;");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    public override IReadOnlyList<Region> Regions => regions;");
        builder.AppendLine("    public override IReadOnlyList<NamedAttribute> Attributes => attributes;");
        builder.AppendLine("    public override TypeReference? TypeSignatureReference => typeSignatureReference;");
        builder.AppendLine("    public override IReadOnlyList<ValueReference> ResultValues => resultValues;");
        builder.AppendLine("    public override IReadOnlyList<ValueReference> OperandValues => operandValues;");
        builder.AppendLine("    public override IReadOnlyList<BlockReference> SuccessorReferences => successorReferences;");
        builder.AppendLine();

        for (var i = 0; i < operation.Operands.Count; i++)
        {
            builder.AppendLine(
                "    public ValueReference " + DialectGeneratorNaming.ToPascalCase(operation.Operands[i]) + " => OperandValues[" + i.ToString(CultureInfo.InvariantCulture) + "];");
        }

        for (var i = 0; i < operation.Results.Count; i++)
        {
            var propertyName = operation.Results.Count == 1
                ? "ResultValue"
                : DialectGeneratorNaming.ToPascalCase(operation.Results[i]);
            builder.AppendLine(
                "    public ValueReference " + propertyName + " => ResultValues[" + i.ToString(CultureInfo.InvariantCulture) + "];");
        }

        if (resultReferenceName != null && operation.Results[0] != "result")
        {
            builder.AppendLine(
                "    public ValueReference " + DialectGeneratorNaming.ToPascalCase(operation.Results[0]) + " => " + resultReferenceName + ";");
        }

        builder.AppendLine("}");
    }
}
