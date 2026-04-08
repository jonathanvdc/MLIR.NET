namespace MLIR.Generators.Emitters.Operation;

using System;
using System.Collections.Generic;
using MLIR.Generators.Emitters;
using MLIR.Generators.Emitters.Common;
using MLIR.ODS.Model;

internal sealed class GeneratedMember
{
    public GeneratedMember(string propertyName, string parameterName, string typeName, string sourceName)
        : this(propertyName, parameterName, typeName, sourceName, null, null, null, false)
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
        bool isVariadic = false)
    {
        PropertyName = propertyName;
        ParameterName = parameterName;
        TypeName = typeName;
        SourceName = sourceName;
        ConstraintRecordName = constraintRecordName;
        ConstraintStrategy = constraintStrategy;
        ConstraintClassName = constraintClassName;
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

            members.Add(new GeneratedMember(propertyName, GetParameterName(propertyName), typeName, operand.Name, null, null, null, operand.IsVariadic));
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

            members.Add(new GeneratedMember(propertyName, GetParameterName(propertyName), typeName, result.Name, null, null, null, result.IsVariadic));
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
            // Start with the fallback strategy so that attributes without a recognised
            // constraint kind always produce AttributeValue-typed properties.
            AttributeConstraintCodeStrategy constraintStrategy = FallbackAttributeConstraintCodeStrategy.Instance;
            string? constraintClassName = null;

            if (!string.IsNullOrEmpty(constraintRecordName))
            {
                var nonNullConstraintRecordName = constraintRecordName!;
                // Only upgrade from the fallback when a generated class exists for the
                // constraint, because the emitter needs the class name to produce casts.
                var resolvedClassName = resolver.TryResolveAttributeConstraintClassName(nonNullConstraintRecordName);
                if (resolvedClassName != null)
                {
                    constraintStrategy = resolver.TryResolveAttributeConstraintStrategy(nonNullConstraintRecordName);
                    constraintClassName = resolvedClassName;
                }
            }

            var typeName = GetAttributeTypeName(constraintRecordName, constraintStrategy, isRequired, resolver);
            members.Add(new GeneratedMember(propertyName, GetParameterName(propertyName), typeName, attributeName, constraintRecordName, constraintStrategy, constraintClassName));
        }

        return members;
    }

    private static string GetAttributeTypeName(string? constraintRecordName, AttributeConstraintCodeStrategy strategy, bool isRequired, DialectSymbolResolver resolver)
    {
        return strategy.GetOperationPropertyTypeName(constraintRecordName ?? string.Empty, isRequired, resolver);
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

            members.Add(new GeneratedMember(propertyName, GetParameterName(propertyName), typeName, region.Name, null, null, null, region.IsVariadic));
        }

        return members;
    }
}
