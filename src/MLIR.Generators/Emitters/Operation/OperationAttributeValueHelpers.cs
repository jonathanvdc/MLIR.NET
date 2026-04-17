namespace MLIR.Generators.Emitters.Operation;

using System;
using MLIR.Generators.Emitters.Common;

internal static class OperationAttributeValueHelpers
{
    public static string GetAttributeGetterExpression(GeneratedMember member, string sourceNameLiteral, string localName)
    {
        var isOptional = IsOptionalMember(member);
        // ConstraintStrategy is always non-null for attribute members.
        var strategy = member.ConstraintStrategy!;
        if (strategy.IsTypedArray && !HasStorageConversionPlan(member))
        {
            var constraintClass = member.ConstraintClassName!;
            if (isOptional)
            {
                return "Attributes.TryGet(" + sourceNameLiteral + ", out var " + localName + ") ? " + constraintClass + ".GetItems(" + localName + ".Value) : null";
            }

            return constraintClass + ".GetItems(Attributes[" + sourceNameLiteral + "].Value)";
        }

        var storagePlan = GetStoragePlan(member);
        var requiredStorage = GetStorageExpression(storagePlan, "Attributes[" + sourceNameLiteral + "].Value");
        var convertedRequired = storagePlan.StorageToPublic.Render(requiredStorage);
        var defaultValue = storagePlan.DefaultValueExpression;
        if (!string.IsNullOrEmpty(defaultValue))
        {
            var optionalStorage = GetStorageExpression(storagePlan, localName + ".Value");
            var convertedOptional = storagePlan.StorageToPublic.Render(optionalStorage);
            return "Attributes.TryGet(" + sourceNameLiteral + ", out var " + localName + ") ? " + convertedOptional + " : " + defaultValue;
        }

        if (isOptional)
        {
            var optionalStorage = GetStorageExpression(storagePlan, localName + ".Value");
            var convertedOptional = storagePlan.StorageToPublic.Render(optionalStorage);
            return "Attributes.TryGet(" + sourceNameLiteral + ", out var " + localName + ") ? " + convertedOptional + " : null";
        }

        return convertedRequired;
    }

    public static string GetAttributeSetterExpression(GeneratedMember member, string sourceNameLiteral, string valueExpression)
    {
        return "SetAttribute(" + sourceNameLiteral + ", " + GetAttributeValueExpression(member, valueExpression) + ")";
    }

    /// <summary>
    /// Returns a C# expression that evaluates to the <c>AttributeValue</c> (or null)
    /// to pass to <c>SetAttribute(string, AttributeValue?)</c> for the given member and value.
    /// </summary>
    public static string GetAttributeValueExpression(GeneratedMember member, string valueExpression)
    {
        var isOptional = IsOptionalMember(member);
        // ConstraintStrategy is always non-null for attribute members.
        var strategy = member.ConstraintStrategy!;

        if (strategy.IsUnit)
        {
            if (string.Equals(member.TypeName, "bool", StringComparison.Ordinal))
            {
                return valueExpression + " ? " + GetUnitAttributeValueExpression() + " : null";
            }

            return valueExpression;
        }

        var storagePlan = GetStoragePlan(member);
        var storageExpression = storagePlan.PublicToStorage.Render(valueExpression);
        if (!isOptional)
        {
            return storageExpression;
        }

        if (storagePlan.OptionalValueKind == OptionalValueKind.NullableValueType)
        {
            var typedStorageExpression = storagePlan.PublicToStorage.Render(valueExpression + ".Value");
            return valueExpression + ".HasValue ? " + typedStorageExpression + " : null";
        }

        if (strategy.IsTypedArray && !HasStorageConversionPlan(member))
        {
            var constraintClass = member.ConstraintClassName!;
            return valueExpression + " != null ? " + constraintClass + ".Create(" + valueExpression + ") : null";
        }

        return valueExpression + " != null ? " + storageExpression + " : null";
    }

    public static string GetNamedAttributeExpression(GeneratedMember member, string valueExpression)
    {
        var sourceName = EmitterHelpers.ToCSharpStringLiteral(member.SourceName);
        var isOptional = IsOptionalMember(member);
        // ConstraintStrategy is always non-null for attribute members.
        var strategy = member.ConstraintStrategy!;

        if (strategy.IsUnit)
        {
            if (string.Equals(member.TypeName, "bool", StringComparison.Ordinal))
            {
                return valueExpression + " ? new NamedAttribute(" + sourceName + ", " + GetUnitAttributeValueExpression() + ") : null";
            }

            return "new NamedAttribute(" + sourceName + ", " + valueExpression + ")";
        }

        var storagePlan = GetStoragePlan(member);
        var storageExpression = storagePlan.PublicToStorage.Render(valueExpression);
        if (!isOptional)
        {
            return "new NamedAttribute(" + sourceName + ", " + storageExpression + ")";
        }

        if (storagePlan.OptionalValueKind == OptionalValueKind.NullableValueType)
        {
            var typedStorageExpression = storagePlan.PublicToStorage.Render(valueExpression + ".Value");
            return valueExpression + ".HasValue ? new NamedAttribute(" + sourceName + ", " + typedStorageExpression + ") : null";
        }

        if (strategy.IsTypedArray && !HasStorageConversionPlan(member))
        {
            var constraintClass = member.ConstraintClassName!;
            return valueExpression + " != null ? new NamedAttribute(" + sourceName + ", " + constraintClass + ".Create(" + valueExpression + ")) : null";
        }

        return valueExpression + " != null ? new NamedAttribute(" + sourceName + ", " + storageExpression + ") : null";
    }

    public static string GetUnitAttributeValueExpression()
    {
        return "new UnknownAttributeValue(new UnitAttributeValueSyntax(TokenFactory.Identifier(\"unit\")), null, null)";
    }

    private static bool IsOptionalMember(GeneratedMember member)
    {
        return member.TypeName.EndsWith("?", System.StringComparison.Ordinal);
    }

    private static AttributeStoragePlan GetStoragePlan(GeneratedMember member)
    {
        return member.AttributeStoragePlan
            ?? throw new InvalidOperationException("Attribute member '" + member.PropertyName + "' has no storage plan.");
    }

    private static bool HasStorageConversionPlan(GeneratedMember member)
    {
        return member.AttributeStoragePlan != null;
    }

    private static string GetStorageExpression(AttributeStoragePlan storagePlan, string valueExpression)
    {
        return IsGenericAttributeValueStorage(storagePlan.StorageTypeName)
            ? valueExpression
            : "((" + storagePlan.StorageTypeName + ")" + valueExpression + ")";
    }

    private static bool IsGenericAttributeValueStorage(string storageTypeName)
    {
        return string.Equals(storageTypeName, "AttributeValue", StringComparison.Ordinal)
            || string.Equals(storageTypeName, "global::MLIR.Semantics.AttributeValue", StringComparison.Ordinal);
    }
}
