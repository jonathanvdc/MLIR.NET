namespace MLIR.Generators.Emitters;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using MLIR.ODS.Model;

internal static class OperationPropertyEmitter
{
    public static void Emit(StringBuilder builder, string className, OperationModel operation, OperationMemberPlan plan)
    {
        EmitRegionProperties(builder, plan.Regions);
        EmitOperandAndResultProperties(builder, plan.Operands, plan.Results, operation);
        EmitAttributeProperties(builder, plan.Attributes);
    }

    // Base-class member names in Operation that an operand or result property might shadow.
    private static readonly System.Collections.Generic.HashSet<string> BaseClassMemberNames =
        new(System.StringComparer.Ordinal) { "Operands", "Results", "Attributes", "Regions", "Successors" };

    private static string MemberModifier(string propertyName) =>
        BaseClassMemberNames.Contains(propertyName) ? "new " : string.Empty;

    private static void EmitOperandAndResultProperties(
        StringBuilder builder,
        IReadOnlyList<GeneratedMember> operandMembers,
        IReadOnlyList<GeneratedMember> resultMembers,
        OperationModel operation)
    {
        var slotIndex = 0;
        for (var i = 0; i < operandMembers.Count; i++)
        {
            var member = operandMembers[i];
            if (member.IsVariadic)
            {
                // Emit a read-only list property that returns all operands from the variadic slot onward.
                // Use base.Operands to avoid shadowing issues.
                builder.AppendLine("    public " + MemberModifier(member.PropertyName) + member.TypeName + " " + member.PropertyName);
                builder.AppendLine("    {");
                builder.AppendLine("        get => base.Operands.Skip(" + slotIndex.ToString(CultureInfo.InvariantCulture) + ").Select(static o => o.Value!).ToList();");
                builder.AppendLine("        set { var start = " + slotIndex.ToString(CultureInfo.InvariantCulture) + "; for (var _i = 0; _i < value.Count; _i++) SetOperand(start + _i, value[_i]); }");
                builder.AppendLine("    }");
                // A variadic consumes all remaining slots; stop indexing.
                slotIndex = -1;
            }
            else
            {
                var suffix = member.TypeName.EndsWith("?", System.StringComparison.Ordinal) ? string.Empty : "!";
                builder.AppendLine("    public " + MemberModifier(member.PropertyName) + member.TypeName + " " + member.PropertyName);
                builder.AppendLine("    {");
                // Use base.Operands to guard against a generated property that shadows the inherited
                // Operands list when an operand happens to be named "Operands".
                builder.AppendLine("        get => base.Operands[" + slotIndex.ToString(CultureInfo.InvariantCulture) + "].Value" + suffix + ";");
                builder.AppendLine("        set => SetOperand(" + slotIndex.ToString(CultureInfo.InvariantCulture) + ", value);");
                builder.AppendLine("    }");
                slotIndex++;
            }
        }

        var resultSlotIndex = 0;
        for (var i = 0; i < resultMembers.Count; i++)
        {
            var member = resultMembers[i];
            if (member.IsVariadic)
            {
                // Variadic results: expose all results from the current slot onward as a list.
                // Use base.Results to guard against a generated property that shadows the inherited
                // Results list when a result happens to be named "Results".
                builder.AppendLine("    public " + MemberModifier(member.PropertyName) + member.TypeName + " " + member.PropertyName);
                builder.AppendLine("    {");
                builder.AppendLine("        get => base.Results.Skip(" + resultSlotIndex.ToString(CultureInfo.InvariantCulture) + ").ToList();");
                builder.AppendLine("    }");
                // A variadic result consumes all remaining slots; stop indexing.
                resultSlotIndex = -1;
            }
            else
            {
                // Use base.Results to guard against a generated property that shadows the inherited
                // Results list when a result happens to be named "Results".
                builder.AppendLine("    public " + MemberModifier(member.PropertyName) + "OperationResult " + member.PropertyName + " => base.Results[" + resultSlotIndex.ToString(CultureInfo.InvariantCulture) + "];");
                resultSlotIndex++;
            }
        }

        // Emit a convenience alias only for a single non-variadic result with a non-default name.
        if (operation.Results.Count == 1 && !operation.Results[0].IsVariadic && operation.Results[0].Name != "result")
        {
            var aliasName = DialectGeneratorNaming.ToPascalCase(operation.Results[0].Name);
            builder.AppendLine("    public " + MemberModifier(aliasName) + "OperationResult " + aliasName + " => ResultValue;");
        }

        builder.AppendLine();
    }

