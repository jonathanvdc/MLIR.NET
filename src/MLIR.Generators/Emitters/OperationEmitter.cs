namespace MLIR.Generators.Emitters;

using System.Globalization;
using System.Text;
using System.Collections.Generic;
using MLIR.ODS.Model;

internal static class OperationEmitter
{
    public sealed class GeneratedMember
    {
        public GeneratedMember(string propertyName, string parameterName, string typeName, string sourceName)
        {
            PropertyName = propertyName;
            ParameterName = parameterName;
            TypeName = typeName;
            SourceName = sourceName;
        }

        public string PropertyName { get; }

        public string ParameterName { get; }

        public string TypeName { get; }

        public string SourceName { get; }
    }

    private static string GetParameterName(string propertyName)
    {
        if (propertyName.Length == 0)
        {
            return propertyName;
        }

        return char.ToLowerInvariant(propertyName[0]) + propertyName.Substring(1);
    }

    private static IReadOnlyList<GeneratedMember> GetOperandMembers(OperationModel operation)
    {
        var members = new List<GeneratedMember>(operation.Operands.Count);
        for (var i = 0; i < operation.Operands.Count; i++)
        {
            var propertyName = DialectGeneratorNaming.ToPascalCase(operation.Operands[i]);
            members.Add(new GeneratedMember(propertyName, GetParameterName(propertyName), "ValueReference", operation.Operands[i]));
        }

        return members;
    }

    private static IReadOnlyList<GeneratedMember> GetResultMembers(OperationModel operation)
    {
        var members = new List<GeneratedMember>(operation.Results.Count);
        for (var i = 0; i < operation.Results.Count; i++)
        {
            var propertyName = operation.Results.Count == 1
                ? "ResultValue"
                : DialectGeneratorNaming.ToPascalCase(operation.Results[i]);
            members.Add(new GeneratedMember(propertyName, GetParameterName(propertyName), "ValueReference", operation.Results[i]));
        }

        return members;
    }

    private static IReadOnlyList<GeneratedMember> GetAttributeMembers(OperationModel operation)
    {
        var members = new List<GeneratedMember>(operation.Attributes.Count);
        for (var i = 0; i < operation.Attributes.Count; i++)
        {
            var propertyName = DialectGeneratorNaming.ToPascalCase(operation.Attributes[i]);
            members.Add(new GeneratedMember(propertyName, GetParameterName(propertyName), "NamedAttribute", operation.Attributes[i]));
        }

        return members;
    }

    private static void AppendAutoProperties(StringBuilder builder, IReadOnlyList<GeneratedMember> members)
    {
        for (var i = 0; i < members.Count; i++)
        {
            var member = members[i];
            builder.AppendLine("    public " + member.TypeName + " " + member.PropertyName + " { get; }");
        }
    }

    private static void AppendDerivedAttributeAccessorProperties(StringBuilder builder, IReadOnlyList<GeneratedMember> attributeMembers)
    {
        for (var i = 0; i < attributeMembers.Count; i++)
        {
            var member = attributeMembers[i];
            builder.AppendLine("    public NamedAttribute " + member.PropertyName + " => Attributes[" + EmitterHelpers.ToCSharpStringLiteral(member.SourceName) + "];");
        }
    }

    private static void AppendConstructorParameters(StringBuilder builder, IReadOnlyList<GeneratedMember> members)
    {
        for (var i = 0; i < members.Count; i++)
        {
            var member = members[i];
            builder.AppendLine("        " + member.TypeName + " " + member.ParameterName + ",");
        }
    }

    private static void AppendAssignments(StringBuilder builder, IReadOnlyList<GeneratedMember> members)
    {
        for (var i = 0; i < members.Count; i++)
        {
            var member = members[i];
            builder.AppendLine("        " + member.PropertyName + " = " + member.ParameterName + ";");
        }
    }

    private static void AppendNamedArguments(
        StringBuilder builder,
        IReadOnlyList<GeneratedMember> members,
        Func<GeneratedMember, string> valueExpression)
    {
        for (var i = 0; i < members.Count; i++)
        {
            var member = members[i];
            builder.AppendLine("            " + member.ParameterName + ": " + valueExpression(member) + ",");
        }
    }

