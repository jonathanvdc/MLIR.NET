namespace MLIR.Generators.Emitters.Operation;

using System;
using System.Collections.Generic;
using MLIR.Generators.Emitters;
using MLIR.Generators.Emitters.Common;
using MLIR.ODS.Model;

internal sealed class GeneratedMember
{
    public GeneratedMember(string propertyName, string parameterName, string typeName, string sourceName)
        : this(propertyName, parameterName, typeName, sourceName, null, null, null, null, null, null, null, false)
    {
    }

    public GeneratedMember(
        string propertyName,
        string parameterName,
        string typeName,
        string sourceName,
        string? constraintRecordName,
        AttributeConstraintCodeStrategy? constraintStrategy,
        string? constraintClassName,
        string? attrStorageTypeName = null,
        string? attrConvertFromStorageExpression = null,
        string? attrConstBuilderCallExpression = null,
        string? attrDefaultValueExpression = null,
        bool isVariadic = false)
    {
        PropertyName = propertyName;
        ParameterName = parameterName;
        TypeName = typeName;
        SourceName = sourceName;
        ConstraintRecordName = constraintRecordName;
        ConstraintStrategy = constraintStrategy;
        ConstraintClassName = constraintClassName;
        AttrStorageTypeName = attrStorageTypeName;
        AttrConvertFromStorageExpression = attrConvertFromStorageExpression;
        AttrConstBuilderCallExpression = attrConstBuilderCallExpression;
        AttrDefaultValueExpression = attrDefaultValueExpression;
        IsVariadic = isVariadic;
    }

    public string PropertyName { get; }

    public string ParameterName { get; }

    public string TypeName { get; }

    public string SourceName { get; }

    public string? ConstraintRecordName { get; }

    /// <summary>
    /// Gets the code-generation strategy for the attribute constraint associated with this
    /// member, or <see langword="null"/> when no specialised constraint handling is
    /// available.
    /// </summary>
    public AttributeConstraintCodeStrategy? ConstraintStrategy { get; }

    public string? ConstraintClassName { get; }

    public string? AttrStorageTypeName { get; }

    public string? AttrConvertFromStorageExpression { get; }

    public string? AttrConstBuilderCallExpression { get; }

    public string? AttrDefaultValueExpression { get; }

    /// <summary>
    /// Gets a value indicating whether this member is variadic (zero or more values).
    /// </summary>
    public bool IsVariadic { get; }
}

internal sealed class OperationMemberPlan
{
    public OperationMemberPlan(
        IReadOnlyList<GeneratedMember> regions,
        IReadOnlyList<GeneratedMember> operands,
        IReadOnlyList<GeneratedMember> results,
        IReadOnlyList<GeneratedMember> attributes)
    {
        Regions = regions;
        Operands = operands;
        Results = results;
        Attributes = attributes;
    }

    public IReadOnlyList<GeneratedMember> Regions { get; }
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
            GetRegionMembers(operation, requiredVariables),
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

