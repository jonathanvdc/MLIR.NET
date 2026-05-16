namespace MLIR.Generators.Emitters.AssemblyFormat;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using MLIR.Generators.Emitters.Common;
using MLIR.Generators.Emitters.Operation;
using MLIR.ODS.Model;
using MLIR.ODS.Model.AssemblyFormat;

internal abstract class FormatSubject
{
    protected FormatSubject(string subjectKind, string displayName, string className)
    {
        SubjectKind = subjectKind;
        DisplayName = displayName;
        ClassName = className;
    }

    public string SubjectKind { get; }
    public string DisplayName { get; }
    public string ClassName { get; }
    public string SyntaxClassName => ClassName + "Syntax";
    public string FormatClassName => ClassName + "AssemblyFormat";
    public abstract string SyntaxBaseType { get; }
    public abstract string PrefixType { get; }
    public virtual bool HasPrefix => true;
    public abstract string FormatBaseType { get; }
    public abstract string FormatMnemonic { get; }
    public virtual bool HasFormatMnemonicConstructor => true;
    public abstract string SyntaxReturnType { get; }
    public abstract string ParseFailureLocationExpression { get; }
    public abstract IReadOnlyList<Element> Elements { get; }
    public abstract FormatSlot? ResolveVariable(VariableChunk variable, int ordinal);
    public virtual FormatSlot? ResolveDirective(DirectiveChunk directive, int ordinal) => null;
    public abstract void EmitTryParseSignature(StringBuilder builder);
    public abstract void EmitBindMethod(StringBuilder builder, AssemblyFormatPlan plan);
    public abstract void EmitBuildMethod(StringBuilder builder, AssemblyFormatPlan plan);
}

internal sealed class AttributeFormatSubject : FormatSubject
{
    private readonly AttributeModel attribute;

    public AttributeFormatSubject(AttributeModel attribute, string className)
        : base("attribute", attribute.RecordName, className)
    {
        this.attribute = attribute;
    }

    public override string SyntaxBaseType => "global::MLIR.Syntax.DialectPrefixedAttributeValueSyntax";
    public override string PrefixType => "global::MLIR.Syntax.DialectAttributePrefix";
    public override string FormatBaseType => "global::MLIR.Dialects.BodyOnlyAttributeAssemblyFormat";
    public override string FormatMnemonic => attribute.Name;
    public override string SyntaxReturnType => "global::MLIR.Syntax.AttributeValueSyntax";
    public override string ParseFailureLocationExpression => "prefix.Location";
    public override IReadOnlyList<Element> Elements => attribute.AssemblyFormat!.Elements;

    public override FormatSlot? ResolveVariable(VariableChunk variable, int ordinal)
    {
        var parameter = attribute.Parameters.FirstOrDefault(p => string.Equals(p.Name, variable.Name, StringComparison.Ordinal));
        return parameter == null ? null : FormatSlot.ForParameter(variable.Name, ordinal, parameter);
    }

    public override void EmitTryParseSignature(StringBuilder builder)
    {
        builder.AppendLine("    protected override global::MLIR.Text.ParseResult<global::MLIR.Syntax.AttributeValueSyntax> TryParseBody(global::MLIR.Text.ParsingContext context, global::MLIR.Syntax.DialectAttributePrefix prefix)");
    }

    public override void EmitBindMethod(StringBuilder builder, AssemblyFormatPlan plan)
    {
        builder.AppendLine("    public override global::MLIR.Semantics.AttributeValue Bind(global::MLIR.Syntax.AttributeValueSyntax syntax, global::MLIR.Semantics.Binder binder)");
        builder.AppendLine("    {");
        if (!plan.IsSupported)
        {
            AssemblyFormatEmitterHelpers.EmitUnsupportedThrow(builder, plan, "attribute binding");
            builder.AppendLine("    }");
            return;
        }

        builder.AppendLine("        if (syntax is not " + SyntaxClassName + " structured)");
        builder.AppendLine("            throw new global::System.InvalidOperationException(\"Expected the generated attribute syntax class.\");");
        var args = new List<string>();
        foreach (var slot in plan.Slots.Where(s => s.Kind == FormatSlotKind.AttributeValue || s.Kind == FormatSlotKind.Type))
        {
            if (slot.ParameterModel?.IsSelfTypeParameter == true)
            {
                args.Add("binder.BindTypeReference(structured." + slot.PropertyName + ")");
            }
            else if (slot.ParameterModel?.CsharpExtractorTemplate != null)
            {
                args.Add(slot.ParameterModel.CsharpExtractorTemplate.Render("syntax", "structured." + slot.PropertyName));
            }
            else
            {
                args.Add("structured." + slot.PropertyName);
            }
        }

        builder.AppendLine("        return new " + ClassName + "(" + string.Join(", ", args) + ", syntax);");
        builder.AppendLine("    }");
    }

