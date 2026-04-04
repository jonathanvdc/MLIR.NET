namespace MLIR.Generators.Emitters;

using System;
using System.Collections.Generic;
using MLIR.ODS.Model;

internal sealed class GeneratedMember
{
    public GeneratedMember(string propertyName, string parameterName, string typeName, string sourceName)
        : this(propertyName, parameterName, typeName, sourceName, null, AttributeConstraintKind.None, null, false, false)
    {
    }

    public GeneratedMember(
        string propertyName,
        string parameterName,
        string typeName,
        string sourceName,
        string? constraintRecordName,
        AttributeConstraintKind constraintKind,
        string? constraintClassName,
        bool usesEnumWrapper,
        bool isVariadic = false)
    {
        PropertyName = propertyName;
        ParameterName = parameterName;
        TypeName = typeName;
        SourceName = sourceName;
        ConstraintRecordName = constraintRecordName;
        ConstraintKind = constraintKind;
        ConstraintClassName = constraintClassName;
        UsesEnumWrapper = usesEnumWrapper;
        IsVariadic = isVariadic;
    }

    public string PropertyName { get; }

    public string ParameterName { get; }

    public string TypeName { get; }

    public string SourceName { get; }

    public string? ConstraintRecordName { get; }

    public AttributeConstraintKind ConstraintKind { get; }

    public string? ConstraintClassName { get; }

    public bool UsesEnumWrapper { get; }

    /// <summary>
    /// Gets a value indicating whether this member is variadic (zero or more values).
    /// </summary>
    public bool IsVariadic { get; }
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
        var requiredVariables = operation.AssemblyFormat != null
            ? AssemblyFormatAnalyzer.GetRequiredVariables(operation)
            : new HashSet<string>(StringComparer.Ordinal);
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
            string typeName;
            if (operand.IsVariadic)
            {
                // Variadic operands hold zero or more values; the list is always present.
                typeName = "global::System.Collections.Generic.IReadOnlyList<Value>";
            }
            else
            {
                typeName = requiredVariables.Contains(operand.Name) ? "Value" : "Value?";
            }

            members.Add(new GeneratedMember(propertyName, GetParameterName(propertyName), typeName, operand.Name, null, AttributeConstraintKind.None, null, false, operand.IsVariadic));
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
                var nonNullConstraintRecordName = constraintRecordName!;
                constraintKind = resolver.TryResolveAttributeConstraintKind(nonNullConstraintRecordName);
                if (constraintKind != AttributeConstraintKind.None)
                {
                    constraintClassName = resolver.TryResolveAttributeConstraintClassName(nonNullConstraintRecordName);
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

            var typeName = GetAttributeTypeName(constraintRecordName, isRequired, resolver);
            members.Add(new GeneratedMember(propertyName, GetParameterName(propertyName), typeName, attributeName, constraintRecordName, constraintKind, constraintClassName, usesEnumWrapper));
        }

        return members;
    }

    private static string GetAttributeTypeName(string? constraintRecordName, bool isRequired, DialectSymbolResolver resolver)
    {
        if (string.IsNullOrEmpty(constraintRecordName))
        {
            return isRequired ? "NamedAttribute" : "NamedAttribute?";
        }

        var nonNullConstraintRecordName = constraintRecordName!;
        var kind = resolver.TryResolveAttributeConstraintKind(nonNullConstraintRecordName);
        if (kind == AttributeConstraintKind.UnitAttribute)
        {
            return isRequired ? "UnitAttributeValue" : "bool";
        }

        if (kind == AttributeConstraintKind.TypeAttribute)
        {
            return isRequired ? "TypeAttributeValue" : "TypeAttributeValue?";
        }

        if (kind == AttributeConstraintKind.DictionaryAttribute)
        {
            return isRequired ? "DictionaryAttributeValue" : "DictionaryAttributeValue?";
        }

        if (kind == AttributeConstraintKind.ElementsAttribute)
        {
            return isRequired ? "ElementsAttributeValue" : "ElementsAttributeValue?";
        }

        if (kind == AttributeConstraintKind.OpaqueAttribute)
        {
            return isRequired ? "OpaqueAttributeValue" : "OpaqueAttributeValue?";
        }

        if (kind == AttributeConstraintKind.TypedArrayAttribute)
        {
            var typedArrayType = AttributeTypeResolver.GetAttributeValueTypeName(constraintRecordName, resolver);
            return typedArrayType != null
                ? (isRequired ? typedArrayType : typedArrayType + "?")
                : (isRequired ? "NamedAttribute" : "NamedAttribute?");
        }

        var baseType = AttributeTypeResolver.GetAttributeValueTypeName(constraintRecordName, resolver);
        if (baseType == null)
        {
            return isRequired ? "NamedAttribute" : "NamedAttribute?";
        }

        return isRequired ? baseType : baseType + "?";
    }
}
