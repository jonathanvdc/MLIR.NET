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

        if (strategy.IsPrimitive)
        {
            var valueAccess = GetPrimitiveValueAccessExpression(member, localName, sourceNameLiteral, isOptional);
            if (isOptional)
            {
                return "Attributes.TryGet(" + sourceNameLiteral + ", out var " + localName + ") ? " + valueAccess + " : null";
            }

            return valueAccess;
        }

        if (strategy.IsDenseCollection || strategy.IsTypedArray)
        {
            var denseCollectionCastExpr = "((" + member.ConstraintClassName + ")";
            if (isOptional)
            {
                return "Attributes.TryGet(" + sourceNameLiteral + ", out var " + localName + ") ? " + denseCollectionCastExpr + localName + ".Value).Items : null";
            }

            return denseCollectionCastExpr + "Attributes[" + sourceNameLiteral + "].Value).Items";
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

        if (strategy.IsDenseCollection || strategy.IsTypedArray)
        {
            var constraintClass = member.ConstraintClassName!;
            if (!isOptional)
            {
                return "new " + constraintClass + "(" + valueExpression + ")";
            }

            return valueExpression + " != null ? new " + constraintClass + "(" + valueExpression + ") : null";
        }

        // Generic AttributeValue: pass through directly (already nullable if optional).
        return valueExpression;
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

        if (strategy.IsDenseCollection || strategy.IsTypedArray)
        {
            var constraintClass = member.ConstraintClassName!;
            if (!isOptional)
            {
                return "new NamedAttribute(" + sourceName + ", new " + constraintClass + "(" + valueExpression + "))";
            }

            return valueExpression + " != null ? new NamedAttribute(" + sourceName + ", new " + constraintClass + "(" + valueExpression + ")) : null";
        }

        if (!isOptional)
        {
            return "new NamedAttribute(" + sourceName + ", " + valueExpression + ")";
        }

        return valueExpression + " != null ? new NamedAttribute(" + sourceName + ", " + valueExpression + ") : null";
    }

    public static string GetUnitAttributeValueExpression()
    {
        return "new UnknownAttributeValue(new UnitAttributeValueSyntax(new SyntaxToken(\"unit\")), null, null, SourceLocation.Unknown)";
    }

    private static bool IsOptionalMember(GeneratedMember member)
    {
        return member.TypeName.EndsWith("?", System.StringComparison.Ordinal);
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
        return string.Equals(typeName.TrimEnd('?'), "bool", StringComparison.Ordinal)
            || string.Equals(typeName.TrimEnd('?'), "BigInteger", StringComparison.Ordinal)
            || string.Equals(typeName.TrimEnd('?'), "global::MLIR.Numerics.ApInt", StringComparison.Ordinal)
            || string.Equals(typeName.TrimEnd('?'), "ApInt", StringComparison.Ordinal)
            || string.Equals(typeName.TrimEnd('?'), "float", StringComparison.Ordinal)
            || string.Equals(typeName.TrimEnd('?'), "double", StringComparison.Ordinal);
    }
}