    public override void EmitBuildMethod(StringBuilder builder, AssemblyFormatPlan plan)
    {
        builder.AppendLine("    public override global::MLIR.Syntax.AttributeValueSyntax BuildCustomAssemblySyntax(global::MLIR.Semantics.AttributeValue attribute, global::MLIR.Transforms.ConcreteSyntaxBuilderContext context)");
        if (!plan.IsSupported)
        {
            builder.AppendLine("    {");
            AssemblyFormatEmitterHelpers.EmitUnsupportedThrow(builder, plan, "attribute syntax building");
            builder.AppendLine("    }");
            return;
        }

        AssemblyFormatEmitterHelpers.EmitAttrOrTypeBuildBody(builder, plan, "attribute", "attr", "global::MLIR.Syntax.DialectAttributePrefix.Synthetic(" + EmitterHelpers.ToCSharpStringLiteral(attribute.Name) + ")");
    }
}

internal sealed class TypeFormatSubject : FormatSubject
{
    private readonly TypeModel type;

    public TypeFormatSubject(TypeModel type, string className)
        : base("type", type.RecordName, className)
    {
        this.type = type;
    }

    public override string SyntaxBaseType => "global::MLIR.Syntax.DialectNamedTypeSyntax";
    public override string PrefixType => "global::MLIR.Syntax.DialectTypePrefix";
    public override string FormatBaseType => "global::MLIR.Dialects.BodyOnlyTypeAssemblyFormat";
    public override string FormatMnemonic => type.Name ?? string.Empty;
    public override string SyntaxReturnType => "global::MLIR.Syntax.TypeSyntax";
    public override string ParseFailureLocationExpression => "prefix.Location";
    public override IReadOnlyList<Element> Elements => type.AssemblyFormat!.Elements;

    public override FormatSlot? ResolveVariable(VariableChunk variable, int ordinal)
    {
        var parameter = type.Parameters.FirstOrDefault(p => string.Equals(p.Name, variable.Name, StringComparison.Ordinal));
        return parameter == null ? null : FormatSlot.ForParameter(variable.Name, ordinal, parameter);
    }

    public override void EmitTryParseSignature(StringBuilder builder)
    {
        builder.AppendLine("    protected override global::MLIR.Text.ParseResult<global::MLIR.Syntax.TypeSyntax> TryParseBody(global::MLIR.Text.ParsingContext context, global::MLIR.Syntax.DialectTypePrefix prefix)");
    }

    public override void EmitBindMethod(StringBuilder builder, AssemblyFormatPlan plan)
    {
        builder.AppendLine("    public override global::MLIR.Semantics.TypeReference Bind(global::MLIR.Syntax.TypeSyntax syntax, global::MLIR.Semantics.Binder binder)");
        builder.AppendLine("    {");
        if (!plan.IsSupported)
        {
            AssemblyFormatEmitterHelpers.EmitUnsupportedThrow(builder, plan, "type binding");
            builder.AppendLine("    }");
            return;
        }

        builder.AppendLine("        if (syntax is not " + SyntaxClassName + " structured)");
        builder.AppendLine("            throw new global::System.InvalidOperationException(\"Expected the generated type syntax class.\");");
        var args = new List<string>();
        foreach (var slot in plan.Slots.Where(s => s.Kind == FormatSlotKind.AttributeValue || s.Kind == FormatSlotKind.Type))
        {
            args.Add(slot.ParameterModel?.CsharpExtractorTemplate != null
                ? slot.ParameterModel.CsharpExtractorTemplate.Render("syntax", "structured." + slot.PropertyName)
                : "structured." + slot.PropertyName);
        }

        builder.AppendLine("        return new " + ClassName + "(" + string.Join(", ", args) + ", syntax);");
        builder.AppendLine("    }");
    }

