namespace MLIR.Generators.Emitters;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using MLIR.ODS.Model;

internal static class OperationConstructorEmitter
{
    public static void Emit(StringBuilder builder, string className, OperationMemberPlan plan)
    {
        EmitContextConstructor(builder, className, plan.Operands, plan.Results);
        EmitPrimaryConstructor(builder, className, plan.Operands, plan.Results);
        EmitConvenienceConstructor(builder, className, plan.Operands, plan.Results);
        EmitPerAttributeConvenienceConstructor(builder, className, plan.Operands, plan.Results, plan.Attributes);
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
        IReadOnlyList<GeneratedMember> operandMembers,
        IReadOnlyList<GeneratedMember> resultMembers)
    {
        builder.AppendLine("    public " + className + "(OperationConstructionContext context)");
        builder.AppendLine("        : this(");
        builder.AppendLine("            syntax: context.Syntax,");

        for (var i = 0; i < operandMembers.Count; i++)
        {
            var member = operandMembers[i];
            var suffix = member.TypeName.EndsWith("?", System.StringComparison.Ordinal) ? string.Empty : "!";
            builder.AppendLine("            " + member.ParameterName + ": context.OperandValues[" + i.ToString(CultureInfo.InvariantCulture) + "]" + suffix + ",");
        }

        for (var i = 0; i < resultMembers.Count; i++)
        {
            var member = resultMembers[i];
            builder.AppendLine("            " + member.ParameterName + ": context.ResultValues[" + i.ToString(CultureInfo.InvariantCulture) + "],");
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
        IReadOnlyList<GeneratedMember> operandMembers,
        IReadOnlyList<GeneratedMember> resultMembers)
    {
        builder.AppendLine("    public " + className + "(");
        builder.AppendLine("        OperationSyntax? syntax,");
        AppendConstructorParameters(builder, operandMembers);
        AppendConstructorParameters(builder, resultMembers);
        builder.AppendLine("        NamedAttributeCollection attributes,");
        builder.AppendLine("        TypeReference? typeSignatureReference)");
        builder.AppendLine("        : base(");
        builder.AppendLine("            syntax,");
        builder.AppendLine("            global::System.Array.Empty<Region>(),");
        builder.AppendLine("            attributes,");
        builder.AppendLine("            typeSignatureReference,");
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
        builder.AppendLine("            global::System.Array.Empty<global::MLIR.Semantics.Block?>())");
        builder.AppendLine("    {");
        builder.AppendLine("    }");
        builder.AppendLine();
    }

    private static void EmitConvenienceConstructor(
        StringBuilder builder,
        string className,
        IReadOnlyList<GeneratedMember> operandMembers,
        IReadOnlyList<GeneratedMember> resultMembers)
    {
        builder.AppendLine("    public " + className + "(");
        AppendConstructorParameters(builder, operandMembers);
        AppendConstructorParameters(builder, resultMembers);
        builder.AppendLine("        NamedAttributeCollection attributes,");
        builder.AppendLine("        TypeReference? typeSignatureReference)");
        builder.AppendLine("        : this(");
        builder.AppendLine("            syntax: null,");
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
        IReadOnlyList<GeneratedMember> operandMembers,
        IReadOnlyList<GeneratedMember> resultMembers,
        IReadOnlyList<GeneratedMember> attributeMembers)
    {
        if (attributeMembers.Count == 0)
        {
            return;
        }

        builder.AppendLine("    public " + className + "(");
        AppendConstructorParameters(builder, operandMembers);
        AppendConstructorParameters(builder, resultMembers);
        AppendConstructorParameters(builder, attributeMembers);
        builder.AppendLine("        TypeReference? typeSignatureReference)");
        builder.AppendLine("        : this(");
        builder.AppendLine("            syntax: null,");
        AppendNamedArguments(builder, operandMembers, static member => member.ParameterName);
        AppendNamedArguments(builder, resultMembers, static member => member.ParameterName);

        var hasOptionalAttributes = false;
        for (var i = 0; i < attributeMembers.Count; i++)
        {
            if (attributeMembers[i].TypeName.EndsWith("?", StringComparison.Ordinal)
                || (attributeMembers[i].ConstraintKind == AttributeConstraintKind.UnitAttribute
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