    private static void EmitRegionProperties(StringBuilder builder, IReadOnlyList<GeneratedMember> regionMembers)
    {
        var slotIndex = 0;
        for (var i = 0; i < regionMembers.Count; i++)
        {
            var member = regionMembers[i];
            if (member.IsVariadic)
            {
                builder.AppendLine("    public " + MemberModifier(member.PropertyName) + member.TypeName + " " + member.PropertyName);
                builder.AppendLine("    {");
                builder.AppendLine("        get => base.Regions.Skip(" + slotIndex.ToString(System.Globalization.CultureInfo.InvariantCulture) + ").ToList();");
                builder.AppendLine("    }");
                slotIndex = -1;
            }
            else
            {
                builder.AppendLine("    public " + MemberModifier(member.PropertyName) + member.TypeName + " " + member.PropertyName);
                builder.AppendLine("    {");
                builder.AppendLine("        get => base.Regions[" + slotIndex.ToString(System.Globalization.CultureInfo.InvariantCulture) + "];");
                builder.AppendLine("    }");
                slotIndex++;
            }
        }

        if (regionMembers.Count > 0)
        {
            builder.AppendLine();
        }
    }

    private static void EmitAttributeProperties(StringBuilder builder, IReadOnlyList<GeneratedMember> attributeMembers)
    {
        for (var i = 0; i < attributeMembers.Count; i++)
        {
            var member = attributeMembers[i];
            var isOptional = member.TypeName.EndsWith("?", StringComparison.Ordinal);

            if (member.ConstraintKind == AttributeConstraintKind.UnitAttribute)
            {
                if (!string.Equals(member.TypeName, "bool", StringComparison.Ordinal))
                {
                    continue;
                }

                var sourceNameLiteral = EmitterHelpers.ToCSharpStringLiteral(member.SourceName);
                builder.AppendLine("    public bool " + member.PropertyName);
                builder.AppendLine("    {");
                builder.AppendLine("        get => Attributes.Contains(" + sourceNameLiteral + ");");
                builder.AppendLine("        set => SetAttribute(" + sourceNameLiteral + ", value ? new NamedAttribute(" + sourceNameLiteral + ", " + OperationAttributeValueHelpers.GetUnitAttributeValueExpression() + ") : null);");
                builder.AppendLine("    }");
                continue;
            }

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
            else if (OperationAttributeValueHelpers.IsPrimitiveConstraintKind(member.ConstraintKind))
            {
                var sourceNameLiteral = EmitterHelpers.ToCSharpStringLiteral(member.SourceName);
                var localName = EmitterHelpers.LowerFirst(member.PropertyName);
                builder.AppendLine("    public " + member.TypeName + " " + member.PropertyName);
                builder.AppendLine("    {");
                builder.AppendLine("        get => " + OperationAttributeValueHelpers.GetAttributeGetterExpression(member, sourceNameLiteral, localName) + ";");
                builder.AppendLine("        set => " + OperationAttributeValueHelpers.GetAttributeSetterExpression(member, sourceNameLiteral, "value") + ";");
                builder.AppendLine("    }");
            }
            else if (OperationAttributeValueHelpers.IsDenseCollectionConstraintKind(member.ConstraintKind))
            {
                var sourceNameLiteral = EmitterHelpers.ToCSharpStringLiteral(member.SourceName);
                var localName = EmitterHelpers.LowerFirst(member.PropertyName);
                builder.AppendLine("    public " + member.TypeName + " " + member.PropertyName);
                builder.AppendLine("    {");
                builder.AppendLine("        get => " + OperationAttributeValueHelpers.GetAttributeGetterExpression(member, sourceNameLiteral, localName) + ";");
                builder.AppendLine("        set => " + OperationAttributeValueHelpers.GetAttributeSetterExpression(member, sourceNameLiteral, "value") + ";");
                builder.AppendLine("    }");
            }
            else
            {
                var sourceNameLiteral = EmitterHelpers.ToCSharpStringLiteral(member.SourceName);
                var baseTypeName = isOptional ? member.TypeName.Substring(0, member.TypeName.Length - 1) : member.TypeName;
                var castExpr = "(" + baseTypeName + ")";
                var localName = EmitterHelpers.LowerFirst(member.PropertyName);

                builder.AppendLine("    public " + member.TypeName + " " + member.PropertyName);
                builder.AppendLine("    {");

                if (!isOptional)
                {
                    builder.AppendLine("        get => " + castExpr + "Attributes[" + sourceNameLiteral + "].Value;");
                    builder.AppendLine("        set => " + OperationAttributeValueHelpers.GetAttributeSetterExpression(member, sourceNameLiteral, "value") + ";");
                }
                else
                {
                    builder.AppendLine("        get => " + OperationAttributeValueHelpers.GetAttributeGetterExpression(member, sourceNameLiteral, localName) + ";");
                    builder.AppendLine("        set => " + OperationAttributeValueHelpers.GetAttributeSetterExpression(member, sourceNameLiteral, "value") + ";");
                }

                builder.AppendLine("    }");
            }
        }

        if (attributeMembers.Count > 0)
        {
            builder.AppendLine();
        }
    }

}