    public override void EmitBuildMethod(StringBuilder builder, AssemblyFormatPlan plan)
    {
        builder.AppendLine("    public override global::MLIR.Syntax.TypeSyntax BuildCustomAssemblySyntax(global::MLIR.Semantics.TypeReference type, global::MLIR.Transforms.ConcreteSyntaxBuilderContext context)");
        if (!plan.IsSupported)
        {
            builder.AppendLine("    {");
            AssemblyFormatEmitterHelpers.EmitUnsupportedThrow(builder, plan, "type syntax building");
            builder.AppendLine("    }");
            return;
        }

        AssemblyFormatEmitterHelpers.EmitAttrOrTypeBuildBody(builder, plan, "type", "typed", "global::MLIR.Syntax.DialectTypePrefix.Synthetic(" + EmitterHelpers.ToCSharpStringLiteral(type.Name ?? string.Empty) + ")");
    }
}

internal sealed class OperationFormatSubject : FormatSubject
{
    private readonly OperationModel operation;
    private readonly DialectSymbolResolver resolver;
    private readonly OperationMemberPlan memberPlan;

    public OperationFormatSubject(OperationModel operation, string className, DialectSymbolResolver resolver)
        : base("operation", operation.ClassName ?? operation.Name, className)
    {
        this.operation = operation;
        this.resolver = resolver;
        memberPlan = OperationMemberPlanner.Plan(operation, resolver);
    }

    public override string SyntaxBaseType => "global::MLIR.Syntax.OperationBodySyntax";
    public override string PrefixType => "global::MLIR.Syntax.OperationHeader";
    public override bool HasPrefix => false;
    public override string FormatBaseType => "global::MLIR.Dialects.BodyOnlyOperationAssemblyFormat";
    public override string FormatMnemonic => string.Empty;
    public override bool HasFormatMnemonicConstructor => false;
    public override string SyntaxReturnType => "global::MLIR.Syntax.OperationBodySyntax";
    public override string ParseFailureLocationExpression => "header.NameToken.Location";
    public override IReadOnlyList<Element> Elements => operation.AssemblyFormat!.Elements;

    public override FormatSlot? ResolveVariable(VariableChunk variable, int ordinal)
    {
        var operand = operation.Operands.FirstOrDefault(o => string.Equals(o.Name, variable.Name, StringComparison.Ordinal));
        if (operand != null)
        {
            return operand.IsVariadic
                ? null
                : FormatSlot.ForOperationVariable(variable.Name, ordinal, FormatSlotKind.SsaValue, "context.TryParseSsaToken()");
        }

        var attribute = operation.Attributes.FirstOrDefault(a => string.Equals(a.Name, variable.Name, StringComparison.Ordinal));
        if (attribute != null)
        {
            var expectedConstraint = EmitterHelpers.TryGetAttributeConstraint(operation, attribute.Name);
            var expectedDefinitionExpr = !string.IsNullOrEmpty(expectedConstraint)
                ? resolver.TryResolveAttributeConstraintDefinitionExpression(expectedConstraint!)
                : null;
            var parseExpr = !string.IsNullOrEmpty(expectedDefinitionExpr)
                ? "context.TryParseAttributeValueSyntax(" + expectedDefinitionExpr + ")"
                : "context.TryParseAttributeValueSyntax()";
            return FormatSlot.ForOperationVariable(variable.Name, ordinal, FormatSlotKind.AttributeValue, parseExpr);
        }

        return null;
    }

    public override FormatSlot? ResolveDirective(DirectiveChunk directive, int ordinal)
    {
        if (directive is AttrDictDirectiveChunk)
        {
            return FormatSlot.ForDirective("attrDict", ordinal, FormatSlotKind.AttrDict, "context.TryParseAttrDict()", "global::MLIR.Syntax.DelimitedSyntaxList<global::MLIR.Syntax.NamedAttributeSyntax>");
        }

        if (directive is TypeDirectiveChunk typeDirective)
        {
            return FormatSlot.ForDirective(GetTypeDirectiveSlotName(typeDirective, ordinal), ordinal, FormatSlotKind.Type, "context.TryParseTypeSyntax()", "global::MLIR.Syntax.TypeSyntax");
        }

        return null;
    }

