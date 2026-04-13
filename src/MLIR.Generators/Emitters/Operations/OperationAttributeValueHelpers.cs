namespace MLIR.Generators.Emitters.Operation;

using System;
using MLIR.Generators.Emitters.Common;

internal static class OperationAttributeValueHelpers
{
    public static string GetAttributeGetterExpression(GeneratedMember member, string sourceNameLiteral, string localName)
    {
        var isOptional = IsOptionalMember(member);
        var attrModelGetterExpression = TryGetAttrModelGetterExpression(member, sourceNameLiteral, localName, isOptional);
        if (attrModelGetterExpression != null)
        {
            return attrModelGetterExpression;
        }

        // ConstraintStrategy is always non-null for attribute members.
        var strategy = member.ConstraintStrategy!;

        if (strategy.IsPrimitive)
        {
            var valueAccess = GetPrimitiveValueAccessExpression(member, localName, sourceNameLiteral, isOptional);
            if (isOptional)
            {
                return "Attributes.TryGet(" + sourceNameLiteral + ", out var " + localName + ") ? " + valueAccess + " : null";
            }

            return valueAccess;
        }

        if (strategy.IsDenseCollection)
        {
            var denseCollectionCastExpr = "((" + member.ConstraintClassName + ")";
            if (isOptional)
            {
                return "Attributes.TryGet(" + sourceNameLiteral + ", out var " + localName + ") ? " + denseCollectionCastExpr + localName + ".Value).Items : null";
            }

            return denseCollectionCastExpr + "Attributes[" + sourceNameLiteral + "].Value).Items";
        }

        if (strategy.IsTypedArray)
        {
            var constraintClass = member.ConstraintClassName!;
            if (isOptional)
            {
                return "Attributes.TryGet(" + sourceNameLiteral + ", out var " + localName + ") ? " + constraintClass + ".GetItems(" + localName + ".Value) : null";
            }

            return constraintClass + ".GetItems(Attributes[" + sourceNameLiteral + "].Value)";
        }

        var baseTypeName = isOptional ? member.TypeName.Substring(0, member.TypeName.Length - 1) : member.TypeName;
        var genericCastExpr = "(" + baseTypeName + ")";
        if (isOptional)
        {
            return "Attributes.TryGet(" + sourceNameLiteral + ", out var " + localName + ") ? " + genericCastExpr + localName + ".Value : null";
        }

        return genericCastExpr + "Attributes[" + sourceNameLiteral + "].Value";
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
        var attrModelStorageExpression = TryGetAttrModelStorageValueExpression(member, valueExpression);
        if (attrModelStorageExpression != null)
        {
            if (!isOptional)
            {
                return attrModelStorageExpression;
            }

            if (IsPrimitiveValueType(member.TypeName) || member.ConstraintStrategy!.IsEnum)
            {
                var typedStorageExpression = TryGetAttrModelStorageValueExpression(member, valueExpression + ".Value");
                return valueExpression + ".HasValue ? " + typedStorageExpression + " : null";
            }

            return valueExpression + " != null ? " + attrModelStorageExpression + " : null";
        }

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

        if (strategy.IsPrimitive)
        {
            var constraintClass = member.ConstraintClassName!;
            if (!isOptional)
            {
                return "new " + constraintClass + "(" + valueExpression + ")";
            }

            if (strategy.IsEnum || IsPrimitiveValueType(member.TypeName))
            {
                return valueExpression + ".HasValue ? new " + constraintClass + "(" + valueExpression + ".Value) : null";
            }

            return valueExpression + " != null ? new " + constraintClass + "(" + valueExpression + ") : null";
        }

        if (strategy.IsDenseCollection)
        {
            var constraintClass = member.ConstraintClassName!;
            if (!isOptional)
            {
                return "new " + constraintClass + "(" + valueExpression + ")";
            }

            return valueExpression + " != null ? new " + constraintClass + "(" + valueExpression + ") : null";
        }

        if (strategy.IsTypedArray)
        {
            var constraintClass = member.ConstraintClassName!;
            if (!isOptional)
            {
                return constraintClass + ".Create(" + valueExpression + ")";
            }

            return valueExpression + " != null ? " + constraintClass + ".Create(" + valueExpression + ") : null";
        }

        // Generic AttributeValue: pass through directly (already nullable if optional).
        return valueExpression;
    }