    private static void AppendDerivedListProperty(
        StringBuilder builder,
        string itemType,
        string propertyName,
        IReadOnlyList<GeneratedMember> members)
    {
        if (members.Count == 0)
        {
            builder.AppendLine("    public override IReadOnlyList<" + itemType + "> " + propertyName + " => global::System.Array.Empty<" + itemType + ">();");
            return;
        }

        builder.Append("    public override IReadOnlyList<" + itemType + "> " + propertyName + " => new " + itemType + "[] { ");
        for (var i = 0; i < members.Count; i++)
        {
            if (i > 0)
            {
                builder.Append(", ");
            }

            builder.Append(members[i].PropertyName);
        }

        builder.AppendLine(" };");
    }

    private static void EmitPropertyDeclarations(
        StringBuilder builder,
        IReadOnlyList<GeneratedMember> operandMembers,
        IReadOnlyList<GeneratedMember> resultMembers,
        IReadOnlyList<GeneratedMember> attributeMembers,
        OperationModel operation,
        string? resultReferenceName)
    {
        AppendAutoProperties(builder, operandMembers);
        AppendAutoProperties(builder, resultMembers);

        if (resultReferenceName != null && operation.Results[0] != "result")
        {
            builder.AppendLine(
                "    public ValueReference " + DialectGeneratorNaming.ToPascalCase(operation.Results[0]) + " => " + resultReferenceName + ";");
        }

        builder.AppendLine("    public override NamedAttributeCollection Attributes { get; }");
        AppendDerivedAttributeAccessorProperties(builder, attributeMembers);

        builder.AppendLine();
    }

    private static void EmitContextConstructor(
        StringBuilder builder,
        string className,
        IReadOnlyList<GeneratedMember> operandMembers,
        IReadOnlyList<GeneratedMember> resultMembers,
        IReadOnlyList<GeneratedMember> attributeMembers)
    {
        builder.AppendLine("    public " + className + "(OperationConstructionContext context)");
        builder.AppendLine("        : this(");
        builder.AppendLine("            syntax: context.Syntax,");
        builder.AppendLine("            name: context.Name,");
        builder.AppendLine("            definition: context.Definition,");

        for (var i = 0; i < operandMembers.Count; i++)
        {
            var member = operandMembers[i];
            builder.AppendLine("            " + member.ParameterName + ": context.OperandValues[" + i.ToString(CultureInfo.InvariantCulture) + "],");
        }

        for (var i = 0; i < resultMembers.Count; i++)
        {
            var member = resultMembers[i];
            builder.AppendLine("            " + member.ParameterName + ": context.ResultValues[" + i.ToString(CultureInfo.InvariantCulture) + "],");
        }

        builder.AppendLine("            attributes: context.Attributes,");
        builder.AppendLine("            typeSignatureReference: context.TypeSignatureReference)");
        builder.AppendLine("    {");
        builder.AppendLine("    }");
        builder.AppendLine();
    }

    private static void EmitPrimaryConstructor(
        StringBuilder builder,
        string className,
        IReadOnlyList<GeneratedMember> operandMembers,
        IReadOnlyList<GeneratedMember> resultMembers,
        IReadOnlyList<GeneratedMember> attributeMembers)
    {
        builder.AppendLine("    public " + className + "(");
        builder.AppendLine("        OperationSyntax? syntax,");
        builder.AppendLine("        string name,");
        builder.AppendLine("        OperationDefinition definition,");
        AppendConstructorParameters(builder, operandMembers);
        AppendConstructorParameters(builder, resultMembers);
        builder.AppendLine("        NamedAttributeCollection attributes,");
        builder.AppendLine("        TypeReference? typeSignatureReference)");
        builder.AppendLine("        : base(syntax, name, definition)");
        builder.AppendLine("    {");
        builder.AppendLine("        this.typeSignatureReference = typeSignatureReference;");
        AppendAssignments(builder, operandMembers);
        AppendAssignments(builder, resultMembers);
        builder.AppendLine("        Attributes = attributes;");
        builder.AppendLine("    }");
        builder.AppendLine();
    }

