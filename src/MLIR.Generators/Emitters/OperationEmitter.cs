namespace MLIR.Generators.Emitters;

using System;
using System.Globalization;
using System.Text;
using System.Collections.Generic;
using MLIR.ODS.Model;

internal static class OperationEmitter
{
    public sealed class GeneratedMember
    {
        public GeneratedMember(string propertyName, string parameterName, string typeName, string sourceName)
            : this(propertyName, parameterName, typeName, sourceName, AttributeConstraintKind.None, null)
        {
        }

        public GeneratedMember(string propertyName, string parameterName, string typeName, string sourceName, AttributeConstraintKind constraintKind, string? constraintClassName)
        {
            PropertyName = propertyName;
            ParameterName = parameterName;
            TypeName = typeName;
            SourceName = sourceName;
            ConstraintKind = constraintKind;
            ConstraintClassName = constraintClassName;
        }

        public string PropertyName { get; }

        public string ParameterName { get; }

        public string TypeName { get; }

        public string SourceName { get; }

        public AttributeConstraintKind ConstraintKind { get; }

        public string? ConstraintClassName { get; }
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
            var operand = operation.Operands[i];
            var propertyName = DialectGeneratorNaming.ToPascalCase(operand.Name);
            var typeName = requiredVariables.Contains(operand.Name) ? "Value" : "Value?";
            members.Add(new GeneratedMember(propertyName, GetParameterName(propertyName), typeName, operand.Name));
        }