    public static string GetNamedAttributeExpression(GeneratedMember member, string valueExpression)
    {
        var sourceName = EmitterHelpers.ToCSharpStringLiteral(member.SourceName);
        var isOptional = IsOptionalMember(member);
        var attrModelStorageExpression = TryGetAttrModelStorageValueExpression(member, valueExpression);
        if (attrModelStorageExpression != null)
        {
            if (!isOptional)
            {
                return "new NamedAttribute(" + sourceName + ", " + attrModelStorageExpression + ")";
            }

            if (IsPrimitiveValueType(member.TypeName) || member.ConstraintStrategy!.IsEnum)
            {
                var typedStorageExpression = TryGetAttrModelStorageValueExpression(member, valueExpression + ".Value");
                return valueExpression + ".HasValue ? new NamedAttribute(" + sourceName + ", " + typedStorageExpression + ") : null";
            }

            return valueExpression + " != null ? new NamedAttribute(" + sourceName + ", " + attrModelStorageExpression + ") : null";
        }

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

        if (strategy.IsPrimitive)
        {
            var constraintClass = member.ConstraintClassName!;
            if (!isOptional)
            {
                return "new NamedAttribute(" + sourceName + ", new " + constraintClass + "(" + valueExpression + "))";
            }

            if (strategy.IsEnum)
            {
                return valueExpression + ".HasValue ? new NamedAttribute(" + sourceName + ", new " + constraintClass + "(" + valueExpression + ".Value)) : null";
            }

            if (IsPrimitiveValueType(member.TypeName))
            {
                return valueExpression + ".HasValue ? new NamedAttribute(" + sourceName + ", new " + constraintClass + "(" + valueExpression + ".Value)) : null";
            }

            return valueExpression + " != null ? new NamedAttribute(" + sourceName + ", new " + constraintClass + "(" + valueExpression + ")) : null";
        }

        if (strategy.IsDenseCollection)
        {
            var constraintClass = member.ConstraintClassName!;
            if (!isOptional)
            {
                return "new NamedAttribute(" + sourceName + ", new " + constraintClass + "(" + valueExpression + "))";
            }

            return valueExpression + " != null ? new NamedAttribute(" + sourceName + ", new " + constraintClass + "(" + valueExpression + ")) : null";
        }

        if (strategy.IsTypedArray)
        {
            var constraintClass = member.ConstraintClassName!;
            if (!isOptional)
            {
                return "new NamedAttribute(" + sourceName + ", " + constraintClass + ".Create(" + valueExpression + "))";
            }

            return valueExpression + " != null ? new NamedAttribute(" + sourceName + ", " + constraintClass + ".Create(" + valueExpression + ")) : null";
        }

        if (!isOptional)
        {
            return "new NamedAttribute(" + sourceName + ", " + valueExpression + ")";
        }

        return valueExpression + " != null ? new NamedAttribute(" + sourceName + ", " + valueExpression + ") : null";
    }

    public static string GetUnitAttributeValueExpression()
    {
        return "new UnknownAttributeValue(new UnitAttributeValueSyntax(TokenFactory.Identifier(\"unit\")), null, null, SourceLocation.Unknown)";
    }

    private static bool IsOptionalMember(GeneratedMember member)
    {
        return member.TypeName.EndsWith("?", System.StringComparison.Ordinal);
    }

