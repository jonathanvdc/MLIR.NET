namespace MLIR.Generators.Emitters;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using MLIR.ODS.Model;

internal static class OperationPropertyEmitter
{
    public static void Emit(StringBuilder builder, string className, OperationModel operation, OperationMemberPlan plan)
    {
        EmitOperandAndResultProperties(builder, plan.Operands, plan.Results, operation);
        EmitAttributeProperties(builder, plan.Attributes);
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
                if (isOptional)
                {
                    var localName = EmitterHelpers.LowerFirst(member.PropertyName);
                    builder.AppendLine("    public NamedAttribute? " + member.PropertyName);
                    builder.AppendLine("    {");
                    builder.AppendLine("        get => Attributes.TryGet(" + EmitterHelpers.ToCSharpStringLiteral(member.SourceName) + ", out var " + localName + ") ? " + localName + " : null;");
                    builder.AppendLine("        set => SetAttribute(" + EmitterHelpers.ToCSharpStringLiteral(member.SourceName) + ", value);");
                    builder.AppendLine("    }");
                }
                else
                {
                    builder.AppendLine("    public NamedAttribute " + member.PropertyName);
                    builder.AppendLine("    {");
                    builder.AppendLine("        get => Attributes[" + EmitterHelpers.ToCSharpStringLiteral(member.SourceName) + "];");
                    builder.AppendLine("        set => SetAttribute(" + EmitterHelpers.ToCSharpStringLiteral(member.SourceName) + ", value);");
                    builder.AppendLine("    }");
                }
            }
            else if (IsPrimitiveConstraintKind(member.ConstraintKind))
            {
                var sourceNameLiteral = EmitterHelpers.ToCSharpStringLiteral(member.SourceName);
                var castExpr = GetPrimitiveAttributeCastExpression(member.ConstraintKind);
                var constraintClass = member.ConstraintClassName!;

                builder.AppendLine("    public " + member.TypeName + " " + member.PropertyName);
                builder.AppendLine("    {");

                if (!isOptional)
                {
                    builder.AppendLine("        get => " + castExpr + "Attributes[" + sourceNameLiteral + "].Value)" + GetPrimitiveValueAccess(member.ConstraintKind) + ";");
                    builder.AppendLine("        set => SetAttribute(" + sourceNameLiteral + ", new NamedAttribute(" + sourceNameLiteral + ", new " + constraintClass + "(value)));");
                }
                else if (IsValueTypeConstraintKind(member.ConstraintKind))
                {
                    var localName = EmitterHelpers.LowerFirst(member.PropertyName);
                    builder.AppendLine("        get => Attributes.TryGet(" + sourceNameLiteral + ", out var " + localName + ") ? " + castExpr + localName + ".Value)" + GetPrimitiveValueAccess(member.ConstraintKind) + " : null;");
                    builder.AppendLine("        set => SetAttribute(" + sourceNameLiteral + ", value.HasValue ? new NamedAttribute(" + sourceNameLiteral + ", new " + constraintClass + "(value.Value)) : null);");
                }
                else
                {
                    var localName = EmitterHelpers.LowerFirst(member.PropertyName);
                    builder.AppendLine("        get => Attributes.TryGet(" + sourceNameLiteral + ", out var " + localName + ") ? " + castExpr + localName + ".Value)" + GetPrimitiveValueAccess(member.ConstraintKind) + " : null;");
                    builder.AppendLine("        set => SetAttribute(" + sourceNameLiteral + ", value != null ? new NamedAttribute(" + sourceNameLiteral + ", new " + constraintClass + "(value)) : null);");
                }

                builder.AppendLine("    }");
            }
            else
            {
                var sourceNameLiteral = EmitterHelpers.ToCSharpStringLiteral(member.SourceName);
                var baseTypeName = isOptional ? member.TypeName.Substring(0, member.TypeName.Length - 1) : member.TypeName;
                var castExpr = "(" + baseTypeName + ")";

                builder.AppendLine("    public " + member.TypeName + " " + member.PropertyName);
                builder.AppendLine("    {");

                if (!isOptional)
                {
                    builder.AppendLine("        get => " + castExpr + "Attributes[" + sourceNameLiteral + "].Value;");
                    builder.AppendLine("        set => SetAttribute(" + sourceNameLiteral + ", new NamedAttribute(" + sourceNameLiteral + ", value));");
                }
                else
                {
                    var localName = EmitterHelpers.LowerFirst(member.PropertyName);
                    builder.AppendLine("        get => Attributes.TryGet(" + sourceNameLiteral + ", out var " + localName + ") ? " + castExpr + localName + ".Value : null;");
                    builder.AppendLine("        set => SetAttribute(" + sourceNameLiteral + ", value != null ? new NamedAttribute(" + sourceNameLiteral + ", value) : null);");
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
}