    public override void EmitTryParseSignature(StringBuilder builder)
    {
        builder.AppendLine("    protected override global::MLIR.Text.ParseResult<global::MLIR.Syntax.OperationBodySyntax> TryParseBody(in global::MLIR.Syntax.OperationHeader header, global::MLIR.Text.ParsingContext context)");
    }

    public override void EmitBindMethod(StringBuilder builder, AssemblyFormatPlan plan)
    {
        builder.AppendLine("    public override global::MLIR.Semantics.Operation Bind(global::MLIR.Syntax.OperationSyntax syntax, global::MLIR.Semantics.Binder binder)");
        builder.AppendLine("    {");
        if (!plan.IsSupported)
        {
            AssemblyFormatEmitterHelpers.EmitUnsupportedThrow(builder, plan, "operation binding");
            builder.AppendLine("    }");
            return;
        }

        builder.AppendLine("        if (syntax.Body is not " + SyntaxClassName + " body)");
        builder.AppendLine("            throw new global::System.InvalidOperationException(\"Expected the generated operation body syntax class.\");");
        builder.AppendLine("        var attributes = new global::System.Collections.Generic.List<global::MLIR.Semantics.NamedAttribute>();");
        foreach (var slot in plan.Slots.Where(s => s.Kind == FormatSlotKind.AttributeValue))
        {
            var expectedConstraint = EmitterHelpers.TryGetAttributeConstraint(operation, slot.SourceName);
            var expectedDefinitionExpr = !string.IsNullOrEmpty(expectedConstraint)
                ? resolver.TryResolveAttributeConstraintDefinitionExpression(expectedConstraint!)
                : null;
            var bindExpr = !string.IsNullOrEmpty(expectedDefinitionExpr)
                ? "binder.BindAttributeValue(body." + slot.PropertyName + ", " + expectedDefinitionExpr + ")"
                : "binder.BindAttributeValue(body." + slot.PropertyName + ")";
            builder.AppendLine("        attributes.Add(new global::MLIR.Semantics.NamedAttribute(" + EmitterHelpers.ToCSharpStringLiteral(slot.SourceName) + ", " + bindExpr + "));");
        }

        var attrDict = plan.Slots.FirstOrDefault(s => s.Kind == FormatSlotKind.AttrDict);
        if (attrDict != null)
        {
            builder.AppendLine("        foreach (var attr in body." + attrDict.PropertyName + ")");
            builder.AppendLine("            attributes.Add(binder.BindNamedAttribute(attr, " + ClassName + ".OperationDefinition));");
        }

        builder.AppendLine("        global::MLIR.Semantics.TypeReference? typeSignatureReference = null;");
        var typeSlot = plan.Slots.FirstOrDefault(s => s.Kind == FormatSlotKind.Type);
        if (typeSlot != null)
        {
            builder.AppendLine("        typeSignatureReference = binder.BindTypeReference(body." + typeSlot.PropertyName + ");");
        }

        builder.AppendLine("        return new " + ClassName + "(");
        builder.AppendLine("            syntax: syntax,");
        if (memberPlan.Regions.Count > 0)
        {
            builder.AppendLine("            regions: global::System.Array.Empty<global::MLIR.Semantics.Region>(),");
        }

        foreach (var member in memberPlan.Operands)
        {
            var slot = plan.Slots.FirstOrDefault(s => s.Kind == FormatSlotKind.SsaValue && string.Equals(s.SourceName, member.SourceName, StringComparison.Ordinal));
            if (member.IsVariadic)
            {
                builder.AppendLine("            " + member.ParameterName + ": global::System.Array.Empty<global::MLIR.Semantics.Value>(),");
            }
            else if (slot != null)
            {
                var nullableSuffix = member.TypeName.EndsWith("?", StringComparison.Ordinal) ? string.Empty : "!";
                builder.AppendLine("            " + member.ParameterName + ": binder.BindValueReference(body." + slot.PropertyName + ")" + nullableSuffix + ",");
            }
            else
            {
                builder.AppendLine("            " + member.ParameterName + ": null,");
            }
        }

        var resultIndex = 0;
        foreach (var member in memberPlan.Results)
        {
            if (member.IsVariadic)
            {
                builder.AppendLine("            " + member.ParameterName + ": syntax.ResultList.Skip(" + resultIndex.ToString(CultureInfo.InvariantCulture) + ").Select(static token => new global::MLIR.Semantics.OperationResult(token)).ToList(),");
                resultIndex = -1;
            }
            else
            {
                builder.AppendLine("            " + member.ParameterName + ": new global::MLIR.Semantics.OperationResult(syntax.ResultList[" + resultIndex.ToString(CultureInfo.InvariantCulture) + "]),");
                resultIndex++;
            }
        }

        builder.AppendLine("            attributes: new global::MLIR.Semantics.NamedAttributeCollection(attributes),");
        builder.AppendLine("            typeSignatureReference: typeSignatureReference);");
        builder.AppendLine("    }");
    }

