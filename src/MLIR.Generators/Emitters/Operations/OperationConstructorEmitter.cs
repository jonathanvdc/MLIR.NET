namespace MLIR.Generators.Emitters.Operation;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using MLIR.Generators.Emitters;
using MLIR.Generators.Emitters.Common;

internal static class OperationConstructorEmitter
{
    public static void Emit(StringBuilder builder, string className, OperationMemberPlan plan)
    {
        EmitContextConstructor(builder, className, plan.Regions, plan.Operands, plan.Results);
        EmitPrimaryConstructor(builder, className, plan.Regions, plan.Operands, plan.Results);
        EmitConvenienceConstructor(builder, className, plan.Regions, plan.Operands, plan.Results);
        EmitPerAttributeConvenienceConstructor(builder, className, plan.Regions, plan.Operands, plan.Results, plan.Attributes);
    }

    private static void AppendConstructorParameters(StringBuilder builder, IReadOnlyList<GeneratedMember> members)
    {
        for (var i = 0; i < members.Count; i++)
        {
            var member = members[i];
            builder.AppendLine("        " + member.TypeName + " " + member.ParameterName + ",");
        }
    }

    private static void AppendNamedArguments(
        StringBuilder builder,
        IReadOnlyList<GeneratedMember> members,
        Func<GeneratedMember, string> valueExpression)
    {
        for (var i = 0; i < members.Count; i++)
        {
            var member = members[i];
            builder.AppendLine("            " + member.ParameterName + ": " + valueExpression(member) + ",");
        }
    }

    private static void EmitContextConstructor(
        StringBuilder builder,
        string className,
        IReadOnlyList<GeneratedMember> regionMembers,
        IReadOnlyList<GeneratedMember> operandMembers,
        IReadOnlyList<GeneratedMember> resultMembers)
    {
        builder.AppendLine("    public " + className + "(OperationConstructionContext context)");
        builder.AppendLine("        : this(");
        builder.AppendLine("            syntax: context.Syntax,");

        if (regionMembers.Count > 0)
        {
            builder.AppendLine("            regions: context.Regions,");
        }

        // Track how many individual Value? slots have been consumed so far so we know
        // the correct starting index when building the variadic slice.
        var slotIndex = 0;
        for (var i = 0; i < operandMembers.Count; i++)
        {
            var member = operandMembers[i];
            if (member.IsVariadic)
            {
                // Collect all remaining operands from slotIndex onward as a non-null Value list.
                // context.OperandValues entries may be null for unresolved refs; filter them out.
                var skip = slotIndex.ToString(CultureInfo.InvariantCulture);
                builder.AppendLine("            " + member.ParameterName + ": context.OperandValues.Skip(" + skip + ").OfType<Value>().ToList(),");
                // A variadic slot consumes all remaining entries; no further fixed indexing.
                slotIndex = -1; // sentinel: can't index further after variadic
            }
            else
            {
                var suffix = member.TypeName.EndsWith("?", System.StringComparison.Ordinal) ? string.Empty : "!";
                builder.AppendLine("            " + member.ParameterName + ": context.OperandValues[" + slotIndex.ToString(CultureInfo.InvariantCulture) + "]" + suffix + ",");
                slotIndex++;
            }
        }

        var resultSlotIndex = 0;
        for (var i = 0; i < resultMembers.Count; i++)
        {
            var member = resultMembers[i];
            if (member.IsVariadic)
            {
                // Collect all remaining results from resultSlotIndex onward as a list.
                var skip = resultSlotIndex.ToString(CultureInfo.InvariantCulture);
                builder.AppendLine("            " + member.ParameterName + ": context.ResultValues.Skip(" + skip + ").ToList(),");
                // A variadic result slot consumes all remaining entries.
                resultSlotIndex = -1;
            }
            else
            {
                builder.AppendLine("            " + member.ParameterName + ": context.ResultValues[" + resultSlotIndex.ToString(CultureInfo.InvariantCulture) + "],");
                resultSlotIndex++;
            }
        }

        builder.AppendLine("            attributes: context.Attributes,");
        builder.AppendLine("            typeSignatureReference: context.TypeSignatureReference)");
        builder.AppendLine("    {");
        builder.AppendLine("    }");
        builder.AppendLine();
    }

    private static void EmitPrimaryConstructor(
        StringBuilder builder,
        string className,
        IReadOnlyList<GeneratedMember> regionMembers,
        IReadOnlyList<GeneratedMember> operandMembers,
        IReadOnlyList<GeneratedMember> resultMembers)
    {
        builder.AppendLine("    public " + className + "(");
        builder.AppendLine("        OperationSyntax? syntax,");
        if (regionMembers.Count > 0)
        {
            builder.AppendLine("        global::System.Collections.Generic.IReadOnlyList<Region> regions,");
        }
        AppendConstructorParameters(builder, operandMembers);
        AppendConstructorParameters(builder, resultMembers);
        builder.AppendLine("        NamedAttributeCollection attributes,");
        builder.AppendLine("        TypeReference? typeSignatureReference)");
        builder.AppendLine("        : base(");
        builder.AppendLine("            syntax,");
        if (regionMembers.Count > 0)
        {
            builder.AppendLine("            regions,");
        }
        else
        {
            builder.AppendLine("            global::System.Array.Empty<Region>(),");
        }
        builder.AppendLine("            attributes,");
        builder.AppendLine("            typeSignatureReference,");
        if (!resultMembers.Any(static m => m.IsVariadic))
        {
            builder.Append("            new OperationResult[] { ");
            for (var i = 0; i < resultMembers.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append(", ");
                }

                builder.Append(resultMembers[i].ParameterName);
            }

            builder.AppendLine(" },");
        }
        else
        {
            // Mix of fixed and variadic results: build via Concat.
            builder.Append("            ");
            var firstResult = true;
            foreach (var member in resultMembers)
            {
                if (!firstResult)
                {
                    builder.Append(".Concat(");
                }

                if (member.IsVariadic)
                {
                    builder.Append(member.ParameterName);
                }
                else
                {
                    builder.Append("new OperationResult[] { " + member.ParameterName + " }");
                }

                if (!firstResult)
                {
                    builder.Append(")");
                }

                firstResult = false;
            }

            builder.AppendLine(".ToArray(),");
        }