            members.Add(new GeneratedMember(propertyName, GetParameterName(propertyName), typeName, operand.Name, null, null, null, isVariadic: operand.IsVariadic));
        }

        return members;
    }

    private static IReadOnlyList<GeneratedMember> GetResultMembers(OperationModel operation)
    {
        var members = new List<GeneratedMember>(operation.Results.Count);
        // Use "ResultValue" as the property name only when there is exactly one non-variadic result.
        var singleNonVariadicResult = operation.Results.Count == 1 && !operation.Results[0].IsVariadic;
        for (var i = 0; i < operation.Results.Count; i++)
        {
            var result = operation.Results[i];
            string propertyName;
            string typeName;
            if (result.IsVariadic)
            {
                // Variadic results hold zero or more values; expose them as a read-only list.
                propertyName = DialectGeneratorNaming.ToPascalCase(result.Name);
                typeName = "global::System.Collections.Generic.IReadOnlyList<OperationResult>";
            }
            else
            {
                propertyName = singleNonVariadicResult
                    ? "ResultValue"
                    : DialectGeneratorNaming.ToPascalCase(result.Name);
                typeName = "OperationResult";
            }

            members.Add(new GeneratedMember(propertyName, GetParameterName(propertyName), typeName, result.Name, null, null, null, isVariadic: result.IsVariadic));
        }

        return members;
    }

    private static IReadOnlyList<GeneratedMember> GetAttributeMembers(OperationModel operation, HashSet<string> requiredVariables, DialectSymbolResolver resolver)
    {
        var hasSymbol = HasTrait(operation.Traits, "Symbol");
        var members = new List<GeneratedMember>(operation.Attributes.Count);
        for (var i = 0; i < operation.Attributes.Count; i++)
        {
            var attribute = operation.Attributes[i];
            var attributeName = attribute.Name;
            var propertyName = hasSymbol && string.Equals(attributeName, "sym_name", StringComparison.Ordinal)
                ? "SymbolName"
                : DialectGeneratorNaming.ToPascalCase(attributeName);
            var isRequired = requiredVariables.Contains(attributeName);

            var constraintRecordName = EmitterHelpers.TryGetAttributeConstraint(operation, attributeName);
            var attrModel = !string.IsNullOrEmpty(constraintRecordName)
                ? resolver.TryResolveAttrModel(constraintRecordName!)
                : null;
            // Start with the fallback strategy so that attributes without a recognised
            // constraint kind always produce AttributeValue-typed properties.
            AttributeConstraintCodeStrategy constraintStrategy = FallbackAttributeConstraintCodeStrategy.Instance;
            string? constraintClassName = null;

            if (!string.IsNullOrEmpty(constraintRecordName))
            {
                var nonNullConstraintRecordName = constraintRecordName!;
                constraintStrategy = resolver.TryResolveAttributeConstraintStrategy(nonNullConstraintRecordName);
                constraintClassName = resolver.TryResolveAttributeConstraintClassName(nonNullConstraintRecordName);
            }

            var useAttrModelTyping = ShouldUseAttrModelTyping(attrModel, constraintStrategy);
            var typeName = GetAttributeTypeName(constraintRecordName, useAttrModelTyping ? attrModel : null, constraintStrategy, isRequired, resolver);
            members.Add(new GeneratedMember(
                propertyName,
                GetParameterName(propertyName),
                typeName,
                attributeName,
                constraintRecordName,
                constraintStrategy,
                constraintClassName,
                attrStorageTypeName: useAttrModelTyping ? attrModel?.CsharpStorageType : null,
                attrConvertFromStorageExpression: useAttrModelTyping ? attrModel?.CsharpConvertFromStorage : null,
                attrConstBuilderCallExpression: useAttrModelTyping ? attrModel?.CsharpConstBuilderCall : null,
                attrDefaultValueExpression: useAttrModelTyping ? attrModel?.CsharpDefaultValue : null));
        }

        return members;
    }

    private static bool HasTrait(IReadOnlyList<TraitModel> traits, string recordName)
    {
        for (var i = 0; i < traits.Count; i++)
        {
            var trait = traits[i];
            if (string.Equals(trait.RecordName, recordName, StringComparison.Ordinal))
            {
                return true;
            }

            if (trait is TraitListModel traitList && HasTrait(traitList.Traits, recordName))
            {
                return true;
            }
        }

        return false;
    }

    private static string GetAttributeTypeName(string? constraintRecordName, AttrModel? attrModel, AttributeConstraintCodeStrategy strategy, bool isRequired, DialectSymbolResolver resolver)
    {
        if (!strategy.IsUnit
            && attrModel?.CsharpReturnType is string returnType
            && returnType.Length > 0
            && !string.Equals(returnType, "AttributeValue", StringComparison.Ordinal)
            && !string.Equals(returnType, "global::MLIR.Semantics.AttributeValue", StringComparison.Ordinal))
        {
            if (isRequired || !string.IsNullOrEmpty(attrModel.CsharpDefaultValue))
            {
                return returnType;
            }

            return returnType.EndsWith("?", StringComparison.Ordinal) ? returnType : returnType + "?";
        }

        return strategy.GetOperationPropertyTypeName(constraintRecordName ?? string.Empty, isRequired, resolver);
    }

    private static bool ShouldUseAttrModelTyping(AttrModel? attrModel, AttributeConstraintCodeStrategy strategy)
    {
        if (attrModel is null
            || strategy.IsUnit
            || strategy.IsEnum
            || strategy.IsTypedArray
            || (!strategy.IsPrimitive && !strategy.IsDenseCollection))
        {
            return false;
        }

        var returnType = attrModel.CsharpReturnType;
        var storageType = attrModel.CsharpStorageType;
        if (string.IsNullOrEmpty(returnType)
            || string.Equals(returnType, "AttributeValue", StringComparison.Ordinal)
            || string.Equals(returnType, "global::MLIR.Semantics.AttributeValue", StringComparison.Ordinal))
        {
            return false;
        }

        if (string.Equals(storageType, "global::MLIR.Semantics.SymbolRefAttr", StringComparison.Ordinal))
        {
            return false;
        }

        return true;
    }

    private static IReadOnlyList<GeneratedMember> GetRegionMembers(OperationModel operation, HashSet<string> requiredVariables)
    {
        var members = new List<GeneratedMember>(operation.Regions.Count);
        for (var i = 0; i < operation.Regions.Count; i++)
        {
            var region = operation.Regions[i];
            var propertyName = DialectGeneratorNaming.ToPascalCase(region.Name);
            string typeName;
            if (region.IsVariadic)
            {
                typeName = "global::System.Collections.Generic.IReadOnlyList<Region>";
            }
            else
            {
                typeName = requiredVariables.Contains(region.Name) ? "Region" : "Region?";
            }

            members.Add(new GeneratedMember(propertyName, GetParameterName(propertyName), typeName, region.Name, null, null, null, isVariadic: region.IsVariadic));
        }

        return members;
    }
}