        return members;
    }

    private static IReadOnlyList<GeneratedMember> GetResultMembers(OperationModel operation)
    {
        var members = new List<GeneratedMember>(operation.Results.Count);
        for (var i = 0; i < operation.Results.Count; i++)
        {
            var result = operation.Results[i];
            var propertyName = operation.Results.Count == 1
                ? "ResultValue"
                : DialectGeneratorNaming.ToPascalCase(result.Name);
            members.Add(new GeneratedMember(propertyName, GetParameterName(propertyName), "OperationResult", result.Name));
        }

        return members;
    }

    private static IReadOnlyList<GeneratedMember> GetAttributeMembers(OperationModel operation, HashSet<string> requiredVariables, DialectSymbolResolver resolver)
    {
        var members = new List<GeneratedMember>(operation.Attributes.Count);
        for (var i = 0; i < operation.Attributes.Count; i++)
        {
            var attribute = operation.Attributes[i];
            var attributeName = attribute.Name;
            var propertyName = DialectGeneratorNaming.ToPascalCase(attributeName);
            var isRequired = requiredVariables.Contains(attributeName);

            var constraintRecordName = EmitterHelpers.TryGetAttributeConstraint(operation, attributeName);
            var constraintKind = AttributeConstraintKind.None;
            string? constraintClassName = null;

            if (!string.IsNullOrEmpty(constraintRecordName))
            {
                constraintKind = resolver.TryResolveAttributeConstraintKind(constraintRecordName!);
                if (constraintKind != AttributeConstraintKind.None)
                {
                    constraintClassName = resolver.TryResolveAttributeConstraintClassName(constraintRecordName!);
                    if (constraintClassName == null)
                    {
                        constraintKind = AttributeConstraintKind.None;
                    }
                }
            }

            var typeName = GetAttributeTypeName(constraintKind, isRequired);
            members.Add(new GeneratedMember(propertyName, GetParameterName(propertyName), typeName, attributeName, constraintKind, constraintClassName));
        }

        return members;
    }

    private static string GetAttributeTypeName(AttributeConstraintKind kind, bool isRequired)
    {
        var baseType = kind switch
        {
            AttributeConstraintKind.IntegerLiteral => "BigInteger",
            AttributeConstraintKind.BooleanLiteral => "bool",
            AttributeConstraintKind.StringLiteral => "string",
            AttributeConstraintKind.FloatingPointLiteral => "string",
            AttributeConstraintKind.DenseArrayAttribute => "ArrayAttributeValue",
            AttributeConstraintKind.ElementsAttribute => "ElementsAttributeValue",
            AttributeConstraintKind.DictionaryAttribute => "DictionaryAttributeValue",
            AttributeConstraintKind.TypeAttribute => "TypeAttributeValue",
            AttributeConstraintKind.UnitAttribute => "UnitAttributeValue",
            AttributeConstraintKind.OpaqueAttribute => "OpaqueAttributeValue",
            _ => null,
        };

        if (baseType == null)
        {
            return isRequired ? "NamedAttribute" : "NamedAttribute?";
        }

        return isRequired ? baseType : baseType + "?";
    }

    private static bool IsPrimitiveConstraintKind(AttributeConstraintKind kind)
    {
        return kind is AttributeConstraintKind.IntegerLiteral or AttributeConstraintKind.BooleanLiteral
            or AttributeConstraintKind.StringLiteral or AttributeConstraintKind.FloatingPointLiteral;
    }

    private static bool IsValueTypeConstraintKind(AttributeConstraintKind kind)
    {
        return kind is AttributeConstraintKind.IntegerLiteral or AttributeConstraintKind.BooleanLiteral;
    }

    private static void EmitDefinition(StringBuilder builder, string className, OperationModel operation, DialectSymbolResolver resolver)
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
            builder.AppendLine("        operation.Operand(" + EmitterHelpers.ToCSharpStringLiteral(operand.Name) + ");");
        }

        foreach (var result in operation.Results)
        {
            builder.AppendLine("        operation.Result(" + EmitterHelpers.ToCSharpStringLiteral(result.Name) + ");");
        }

        foreach (var attribute in operation.Attributes)
        {
            var expectedConstraint = EmitterHelpers.TryGetAttributeConstraint(operation, attribute.Name);
            var expectedConstraintExpr = !string.IsNullOrEmpty(expectedConstraint)
                ? resolver.TryResolveAttributeConstraintDefinitionExpression(expectedConstraint!)
                : null;
            var constraintSuffix = !string.IsNullOrEmpty(expectedConstraintExpr)
                ? ", " + expectedConstraintExpr
                : string.Empty;
            if (requiredVariables.Contains(attribute.Name))
            {
                builder.AppendLine("        operation.RequiredAttribute(" + EmitterHelpers.ToCSharpStringLiteral(attribute.Name) + constraintSuffix + ");");
            }
            else
            {
                builder.AppendLine("        operation.OptionalAttribute(" + EmitterHelpers.ToCSharpStringLiteral(attribute.Name) + constraintSuffix + ");");
            }
        }

        builder.AppendLine("        operation.WithFactory(static context => new " + className + "(context));");
        if (operation.AssemblyFormat != null)
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
            builder.AppendLine("    public " + member.TypeName + " " + member.PropertyName);
            builder.AppendLine("    {");
            builder.AppendLine("        get => Operands[" + i.ToString(CultureInfo.InvariantCulture) + "].Value" + suffix + ";");
            builder.AppendLine("        set => SetOperand(" + i.ToString(CultureInfo.InvariantCulture) + ", value);");
            builder.AppendLine("    }");
        }

        for (var i = 0; i < resultMembers.Count; i++)
        {
            var member = resultMembers[i];
            builder.AppendLine("    public OperationResult " + member.PropertyName + " => Results[" + i.ToString(CultureInfo.InvariantCulture) + "];");
        }

        if (operation.Results.Count == 1 && operation.Results[0].Name != "result")
        {
            builder.AppendLine("    public OperationResult " + DialectGeneratorNaming.ToPascalCase(operation.Results[0].Name) + " => ResultValue;");
        }

        builder.AppendLine();
    }

    private static void EmitAttributeProperties(StringBuilder builder, IReadOnlyList<GeneratedMember> attributeMembers)
    {
        for (var i = 0; i < attributeMembers.Count; i++)
        {
            var member = attributeMembers[i];
            var isOptional = member.TypeName.EndsWith("?", StringComparison.Ordinal);

            if (member.ConstraintKind == AttributeConstraintKind.None)
            {
                // Legacy NamedAttribute behavior
                if (isOptional)
                {
                    var localName = EmitterHelpers.LowerFirst(member.PropertyName);
                    builder.AppendLine(
                        "    public NamedAttribute? " + member.PropertyName);
                    builder.AppendLine("    {");
                    builder.AppendLine(
                        "        get => Attributes.TryGet(" + EmitterHelpers.ToCSharpStringLiteral(member.SourceName) +
                        ", out var " + localName + ") ? " + localName + " : null;");
                    builder.AppendLine(
                        "        set => SetAttribute(" + EmitterHelpers.ToCSharpStringLiteral(member.SourceName) + ", value);");
                    builder.AppendLine("    }");
                }
                else
                {
                    builder.AppendLine(
                        "    public NamedAttribute " + member.PropertyName);
                    builder.AppendLine("    {");
                    builder.AppendLine(
                        "        get => Attributes[" + EmitterHelpers.ToCSharpStringLiteral(member.SourceName) + "];");
                    builder.AppendLine(
                        "        set => SetAttribute(" + EmitterHelpers.ToCSharpStringLiteral(member.SourceName) + ", value);");
                    builder.AppendLine("    }");
                }
            }
            else if (IsPrimitiveConstraintKind(member.ConstraintKind))
            {
                var sourceNameLiteral = EmitterHelpers.ToCSharpStringLiteral(member.SourceName);
                var castExpr = GetPrimitiveAttributeCastExpression(member.ConstraintKind);
                var constraintClass = member.ConstraintClassName!;

                builder.AppendLine(
                    "    public " + member.TypeName + " " + member.PropertyName);
                builder.AppendLine("    {");

                if (!isOptional)
                {
                    builder.AppendLine(
                        "        get => " + castExpr + "Attributes[" + sourceNameLiteral + "].Value)" + GetPrimitiveValueAccess(member.ConstraintKind) + ";");
                    builder.AppendLine(
                        "        set => SetAttribute(" + sourceNameLiteral + ", new NamedAttribute(" + sourceNameLiteral + ", new " + constraintClass + "(value)));");
                }
                else if (IsValueTypeConstraintKind(member.ConstraintKind))
                {
                    var localName = EmitterHelpers.LowerFirst(member.PropertyName);
                    builder.AppendLine(
                        "        get => Attributes.TryGet(" + sourceNameLiteral + ", out var " + localName + ") ? " +
                        castExpr + localName + ".Value)" + GetPrimitiveValueAccess(member.ConstraintKind) + " : null;");
                    builder.AppendLine(
                        "        set => SetAttribute(" + sourceNameLiteral + ", value.HasValue ? new NamedAttribute(" + sourceNameLiteral + ", new " + constraintClass + "(value.Value)) : null);");
                }
                else
                {
                    // Nullable reference type (string?)
                    var localName = EmitterHelpers.LowerFirst(member.PropertyName);
                    builder.AppendLine(
                        "        get => Attributes.TryGet(" + sourceNameLiteral + ", out var " + localName + ") ? " +
                        castExpr + localName + ".Value)" + GetPrimitiveValueAccess(member.ConstraintKind) + " : null;");
                    builder.AppendLine(
                        "        set => SetAttribute(" + sourceNameLiteral + ", value != null ? new NamedAttribute(" + sourceNameLiteral + ", new " + constraintClass + "(value)) : null);");
                }

                builder.AppendLine("    }");
            }
            else
            {
                // AttributeValue subclass property
                var sourceNameLiteral = EmitterHelpers.ToCSharpStringLiteral(member.SourceName);
                var baseTypeName = isOptional ? member.TypeName.Substring(0, member.TypeName.Length - 1) : member.TypeName;
                var castExpr = "(" + baseTypeName + ")";

                builder.AppendLine(
                    "    public " + member.TypeName + " " + member.PropertyName);
                builder.AppendLine("    {");

                if (!isOptional)
                {
                    builder.AppendLine(
                        "        get => " + castExpr + "Attributes[" + sourceNameLiteral + "].Value;");
                    builder.AppendLine(
                        "        set => SetAttribute(" + sourceNameLiteral + ", new NamedAttribute(" + sourceNameLiteral + ", value));");
                }
                else
                {
                    var localName = EmitterHelpers.LowerFirst(member.PropertyName);
                    builder.AppendLine(
                        "        get => Attributes.TryGet(" + sourceNameLiteral + ", out var " + localName + ") ? " + castExpr + localName + ".Value : null;");
                    builder.AppendLine(
                        "        set => SetAttribute(" + sourceNameLiteral + ", value != null ? new NamedAttribute(" + sourceNameLiteral + ", value) : null);");
                }

                builder.AppendLine("    }");
            }
        }

        if (attributeMembers.Count > 0)
        {
            builder.AppendLine();
        }
    }

    private static string GetPrimitiveAttributeCastExpression(AttributeConstraintKind kind)
    {
        return kind switch
        {
            AttributeConstraintKind.IntegerLiteral => "((IntegerAttributeValue)",
            AttributeConstraintKind.BooleanLiteral => "((BooleanAttributeValue)",
            AttributeConstraintKind.StringLiteral => "((StringAttributeValue)",
            AttributeConstraintKind.FloatingPointLiteral => "((FloatingPointAttributeValue)",
            _ => "((AttributeValue)",
        };
    }

    private static string GetPrimitiveValueAccess(AttributeConstraintKind kind)
    {
        return kind switch
        {
            AttributeConstraintKind.FloatingPointLiteral => ".LiteralText",
            _ => ".Value",
        };
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
            if (attributeMembers[i].TypeName.EndsWith("?", StringComparison.Ordinal))
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

                builder.Append(GetAttributeToNamedAttributeExpression(attributeMembers[i]));
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

                builder.Append(GetAttributeToNamedAttributeExpression(attributeMembers[i]));
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

    private static string GetAttributeToNamedAttributeExpression(GeneratedMember member)
    {
        var sourceName = EmitterHelpers.ToCSharpStringLiteral(member.SourceName);
        var paramName = member.ParameterName;
        var isOptional = member.TypeName.EndsWith("?", StringComparison.Ordinal);

        if (member.ConstraintKind == AttributeConstraintKind.None)
        {
            return paramName;
        }

        if (IsPrimitiveConstraintKind(member.ConstraintKind))
        {
            var constraintClass = member.ConstraintClassName!;
            if (!isOptional)
            {
                return "new NamedAttribute(" + sourceName + ", new " + constraintClass + "(" + paramName + "))";
            }
            else if (IsValueTypeConstraintKind(member.ConstraintKind))
            {
                return paramName + ".HasValue ? new NamedAttribute(" + sourceName + ", new " + constraintClass + "(" + paramName + ".Value)) : null";
            }
            else
            {
                return paramName + " != null ? new NamedAttribute(" + sourceName + ", new " + constraintClass + "(" + paramName + ")) : null";
            }
        }

        if (!isOptional)
        {
            return "new NamedAttribute(" + sourceName + ", " + paramName + ")";
        }

        return paramName + " != null ? new NamedAttribute(" + sourceName + ", " + paramName + ") : null";
    }

    public static EmittedOperationMembers Emit(StringBuilder builder, OperationModel operation, DialectSymbolResolver resolver)
    {
        var className = DialectGeneratorNaming.GetOperationClassName(operation);
        var requiredVariables = AssemblyFormatAnalyzer.GetRequiredVariables(operation);
        var operandMembers = GetOperandMembers(operation, requiredVariables);
        var resultMembers = GetResultMembers(operation);
        var attributeMembers = GetAttributeMembers(operation, requiredVariables, resolver);

        EmitterHelpers.AppendXmlDocComment(builder, operation.Summary, operation.Description);
        builder.AppendLine("public sealed class " + className + " : Operation");
        builder.AppendLine("{");
        EmitDefinition(builder, className, operation, resolver);
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
