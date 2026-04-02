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

    private static IReadOnlyList<GeneratedMember> GetOperandMembers(OperationModel operation, HashSet<string> requiredVariables)
    {
        var members = new List<GeneratedMember>(operation.Operands.Count);
        for (var i = 0; i < operation.Operands.Count; i++)
        {
            var propertyName = DialectGeneratorNaming.ToPascalCase(operation.Operands[i]);
            var typeName = requiredVariables.Contains(operation.Operands[i]) ? "Value" : "Value?";
            members.Add(new GeneratedMember(propertyName, GetParameterName(propertyName), typeName, operation.Operands[i]));
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
            members.Add(new GeneratedMember(propertyName, GetParameterName(propertyName), "OperationResult", operation.Results[i]));
        }

        return members;
    }

    private static IReadOnlyList<GeneratedMember> GetAttributeMembers(OperationModel operation, HashSet<string> requiredVariables)
    {
        var members = new List<GeneratedMember>(operation.Attributes.Count);
        for (var i = 0; i < operation.Attributes.Count; i++)
        {
            var propertyName = DialectGeneratorNaming.ToPascalCase(operation.Attributes[i]);
            var typeName = requiredVariables.Contains(operation.Attributes[i]) ? "NamedAttribute" : "NamedAttribute?";
            members.Add(new GeneratedMember(propertyName, GetParameterName(propertyName), typeName, operation.Attributes[i]));
        }

        return members;
    }

    private static void EmitDefinition(StringBuilder builder, string className, OperationModel operation)
    {
        var requiredVariables = AssemblyFormatAnalyzer.GetRequiredVariables(operation);

        builder.AppendLine("    public static OperationDefinition OperationDefinition { get; } = CreateOperationDefinition();");
        builder.AppendLine("    public override string Name => OperationDefinition.Name;");
        builder.AppendLine("    public override OperationDefinition? Definition => OperationDefinition;");
        builder.AppendLine();
        builder.AppendLine("    private static OperationDefinition CreateOperationDefinition()");
        builder.AppendLine("    {");
        builder.AppendLine("        var operation = new OperationDefinitionBuilder(" + EmitterHelpers.ToCSharpStringLiteral(operation.Name) + ");");

        foreach (var operand in operation.Operands)
        {
            builder.AppendLine("        operation.Operand(" + EmitterHelpers.ToCSharpStringLiteral(operand) + ");");
        }

        foreach (var result in operation.Results)
        {
            builder.AppendLine("        operation.Result(" + EmitterHelpers.ToCSharpStringLiteral(result) + ");");
        }

        foreach (var attribute in operation.Attributes)
        {
            if (requiredVariables.Contains(attribute))
            {
                builder.AppendLine("        operation.RequiredAttribute(" + EmitterHelpers.ToCSharpStringLiteral(attribute) + ");");
            }
            else
            {
                builder.AppendLine("        operation.OptionalAttribute(" + EmitterHelpers.ToCSharpStringLiteral(attribute) + ");");
            }
        }

        builder.AppendLine("        operation.WithFactory(static context => new " + className + "(context));");
        if (operation.HasCustomAssemblyFormat)
        {
            builder.AppendLine("        operation.WithAssemblyFormat(new " + className + "AssemblyFormat());");
        }

        builder.AppendLine("        return operation.Build();");
        builder.AppendLine("    }");
        builder.AppendLine();
    }

    private static void EmitOperandAndResultProperties(
        StringBuilder builder,
        IReadOnlyList<GeneratedMember> operandMembers,
        IReadOnlyList<GeneratedMember> resultMembers,
        OperationModel operation)
    {
        for (var i = 0; i < operandMembers.Count; i++)
        {
            var member = operandMembers[i];
            var suffix = member.TypeName.EndsWith("?", System.StringComparison.Ordinal) ? string.Empty : "!";
            builder.AppendLine("    public " + member.TypeName + " " + member.PropertyName + " => Operands[" + i.ToString(CultureInfo.InvariantCulture) + "].Value" + suffix + ";");
        }

        for (var i = 0; i < resultMembers.Count; i++)
        {
            var member = resultMembers[i];
            builder.AppendLine("    public OperationResult " + member.PropertyName + " => Results[" + i.ToString(CultureInfo.InvariantCulture) + "];");
        }

        if (operation.Results.Count == 1 && operation.Results[0] != "result")
        {
            builder.AppendLine("    public OperationResult " + DialectGeneratorNaming.ToPascalCase(operation.Results[0]) + " => ResultValue;");
        }

        builder.AppendLine();
    }

    private static void EmitAttributeProperties(StringBuilder builder, IReadOnlyList<GeneratedMember> attributeMembers)
    {
        for (var i = 0; i < attributeMembers.Count; i++)
        {
            var member = attributeMembers[i];
            if (member.TypeName == "NamedAttribute?")
            {
                var localName = EmitterHelpers.LowerFirst(member.PropertyName);
                builder.AppendLine(
                    "    public NamedAttribute? " + member.PropertyName +
                    " => Attributes.TryGet(" + EmitterHelpers.ToCSharpStringLiteral(member.SourceName) +
                    ", out var " + localName + ") ? " + localName + " : null;");
            }
            else
            {
                builder.AppendLine(
                    "    public NamedAttribute " + member.PropertyName +
                    " => Attributes[" + EmitterHelpers.ToCSharpStringLiteral(member.SourceName) + "];");
            }
        }

        if (attributeMembers.Count > 0)
        {
            builder.AppendLine();
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

    private static void EmitContextConstructor(
        StringBuilder builder,
        string className,
        IReadOnlyList<GeneratedMember> operandMembers,
        IReadOnlyList<GeneratedMember> resultMembers)
    {
        builder.AppendLine("    public " + className + "(OperationConstructionContext context)");
        builder.AppendLine("        : this(");
        builder.AppendLine("            syntax: context.Syntax,");

        for (var i = 0; i < operandMembers.Count; i++)
        {
            var member = operandMembers[i];
            var suffix = member.TypeName.EndsWith("?", System.StringComparison.Ordinal) ? string.Empty : "!";
            builder.AppendLine("            " + member.ParameterName + ": context.OperandValues[" + i.ToString(CultureInfo.InvariantCulture) + "]" + suffix + ",");
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
        IReadOnlyList<GeneratedMember> resultMembers)
    {
        builder.AppendLine("    public " + className + "(");
        builder.AppendLine("        OperationSyntax? syntax,");
        AppendConstructorParameters(builder, operandMembers);
        AppendConstructorParameters(builder, resultMembers);
        builder.AppendLine("        NamedAttributeCollection attributes,");
        builder.AppendLine("        TypeReference? typeSignatureReference)");
        builder.AppendLine("        : base(");
        builder.AppendLine("            syntax,");
        builder.AppendLine("            global::System.Array.Empty<Region>(),");
        builder.AppendLine("            attributes,");
        builder.AppendLine("            typeSignatureReference,");
        builder.Append("            new OperationResult[] { ");
        for (var i = 0; i < resultMembers.Count; i++)
        {
            if (i > 0)
            {
                builder.Append(", ");
            }

            builder.Append(resultMembers[i].ParameterName);
        }

        builder.AppendLine(" },");
        builder.Append("            new Value?[] { ");
        for (var i = 0; i < operandMembers.Count; i++)
        {
            if (i > 0)
            {
                builder.Append(", ");
            }

            builder.Append(operandMembers[i].ParameterName);
        }

        builder.AppendLine(" },");
        builder.AppendLine("            global::System.Array.Empty<global::MLIR.Semantics.Block?>())");
        builder.AppendLine("    {");
        builder.AppendLine("    }");
        builder.AppendLine();
    }

    private static void EmitConvenienceConstructor(
        StringBuilder builder,
        string className,
        IReadOnlyList<GeneratedMember> operandMembers,
        IReadOnlyList<GeneratedMember> resultMembers)
    {
        builder.AppendLine("    public " + className + "(");
        AppendConstructorParameters(builder, operandMembers);
        AppendConstructorParameters(builder, resultMembers);
        builder.AppendLine("        NamedAttributeCollection attributes,");
        builder.AppendLine("        TypeReference? typeSignatureReference)");
        builder.AppendLine("        : this(");
        builder.AppendLine("            syntax: null,");
        AppendNamedArguments(builder, operandMembers, static member => member.ParameterName);
        AppendNamedArguments(builder, resultMembers, static member => member.ParameterName);
        builder.AppendLine("            attributes: attributes,");
        builder.AppendLine("            typeSignatureReference: typeSignatureReference)");
        builder.AppendLine("    {");
        builder.AppendLine("    }");
        builder.AppendLine();
    }

    private static void EmitPerAttributeConvenienceConstructor(
        StringBuilder builder,
        string className,
        IReadOnlyList<GeneratedMember> operandMembers,
        IReadOnlyList<GeneratedMember> resultMembers,
        IReadOnlyList<GeneratedMember> attributeMembers)
    {
        if (attributeMembers.Count == 0)
        {
            return;
        }

        builder.AppendLine("    public " + className + "(");
        AppendConstructorParameters(builder, operandMembers);
        AppendConstructorParameters(builder, resultMembers);
        AppendConstructorParameters(builder, attributeMembers);
        builder.AppendLine("        TypeReference? typeSignatureReference)");
        builder.AppendLine("        : this(");
        builder.AppendLine("            syntax: null,");
        AppendNamedArguments(builder, operandMembers, static member => member.ParameterName);
        AppendNamedArguments(builder, resultMembers, static member => member.ParameterName);

        var hasOptionalAttributes = false;
        for (var i = 0; i < attributeMembers.Count; i++)
        {
            if (attributeMembers[i].TypeName == "NamedAttribute?")
            {
                hasOptionalAttributes = true;
                break;
            }
        }

        if (!hasOptionalAttributes)
        {
            builder.Append("            attributes: NamedAttributeCollection.Create(");
            for (var i = 0; i < attributeMembers.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append(", ");
                }

                builder.Append(attributeMembers[i].ParameterName);
            }

            builder.AppendLine("),");
        }
        else
        {
            builder.Append("            attributes: new NamedAttributeCollection(new NamedAttribute?[] { ");
            for (var i = 0; i < attributeMembers.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append(", ");
                }

                builder.Append(attributeMembers[i].ParameterName);
            }

            builder.AppendLine(" }.Where(a => a is not null).Select(a => a!)),");
        }

        builder.AppendLine("            typeSignatureReference: typeSignatureReference)");
        builder.AppendLine("    {");
        builder.AppendLine("    }");
        builder.AppendLine();
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
        var requiredVariables = AssemblyFormatAnalyzer.GetRequiredVariables(operation);
        var operandMembers = GetOperandMembers(operation, requiredVariables);
        var resultMembers = GetResultMembers(operation);
        var attributeMembers = GetAttributeMembers(operation, requiredVariables);

        EmitterHelpers.AppendXmlDocComment(builder, operation.Summary, operation.Description);
        builder.AppendLine("public sealed class " + className + " : Operation");
        builder.AppendLine("{");
        EmitDefinition(builder, className, operation);
        EmitOperandAndResultProperties(builder, operandMembers, resultMembers, operation);
        EmitAttributeProperties(builder, attributeMembers);
        EmitContextConstructor(builder, className, operandMembers, resultMembers);
        EmitPrimaryConstructor(builder, className, operandMembers, resultMembers);
        EmitConvenienceConstructor(builder, className, operandMembers, resultMembers);
        EmitPerAttributeConvenienceConstructor(builder, className, operandMembers, resultMembers, attributeMembers);
        builder.AppendLine("}");
        return new EmittedOperationMembers(operandMembers, resultMembers, attributeMembers);
    }
}
