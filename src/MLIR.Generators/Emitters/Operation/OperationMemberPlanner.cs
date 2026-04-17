namespace MLIR.Generators.Emitters.Operation;

using System;
using System.Collections.Generic;
using MLIR.Generators.Emitters;
using MLIR.Generators.Emitters.Common;
using MLIR.ODS.Model;

internal sealed class GeneratedMember
{
    public GeneratedMember(
        string propertyName,
        string parameterName,
        string typeName,
        string sourceName,
        string? constraintRecordName,
        AttributeConstraintCodeStrategy? constraintStrategy,
        string? constraintClassName,
        AttributeStoragePlan? attributeStoragePlan = null,
        bool isVariadic = false)
    {
        PropertyName = propertyName;
        ParameterName = parameterName;
        TypeName = typeName;
        SourceName = sourceName;
        ConstraintRecordName = constraintRecordName;
        ConstraintStrategy = constraintStrategy;
        ConstraintClassName = constraintClassName;
        AttributeStoragePlan = attributeStoragePlan;
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

    /// <summary>
    /// Gets the storage conversion plan for this attribute member, or <see langword="null"/>
    /// for non-attribute members.
    /// </summary>
    public AttributeStoragePlan? AttributeStoragePlan { get; }

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
        IReadOnlyList<GeneratedMember> attributes,
        IReadOnlyList<AttributePropertyPlan> attributeProperties)
    {
        Regions = regions;
        Operands = operands;
        Results = results;
        Attributes = attributes;
        AttributeProperties = attributeProperties;
    }

    public IReadOnlyList<GeneratedMember> Regions { get; }
    public IReadOnlyList<GeneratedMember> Operands { get; }

    public IReadOnlyList<GeneratedMember> Results { get; }

    public IReadOnlyList<GeneratedMember> Attributes { get; }

    public IReadOnlyList<AttributePropertyPlan> AttributeProperties { get; }
}

internal sealed class AttributePropertyPlan
{
    public AttributePropertyPlan(
        string propertyName,
        string publicType,
        string getterExpression,
        string setterExpression)
    {
        PropertyName = propertyName;
        PublicType = publicType;
        GetterExpression = getterExpression;
        SetterExpression = setterExpression;
    }

    public string PropertyName { get; }

    public string PublicType { get; }

    public string GetterExpression { get; }

    public string SetterExpression { get; }
}

internal static class AttributePropertyPlanner
{
    public static IReadOnlyList<AttributePropertyPlan> Plan(IReadOnlyList<GeneratedMember> attributeMembers, OperationModel operation)
    {
        var hasSymbol = HasTrait(operation.Traits, "Symbol");
        var plans = new List<AttributePropertyPlan>(attributeMembers.Count);

        for (var i = 0; i < attributeMembers.Count; i++)
        {
            var plan = TryCreateAttributePropertyPlan(attributeMembers[i], hasSymbol);
            if (plan is not null)
            {
                plans.Add(plan);
            }
        }

        return plans;
    }

    private static AttributePropertyPlan? TryCreateAttributePropertyPlan(GeneratedMember member, bool hasSymbol)
    {
        if (hasSymbol && string.Equals(member.SourceName, "sym_name", StringComparison.Ordinal))
        {
            // Symbol-trait ops expose sym_name via the dedicated SymbolName property.
            // Skip the generic attribute property to avoid emitting both SymbolName and SymName.
            return null;
        }

        // ConstraintStrategy is always non-null for attribute members: the planner
        // sets it to at least FallbackAttributeConstraintCodeStrategy.Instance.
        var strategy = member.ConstraintStrategy!;
        var sourceNameLiteral = EmitterHelpers.ToCSharpStringLiteral(member.SourceName);
        var localName = EmitterHelpers.LowerFirst(member.PropertyName);

        if (strategy.IsUnit)
        {
            if (!string.Equals(member.TypeName, "bool", StringComparison.Ordinal))
            {
                return null;
            }

            return new AttributePropertyPlan(
                member.PropertyName,
                member.TypeName,
                "Attributes.Contains(" + sourceNameLiteral + ")",
                "SetAttribute(" + sourceNameLiteral + ", value ? " + OperationAttributeValueHelpers.GetUnitAttributeValueExpression() + " : null)");
        }

        return new AttributePropertyPlan(
            member.PropertyName,
            member.TypeName,
            OperationAttributeValueHelpers.GetAttributeGetterExpression(member, sourceNameLiteral, localName),
            OperationAttributeValueHelpers.GetAttributeSetterExpression(member, sourceNameLiteral, "value"));
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
}

internal static class OperationMemberPlanner
{
    public static OperationMemberPlan Plan(OperationModel operation, DialectSymbolResolver resolver)
    {
        var requiredVariables = operation.AssemblyFormat != null
            ? AssemblyFormatAnalyzer.GetRequiredVariables(operation)
            : new HashSet<string>(StringComparer.Ordinal);
        var regionMembers = GetRegionMembers(operation, requiredVariables);
        var operandMembers = GetOperandMembers(operation, requiredVariables);
        var resultMembers = GetResultMembers(operation);
        var attributeMembers = GetAttributeMembers(operation, requiredVariables, resolver);
        var attributeProperties = AttributePropertyPlanner.Plan(attributeMembers, operation);
        return new OperationMemberPlan(
            regionMembers,
            operandMembers,
            resultMembers,
            attributeMembers,
            attributeProperties);
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

            var typeName = constraintStrategy.GetOperationPropertyTypeName(isRequired);
            members.Add(new GeneratedMember(
                propertyName,
                GetParameterName(propertyName),
                typeName,
                attributeName,
                constraintRecordName,
                constraintStrategy,
                constraintClassName,
                attributeStoragePlan: constraintStrategy.CreateStoragePlan()));
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