    private static void EmitConvenienceConstructor(
        StringBuilder builder,
        string className,
        IReadOnlyList<GeneratedMember> operandMembers,
        IReadOnlyList<GeneratedMember> resultMembers,
        IReadOnlyList<GeneratedMember> attributeMembers)
    {
        builder.AppendLine("    public " + className + "(");
        builder.AppendLine("        string name,");
        builder.AppendLine("        OperationDefinition definition,");
        AppendConstructorParameters(builder, operandMembers);
        AppendConstructorParameters(builder, resultMembers);
        builder.AppendLine("        NamedAttributeCollection attributes,");
        builder.AppendLine("        TypeReference? typeSignatureReference)");
        builder.AppendLine("        : this(");
        builder.AppendLine("            syntax: null,");
        builder.AppendLine("            name: name,");
        builder.AppendLine("            definition: definition,");
        AppendNamedArguments(builder, operandMembers, static member => member.ParameterName);
        AppendNamedArguments(builder, resultMembers, static member => member.ParameterName);
        builder.AppendLine("            attributes: attributes,");
        builder.AppendLine("            typeSignatureReference: typeSignatureReference)");
        builder.AppendLine("    {");
        builder.AppendLine("    }");
        builder.AppendLine();
    }

    private static void EmitOverrideProperties(
        StringBuilder builder,
        IReadOnlyList<GeneratedMember> operandMembers,
        IReadOnlyList<GeneratedMember> resultMembers,
        IReadOnlyList<GeneratedMember> attributeMembers)
    {
        builder.AppendLine("    public override IReadOnlyList<Region> Regions => global::System.Array.Empty<Region>();");
        builder.AppendLine("    public override TypeReference? TypeSignatureReference => typeSignatureReference;");
        AppendDerivedListProperty(builder, "ValueReference", "ResultValues", resultMembers);
        AppendDerivedListProperty(builder, "ValueReference", "OperandValues", operandMembers);
        builder.AppendLine("    public override IReadOnlyList<BlockReference> SuccessorReferences => global::System.Array.Empty<BlockReference>();");
    }

    public sealed class EmittedOperationMembers
    {
        public EmittedOperationMembers(
            IReadOnlyList<GeneratedMember> operands,
            IReadOnlyList<GeneratedMember> results,
            IReadOnlyList<GeneratedMember> attributes)
        {
            Operands = operands;
            Results = results;
            Attributes = attributes;
        }

        public IReadOnlyList<GeneratedMember> Operands { get; }

        public IReadOnlyList<GeneratedMember> Results { get; }

        public IReadOnlyList<GeneratedMember> Attributes { get; }
    }

    public static EmittedOperationMembers Emit(StringBuilder builder, OperationModel operation)
    {
        var className = DialectGeneratorNaming.GetOperationClassName(operation);
        var resultReferenceName = operation.Results.Count == 1 ? "ResultValue" : null;
        var operandMembers = GetOperandMembers(operation);
        var resultMembers = GetResultMembers(operation);
        var attributeMembers = GetAttributeMembers(operation);

        EmitterHelpers.AppendXmlDocComment(builder, operation.Summary, operation.Description);
        builder.AppendLine("public sealed class " + className + " : Operation");
        builder.AppendLine("{");
        builder.AppendLine("    private readonly TypeReference? typeSignatureReference;");
        builder.AppendLine();

        EmitPropertyDeclarations(builder, operandMembers, resultMembers, attributeMembers, operation, resultReferenceName);
        EmitContextConstructor(builder, className, operandMembers, resultMembers, attributeMembers);
        EmitPrimaryConstructor(builder, className, operandMembers, resultMembers, attributeMembers);
        EmitConvenienceConstructor(builder, className, operandMembers, resultMembers, attributeMembers);
        EmitOverrideProperties(builder, operandMembers, resultMembers, attributeMembers);

        builder.AppendLine("}");
        return new EmittedOperationMembers(operandMembers, resultMembers, attributeMembers);
    }
}
