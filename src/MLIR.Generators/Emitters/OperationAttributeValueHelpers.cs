namespace MLIR.Generators.Emitters;

using System;
using MLIR.ODS.Model;

internal static class OperationAttributeValueHelpers
{
    public static bool IsPrimitiveConstraintKind(AttributeConstraintKind kind)
    {
        return kind is AttributeConstraintKind.IntegerLiteral or AttributeConstraintKind.BooleanLiteral
            or AttributeConstraintKind.StringLiteral or AttributeConstraintKind.FloatingPointLiteral;
    }

    public static bool IsValueTypeConstraintKind(AttributeConstraintKind kind)
    {
        return kind is AttributeConstraintKind.IntegerLiteral or AttributeConstraintKind.BooleanLiteral;
    }

    public static string GetAttributeGetterExpression(GeneratedMember member, string sourceNameLiteral, string localName)
    {
        var isOptional = IsOptionalMember(member);

        if (member.ConstraintKind == AttributeConstraintKind.None)
        {
            if (isOptional)
            {
                return "Attributes.TryGet(" + sourceNameLiteral + ", out var " + localName + ") ? " + localName + " : null";
            }

            return "Attributes[" + sourceNameLiteral + "]";
        }

        if (IsPrimitiveConstraintKind(member.ConstraintKind))
        {
            var valueAccess = GetPrimitiveValueAccessExpression(member, localName, sourceNameLiteral, isOptional);
            if (isOptional)
            {
                return "Attributes.TryGet(" + sourceNameLiteral + ", out var " + localName + ") ? " + valueAccess + " : null";
            }

            return valueAccess;
        }

        var baseTypeName = isOptional ? member.TypeName.Substring(0, member.TypeName.Length - 1) : member.TypeName;
        var castExpr = "(" + baseTypeName + ")";
        if (isOptional)
        {
            return "Attributes.TryGet(" + sourceNameLiteral + ", out var " + localName + ") ? " + castExpr + localName + ".Value : null";
        }

        return castExpr + "Attributes[" + sourceNameLiteral + "].Value";
    }

    public static string GetAttributeSetterExpression(GeneratedMember member, string sourceNameLiteral, string valueExpression)
    {
        return "SetAttribute(" + sourceNameLiteral + ", " + GetNamedAttributeExpression(member, valueExpression) + ")";
    }

    public static string GetNamedAttributeExpression(GeneratedMember member, string valueExpression)
    {
        var sourceName = EmitterHelpers.ToCSharpStringLiteral(member.SourceName);
        var isOptional = IsOptionalMember(member);

        if (member.ConstraintKind == AttributeConstraintKind.UnitAttribute)
        {
            if (string.Equals(member.TypeName, "bool", StringComparison.Ordinal))
            {
                return valueExpression + " ? new NamedAttribute(" + sourceName + ", " + GetUnitAttributeValueExpression() + ") : null";
            }

            return "new NamedAttribute(" + sourceName + ", " + valueExpression + ")";
        }

        if (member.ConstraintKind == AttributeConstraintKind.None)
        {
            return valueExpression;
        }

        if (IsPrimitiveConstraintKind(member.ConstraintKind))
        {
            var constraintClass = member.ConstraintClassName!;
            if (!isOptional)
            {
                return "new NamedAttribute(" + sourceName + ", new " + constraintClass + "(" + valueExpression + "))";
            }

            if (IsValueTypeConstraintKind(member.ConstraintKind))
            {
                return valueExpression + ".HasValue ? new NamedAttribute(" + sourceName + ", new " + constraintClass + "(" + valueExpression + ".Value)) : null";
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
        var castExpr = GetPrimitiveAttributeCastExpression(member.ConstraintKind);
        if (isOptional)
        {
            return castExpr + localName + ".Value)" + GetPrimitiveValueAccess(member.ConstraintKind);
        }

        return castExpr + "Attributes[" + sourceNameLiteral + "].Value)" + GetPrimitiveValueAccess(member.ConstraintKind);
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
}