        // Build the operand array, spreading variadic list members.
        if (!operandMembers.Any(static m => m.IsVariadic))
        {
            builder.Append("            new Value?[] { ");
            for (var i = 0; i < operandMembers.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append(", ");
                }

                builder.Append(operandMembers[i].ParameterName);
            }

            builder.AppendLine(" },");
        }
        else
        {
            // Mix of fixed and variadic: build via Concat / Cast.
            builder.Append("            ");
            var first = true;
            foreach (var member in operandMembers)
            {
                if (!first)
                {
                    builder.Append(".Concat(");
                }

                if (member.IsVariadic)
                {
                    builder.Append("global::System.Linq.Enumerable.Cast<Value?>(" + member.ParameterName + ")");
                }
                else
                {
                    builder.Append("new Value?[] { " + member.ParameterName + " }");
                }

                if (!first)
                {
                    builder.Append(")");
                }

                first = false;
            }

            builder.AppendLine(".ToArray(),");
        }

        builder.AppendLine("            global::System.Array.Empty<global::MLIR.Semantics.Block?>())");
        builder.AppendLine("    {");
        builder.AppendLine("    }");
        builder.AppendLine();
    }

    private static void EmitConvenienceConstructor(
        StringBuilder builder,
        string className,
        IReadOnlyList<GeneratedMember> regionMembers,
        IReadOnlyList<GeneratedMember> operandMembers,
        IReadOnlyList<GeneratedMember> resultMembers)
    {
        builder.AppendLine("    public " + className + "(");
        if (regionMembers.Count > 0)
        {
            builder.AppendLine("        global::System.Collections.Generic.IReadOnlyList<Region> regions,");
        }
        AppendConstructorParameters(builder, operandMembers);
        AppendConstructorParameters(builder, resultMembers);
        builder.AppendLine("        NamedAttributeCollection attributes,");
        builder.AppendLine("        TypeReference? typeSignatureReference)");
        builder.AppendLine("        : this(");
        builder.AppendLine("            syntax: null,");
        if (regionMembers.Count > 0)
        {
            builder.AppendLine("            regions: regions,");
        }
        AppendNamedArguments(builder, operandMembers, static member => member.ParameterName);
        AppendNamedArguments(builder, resultMembers, static member => member.ParameterName);
        builder.AppendLine("            attributes: attributes,");
        builder.AppendLine("            typeSignatureReference: typeSignatureReference)");
        builder.AppendLine("    {");
        builder.AppendLine("    }");
        builder.AppendLine();
    }

    private static void EmitPerAttributeConvenienceConstructor(
        StringBuilder builder,
        string className,
        IReadOnlyList<GeneratedMember> regionMembers,
        IReadOnlyList<GeneratedMember> operandMembers,
        IReadOnlyList<GeneratedMember> resultMembers,
        IReadOnlyList<GeneratedMember> attributeMembers)
    {
        if (attributeMembers.Count == 0)
        {
            return;
        }

        builder.AppendLine("    public " + className + "(");
        if (regionMembers.Count > 0)
        {
            builder.AppendLine("        global::System.Collections.Generic.IReadOnlyList<Region> regions,");
        }
        AppendConstructorParameters(builder, operandMembers);
        AppendConstructorParameters(builder, resultMembers);
        AppendConstructorParameters(builder, attributeMembers);
        builder.AppendLine("        TypeReference? typeSignatureReference)");
        builder.AppendLine("        : this(");
        builder.AppendLine("            syntax: null,");
        if (regionMembers.Count > 0)
        {
            builder.AppendLine("            regions: regions,");
        }
        AppendNamedArguments(builder, operandMembers, static member => member.ParameterName);
        AppendNamedArguments(builder, resultMembers, static member => member.ParameterName);

        var hasOptionalAttributes = false;
        for (var i = 0; i < attributeMembers.Count; i++)
        {
            if (attributeMembers[i].TypeName.EndsWith("?", StringComparison.Ordinal)
                || (attributeMembers[i].ConstraintStrategy!.IsUnit
                    && string.Equals(attributeMembers[i].TypeName, "bool", StringComparison.Ordinal)))
            {
                hasOptionalAttributes = true;
                break;
            }
        }

        if (!hasOptionalAttributes)
        {
            builder.Append("            attributes: NamedAttributeCollection.Create(");
            for (var i = 0; i < attributeMembers.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append(", ");
                }

                builder.Append(OperationAttributeValueHelpers.GetNamedAttributeExpression(attributeMembers[i], attributeMembers[i].ParameterName));
            }

            builder.AppendLine("),");
        }
        else
        {
            builder.Append("            attributes: new NamedAttributeCollection(new NamedAttribute?[] { ");
            for (var i = 0; i < attributeMembers.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append(", ");
                }

                builder.Append(OperationAttributeValueHelpers.GetNamedAttributeExpression(attributeMembers[i], attributeMembers[i].ParameterName));
            }

            builder.AppendLine(" }.Where(a => a is not null).Select(a => a!)),");
        }

        builder.AppendLine("            typeSignatureReference: typeSignatureReference)");
        builder.AppendLine("    {");
        builder.AppendLine("    }");
        builder.AppendLine();
    }
}
