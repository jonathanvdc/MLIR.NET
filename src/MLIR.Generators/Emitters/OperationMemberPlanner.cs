namespace MLIR.Generators.Emitters;

using System;
using System.Collections.Generic;
using MLIR.ODS.Model;

internal sealed class GeneratedMember
{
    public GeneratedMember(string propertyName, string parameterName, string typeName, string sourceName)
        : this(propertyName, parameterName, typeName, sourceName, AttributeConstraintKind.None, null, false)
    {
    }

    public GeneratedMember(string propertyName, string parameterName, string typeName, string sourceName, AttributeConstraintKind constraintKind, string? constraintClassName, bool usesEnumWrapper)
    {
        PropertyName = propertyName;
        ParameterName = parameterName;
        TypeName = typeName;
        SourceName = sourceName;
        ConstraintKind = constraintKind;
        ConstraintClassName = constraintClassName;
        UsesEnumWrapper = usesEnumWrapper;
    }

    public string PropertyName { get; }

    public string ParameterName { get; }

    public string TypeName { get; }

    public string SourceName { get; }

    public AttributeConstraintKind ConstraintKind { get; }

    public string? ConstraintClassName { get; }

    public bool UsesEnumWrapper { get; }
}

internal sealed class OperationMemberPlan
{
    public OperationMemberPlan(
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

internal static class OperationMemberPlanner
{
    public static OperationMemberPlan Plan(OperationModel operation, DialectSymbolResolver resolver)
    {
        var requiredVariables = AssemblyFormatAnalyzer.GetRequiredVariables(operation);
        return new OperationMemberPlan(
            GetOperandMembers(operation, requiredVariables),
            GetResultMembers(operation),
            GetAttributeMembers(operation, requiredVariables, resolver));
    }

    private static string GetParameterName(string propertyName)
    {
        return EmitterHelpers.LowerFirst(propertyName);
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

            var enumTypeName = !string.IsNullOrEmpty(constraintRecordName)
                ? resolver.TryResolveEnumTypeName(constraintRecordName!)
                : null;
            var usesEnumWrapper = constraintKind == AttributeConstraintKind.EnumAttribute
                && constraintClassName != null
                && enumTypeName != null;

            var typeName = GetAttributeTypeName(constraintKind, isRequired, constraintRecordName, enumTypeName);
            members.Add(new GeneratedMember(propertyName, GetParameterName(propertyName), typeName, attributeName, constraintKind, constraintClassName, usesEnumWrapper));
        }

        return members;
    }

    private static string GetAttributeTypeName(AttributeConstraintKind kind, bool isRequired, string? constraintRecordName, string? enumTypeName)
    {
        if (kind == AttributeConstraintKind.UnitAttribute)
        {
            return isRequired ? "UnitAttributeValue" : "bool";
        }

        if (kind == AttributeConstraintKind.EnumAttribute && !string.IsNullOrEmpty(enumTypeName))
        {
            return isRequired ? enumTypeName! : enumTypeName! + "?";
        }

        var baseType = kind switch
        {
            AttributeConstraintKind.IntegerLiteral => "BigInteger",
            AttributeConstraintKind.BooleanLiteral => "bool",
            AttributeConstraintKind.StringLiteral => "string",
            AttributeConstraintKind.FloatingPointLiteral => constraintRecordName switch
            {
                "F32Attr" => "float",
                "F64Attr" => "double",
                _ => "string",
            },
            AttributeConstraintKind.DenseBooleanArrayAttribute => "IReadOnlyList<bool>",
            AttributeConstraintKind.DenseIntegerArrayAttribute => "IReadOnlyList<BigInteger>",
            AttributeConstraintKind.DenseF32ArrayAttribute => "IReadOnlyList<float>",
            AttributeConstraintKind.DenseF64ArrayAttribute => "IReadOnlyList<double>",
            AttributeConstraintKind.ElementsAttribute => "ElementsAttributeValue",
            AttributeConstraintKind.DictionaryAttribute => "DictionaryAttributeValue",
            AttributeConstraintKind.TypeAttribute => "TypeAttributeValue",
            AttributeConstraintKind.OpaqueAttribute => "OpaqueAttributeValue",
            _ => null,
        };

        if (baseType == null)
        {
            return isRequired ? "NamedAttribute" : "NamedAttribute?";
        }

        return isRequired ? baseType : baseType + "?";
    }
}