    public override void EmitBuildMethod(StringBuilder builder, AssemblyFormatPlan plan)
    {
        builder.AppendLine("    public override global::MLIR.Syntax.OperationSyntax BuildCustomAssemblySyntax(global::MLIR.Semantics.Operation operation, global::MLIR.Transforms.ConcreteSyntaxBuilderContext context)");
        builder.AppendLine("    {");
        if (!plan.IsSupported)
        {
            AssemblyFormatEmitterHelpers.EmitUnsupportedThrow(builder, plan, "operation syntax building");
            builder.AppendLine("    }");
            return;
        }

        builder.AppendLine("        var typed = (" + ClassName + ")operation;");
        builder.AppendLine("        if (typed.Syntax != null)");
        builder.AppendLine("            return typed.Syntax;");
        foreach (var slot in plan.Slots)
        {
            builder.AppendLine("        var " + slot.ParameterName + " = " + BuildOperationSlotExpression(plan, slot) + ";");
        }

        builder.Append("        var body = new " + SyntaxClassName + "(");
        for (var i = 0; i < plan.Slots.Count; i++)
        {
            if (i > 0)
            {
                builder.Append(", ");
            }

            builder.Append(plan.Slots[i].ParameterName);
        }

        builder.AppendLine(");");
        builder.AppendLine("        return context.RewriteOperation(operation, body, global::MLIR.Syntax.TokenFactory.Identifier(" + EmitterHelpers.ToCSharpStringLiteral(operation.Name) + "));");
        builder.AppendLine("    }");
    }

    private string BuildOperationSlotExpression(AssemblyFormatPlan plan, FormatSlot slot)
    {
        switch (slot.Kind)
        {
            case FormatSlotKind.LiteralToken:
                return slot.BuildExpression("typed");
            case FormatSlotKind.SsaValue:
            {
                var member = memberPlan.Operands.FirstOrDefault(m => string.Equals(m.SourceName, slot.SourceName, StringComparison.Ordinal));
                var valueExpr = member != null ? "typed." + member.PropertyName : "null";
                return valueExpr + ".Token ?? global::MLIR.Syntax.TokenFactory.SsaName(" + valueExpr + ".Name)";
            }
            case FormatSlotKind.AttributeValue:
                return "context.BuildAttributeValueSyntax(typed.Attributes[" + EmitterHelpers.ToCSharpStringLiteral(slot.SourceName) + "].Value)";
            case FormatSlotKind.AttrDict:
                return "context.BuildAttrDict(" + BuildAttributeDictionaryExpression(plan) + ")";
            case FormatSlotKind.Type:
                return "context.BuildTypeSyntax(typed.TypeSignatureReference!)";
            default:
                throw new InvalidOperationException("Unsupported operation slot kind '" + slot.Kind + "'.");
        }
    }

    private static string BuildAttributeDictionaryExpression(AssemblyFormatPlan plan)
    {
        var expression = "typed.Attributes";
        foreach (var explicitAttribute in plan.Slots.Where(s => s.Kind == FormatSlotKind.AttributeValue))
        {
            expression += ".Remove(" + EmitterHelpers.ToCSharpStringLiteral(explicitAttribute.SourceName) + ")";
        }

        return expression;
    }

    private static string GetTypeDirectiveSlotName(TypeDirectiveChunk directive, int ordinal)
    {
        return directive.Operand switch
        {
            VariableOperand variable => variable.Name + "Type",
            _ => "type" + ordinal.ToString(CultureInfo.InvariantCulture),
        };
    }
}
