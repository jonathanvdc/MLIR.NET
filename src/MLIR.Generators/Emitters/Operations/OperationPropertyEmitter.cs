namespace MLIR.Generators.Emitters.Operation;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using MLIR.Generators.Emitters;
using MLIR.Generators.Emitters.Common;
using MLIR.ODS.Model;

internal static class OperationPropertyEmitter
{
    public static void Emit(StringBuilder builder, string className, OperationModel operation, OperationMemberPlan plan)
    {
        EmitRegionProperties(builder, plan.Regions);
        EmitBlockAndOperationsConvenienceProperties(builder, operation, plan.Regions);
        EmitSymbolProperties(builder, operation);
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
                // Skip generating a redundant shadowing property: when the variadic result starts at slot 0
                // (covering all results) and its name shadows a base-class member, the generated property
                // is equivalent to the base Results property—same type, same values—and adds no value.
                if (resultSlotIndex == 0 && MemberModifier(member.PropertyName).Length > 0)
                {
                    resultSlotIndex = -1;
                    continue;
                }

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

    /// <summary>
    /// Recursively searches for a trait with the given <paramref name="recordName"/> in the
    /// provided trait list, descending into <see cref="TraitListModel"/> entries.
    /// </summary>
    internal static bool HasTrait(IReadOnlyList<TraitModel> traits, string recordName)
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

    /// <summary>
    /// Emits <c>Block</c> and <c>Operations</c> convenience properties when the operation
    /// declares exactly one non-variadic region and satisfies the ODS <c>SingleBlock</c>
    /// (and, for <c>Operations</c>, <c>NoRegionArguments</c>) traits.
    /// </summary>
    private static void EmitBlockAndOperationsConvenienceProperties(
        StringBuilder builder,
        OperationModel operation,
        IReadOnlyList<GeneratedMember> regionMembers)
    {
        // Only applicable when there is exactly one, non-variadic region.
        if (regionMembers.Count != 1 || regionMembers[0].IsVariadic)
        {
            return;
        }

        if (!HasTrait(operation.Traits, "SingleBlock"))
        {
            return;
        }

        var hasNoRegionArguments = HasTrait(operation.Traits, "NoRegionArguments");

        var regionPropertyName = regionMembers[0].PropertyName;
        // When the region type is nullable, use the null-forgiving operator so that the
        // convenience property stays non-nullable.  The SingleBlock trait implies the region
        // is structurally required; nullable here reflects only a static-analysis limitation
        // in how the planner determines requiredness.
        var isNullableRegion = regionMembers[0].TypeName.EndsWith("?", StringComparison.Ordinal);
        var regionAccess = isNullableRegion ? regionPropertyName + "!" : regionPropertyName;

        // Append a mention of the operation summary to the remarks so the generated surface
        // is self-explanatory (required by the issue: the doc comment must cite the summary
        // and the traits that justify the property).
        var summary = operation.Summary;
        var summaryRemark = summary != null && summary.Trim().Length > 0
            ? " The operation summary states: '" + EmitterHelpers.EscapeXmlText(summary.Trim()) + "'."
            : string.Empty;

        // Emit the Block convenience property.
        builder.AppendLine("    /// <summary>Gets the single block of this operation's body region.</summary>");
        builder.AppendLine("    /// <remarks>");
        builder.AppendLine("    /// This property is generated because this operation declares exactly one region and");
        builder.AppendLine("    /// satisfies the ODS <c>SingleBlock</c> constraint." + summaryRemark);
        builder.AppendLine("    /// </remarks>");
        builder.AppendLine("    public Block Block => " + regionAccess + ".Blocks.Single();");
        builder.AppendLine();

        if (hasNoRegionArguments)
        {
            // Emit the Operations convenience property.
            builder.AppendLine("    /// <summary>Gets the operations in the single block of this operation's body region.</summary>");
            builder.AppendLine("    /// <remarks>");
            builder.AppendLine("    /// This property is generated because this operation declares exactly one region and");
            builder.AppendLine("    /// satisfies ODS constraints that imply a single block (<c>SingleBlock</c>) and no region");
            builder.AppendLine("    /// arguments (<c>NoRegionArguments</c>)." + summaryRemark);
            builder.AppendLine("    /// </remarks>");
            builder.AppendLine("    public IReadOnlyList<Operation> Operations => Block.Operations;");
            builder.AppendLine();
        }
    }

    /// <summary>
    /// Emits <c>SymbolName</c> for operations with the ODS <c>Symbol</c> trait.
    /// Operations with the <c>SymbolTable</c> trait inherit all symbol-table logic from
    /// <c>SymbolTableOperation</c> and require no additional property emission here.
    /// </summary>
    private static void EmitSymbolProperties(StringBuilder builder, OperationModel operation)
    {
        var hasSymbol = HasTrait(operation.Traits, "Symbol");

        if (!hasSymbol)
        {
            return;
        }

        // SymbolName getter/setter backed by the "sym_name" attribute.
        // sym_name is a StringAttr in MLIR; we access it as StringAttributeValue.
        // This property satisfies the ISymbolOp interface that is declared on the class.
        builder.AppendLine("    /// <summary>Gets or sets the symbol name of this operation, backed by the <c>sym_name</c> attribute.</summary>");
        builder.AppendLine("    /// <remarks>This property is generated because this operation has the ODS <c>Symbol</c> trait.</remarks>");
        builder.AppendLine("    public string? SymbolName");
        builder.AppendLine("    {");
        builder.AppendLine("        get => Attributes.TryGet(\"sym_name\", out var symAttr) && symAttr.Value is StringAttributeValue sv ? sv.Value : null;");
        builder.AppendLine("        set => SetAttribute(\"sym_name\", value != null ? new SyntheticStringAttributeValue(value) : null);");
        builder.AppendLine("    }");
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
            // ConstraintStrategy is always non-null for attribute members: the planner
            // sets it to at least FallbackAttributeConstraintCodeStrategy.Instance.
            var strategy = member.ConstraintStrategy!;
            var hasAttrModelTyping = !string.IsNullOrEmpty(member.AttrStorageTypeName)
                && !string.IsNullOrEmpty(member.AttrConvertFromStorageExpression);

            if (hasAttrModelTyping && !strategy.IsUnit && !strategy.IsEnum)
            {
                var sourceNameLiteral = EmitterHelpers.ToCSharpStringLiteral(member.SourceName);
                var localName = EmitterHelpers.LowerFirst(member.PropertyName);
                builder.AppendLine("    public " + member.TypeName + " " + member.PropertyName);
                builder.AppendLine("    {");
                builder.AppendLine("        get => " + OperationAttributeValueHelpers.GetAttributeGetterExpression(member, sourceNameLiteral, localName) + ";");
                builder.AppendLine("        set => " + OperationAttributeValueHelpers.GetAttributeSetterExpression(member, sourceNameLiteral, "value") + ";");
                builder.AppendLine("    }");
                continue;
            }

            if (strategy.IsUnit)
            {
                if (!string.Equals(member.TypeName, "bool", StringComparison.Ordinal))
                {
                    continue;
                }

                var sourceNameLiteral = EmitterHelpers.ToCSharpStringLiteral(member.SourceName);
                builder.AppendLine("    public bool " + member.PropertyName);
                builder.AppendLine("    {");
                builder.AppendLine("        get => Attributes.Contains(" + sourceNameLiteral + ");");
                builder.AppendLine("        set => SetAttribute(" + sourceNameLiteral + ", value ? " + OperationAttributeValueHelpers.GetUnitAttributeValueExpression() + " : null);");
                builder.AppendLine("    }");
                continue;
            }

            if (strategy.IsPrimitive)
            {
                var sourceNameLiteral = EmitterHelpers.ToCSharpStringLiteral(member.SourceName);
                var localName = EmitterHelpers.LowerFirst(member.PropertyName);
                builder.AppendLine("    public " + member.TypeName + " " + member.PropertyName);
                builder.AppendLine("    {");
                builder.AppendLine("        get => " + OperationAttributeValueHelpers.GetAttributeGetterExpression(member, sourceNameLiteral, localName) + ";");
                builder.AppendLine("        set => " + OperationAttributeValueHelpers.GetAttributeSetterExpression(member, sourceNameLiteral, "value") + ";");
                builder.AppendLine("    }");
            }
            else if (strategy.IsDenseCollection || strategy.IsTypedArray)
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