    private static string? TryGetAttrModelGetterExpression(GeneratedMember member, string sourceNameLiteral, string localName, bool isOptional)
    {
        var storageTypeName = member.AttrStorageTypeName;
        var convertExpression = member.AttrConvertFromStorageExpression;
        if (string.IsNullOrEmpty(storageTypeName) || string.IsNullOrEmpty(convertExpression))
        {
            return null;
        }

        var castPrefix = "((" + storageTypeName + ")";
        var convertedRequired = ApplyAttrModelStorageConversion(convertExpression!, castPrefix + "Attributes[" + sourceNameLiteral + "].Value)");
        var defaultValue = member.AttrDefaultValueExpression;
        if (!string.IsNullOrEmpty(defaultValue))
        {
            var convertedOptionalWithDefault = ApplyAttrModelStorageConversion(convertExpression!, castPrefix + localName + ".Value)");
            return "Attributes.TryGet(" + sourceNameLiteral + ", out var " + localName + ") ? " + convertedOptionalWithDefault + " : " + defaultValue;
        }

        if (!isOptional)
        {
            return convertedRequired;
        }

        var convertedOptional = ApplyAttrModelStorageConversion(convertExpression!, castPrefix + localName + ".Value)");
        return "Attributes.TryGet(" + sourceNameLiteral + ", out var " + localName + ") ? " + convertedOptional + " : null";
    }

    private static string? TryGetAttrModelStorageValueExpression(GeneratedMember member, string valueExpression)
    {
        if (!string.IsNullOrEmpty(member.AttrConstBuilderCallExpression))
        {
            return member.AttrConstBuilderCallExpression!.Replace("$0", valueExpression);
        }

        var storageTypeName = member.AttrStorageTypeName;
        if (string.IsNullOrEmpty(storageTypeName))
        {
            return null;
        }

        return "new " + storageTypeName + "(" + valueExpression + ")";
    }

    private static string ApplyAttrModelStorageConversion(string conversionExpression, string storageExpression)
    {
        return conversionExpression.Replace("$_self", storageExpression);
    }

    private static string GetPrimitiveValueAccessExpression(GeneratedMember member, string localName, string sourceNameLiteral, bool isOptional)
    {
        var castExpr = "((" + member.ConstraintClassName + ")";
        if (isOptional)
        {
            return castExpr + localName + ".Value)" + GetPrimitiveValueAccess(member.ConstraintStrategy!, member.TypeName);
        }

        return castExpr + "Attributes[" + sourceNameLiteral + "].Value)" + GetPrimitiveValueAccess(member.ConstraintStrategy!, member.TypeName);
    }

    private static string GetPrimitiveValueAccess(AttributeConstraintCodeStrategy strategy, string typeName)
    {
        return strategy.GetPrimitiveValueAccess(typeName);
    }

    private static bool IsPrimitiveValueType(string typeName)
    {
        var trimmedTypeName = typeName.TrimEnd('?');
        return string.Equals(trimmedTypeName, "bool", StringComparison.Ordinal)
            || string.Equals(trimmedTypeName, "byte", StringComparison.Ordinal)
            || string.Equals(trimmedTypeName, "sbyte", StringComparison.Ordinal)
            || string.Equals(trimmedTypeName, "short", StringComparison.Ordinal)
            || string.Equals(trimmedTypeName, "ushort", StringComparison.Ordinal)
            || string.Equals(trimmedTypeName, "int", StringComparison.Ordinal)
            || string.Equals(trimmedTypeName, "uint", StringComparison.Ordinal)
            || string.Equals(trimmedTypeName, "long", StringComparison.Ordinal)
            || string.Equals(trimmedTypeName, "ulong", StringComparison.Ordinal)
            || string.Equals(trimmedTypeName, "BigInteger", StringComparison.Ordinal)
            || string.Equals(trimmedTypeName, "global::MLIR.Numerics.ApInt", StringComparison.Ordinal)
            || string.Equals(trimmedTypeName, "ApInt", StringComparison.Ordinal)
            || string.Equals(trimmedTypeName, "global::MLIR.Numerics.ApFloat", StringComparison.Ordinal)
            || string.Equals(trimmedTypeName, "ApFloat", StringComparison.Ordinal)
            || string.Equals(trimmedTypeName, "float", StringComparison.Ordinal)
            || string.Equals(trimmedTypeName, "double", StringComparison.Ordinal);
    }
}
