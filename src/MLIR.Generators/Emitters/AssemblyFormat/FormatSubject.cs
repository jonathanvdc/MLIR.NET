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
                ? FormatSlot.ForOperationVariable(variable.Name, ordinal, FormatSlotKind.SsaValueList, "context.TryParseSsaTokenList()")
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

        if (directive is QualifiedDirectiveChunk qualifiedDirective)
        {
            return ResolveQualifiedDirective(qualifiedDirective, ordinal);
        }

        if (directive is FunctionalTypeDirectiveChunk)
        {
            return FormatSlot.ForDirective("functionType" + ordinal.ToString(CultureInfo.InvariantCulture), ordinal, FormatSlotKind.Type, "context.TryParseTypeSyntax()", "global::MLIR.Syntax.TypeSyntax");
        }

        return null;
    }

    private FormatSlot? ResolveQualifiedDirective(QualifiedDirectiveChunk directive, int ordinal)
    {
        if (directive.Operand is TypeDirectiveOperand typeOperand)
        {
            return FormatSlot.ForDirective(GetTypeDirectiveSlotName(new TypeDirectiveChunk(typeOperand.Operand), ordinal), ordinal, FormatSlotKind.Type, "context.TryParseTypeSyntax()", "global::MLIR.Syntax.TypeSyntax");
        }

        if (directive.Operand is VariableOperand variable)
        {
            return ResolveVariable(new VariableChunk(variable.Name), ordinal);
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
                ? "binder.BindAttributeValue(" + slot.BodyAccessExpression + ", " + expectedDefinitionExpr + ")"
                : "binder.BindAttributeValue(" + slot.BodyAccessExpression + ")";
            if (slot.ContainingOptionalGroup != null)
            {
                builder.AppendLine("        if (body." + slot.ContainingOptionalGroup.PropertyName + " != null)");
                builder.AppendLine("            attributes.Add(new global::MLIR.Semantics.NamedAttribute(" + EmitterHelpers.ToCSharpStringLiteral(slot.SourceName) + ", " + bindExpr + "));");
            }
            else
            {
                builder.AppendLine("        attributes.Add(new global::MLIR.Semantics.NamedAttribute(" + EmitterHelpers.ToCSharpStringLiteral(slot.SourceName) + ", " + bindExpr + "));");
            }
        }

        var attrDict = plan.Slots.FirstOrDefault(s => s.Kind == FormatSlotKind.AttrDict);
        if (attrDict != null)
        {
            builder.AppendLine("        foreach (var attr in " + attrDict.BodyAccessExpression + ")");
            builder.AppendLine("            attributes.Add(binder.BindNamedAttribute(attr, " + ClassName + ".OperationDefinition));");
        }

        builder.AppendLine("        global::MLIR.Semantics.TypeReference? typeSignatureReference = null;");
        var typeSlot = plan.Slots.FirstOrDefault(s => s.Kind == FormatSlotKind.Type);
        if (typeSlot != null)
        {
            if (typeSlot.ContainingOptionalGroup != null)
            {
                builder.AppendLine("        if (body." + typeSlot.ContainingOptionalGroup.PropertyName + " != null)");
                builder.AppendLine("            typeSignatureReference = binder.BindTypeReference(" + typeSlot.BodyAccessExpression + ");");
            }
            else
            {
                builder.AppendLine("        typeSignatureReference = binder.BindTypeReference(" + typeSlot.BodyAccessExpression + ");");
            }
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
                var listSlot = plan.Slots.FirstOrDefault(s => s.Kind == FormatSlotKind.SsaValueList && string.Equals(s.SourceName, member.SourceName, StringComparison.Ordinal));
                if (listSlot != null)
                {
                    var bindList = "global::System.Linq.Enumerable.ToList(global::System.Linq.Enumerable.Select(" + listSlot.BodyAccessExpression + ", binder.BindValueReference))";
                    if (listSlot.ContainingOptionalGroup != null)
                    {
                        bindList = "body." + listSlot.ContainingOptionalGroup.PropertyName + " == null ? new global::System.Collections.Generic.List<global::MLIR.Semantics.Value>() : " + bindList;
                    }

                    builder.AppendLine("            " + member.ParameterName + ": " + bindList + ",");
                }
                else
                {
                    builder.AppendLine("            " + member.ParameterName + ": global::System.Array.Empty<global::MLIR.Semantics.Value>(),");
                }
            }
            else if (slot != null)
            {
                var nullableSuffix = member.TypeName.EndsWith("?", StringComparison.Ordinal) ? string.Empty : "!";
                var bindValue = "binder.BindValueReference(" + slot.BodyAccessExpression + ")" + nullableSuffix;
                if (slot.ContainingOptionalGroup != null)
                {
                    bindValue = "body." + slot.ContainingOptionalGroup.PropertyName + " == null ? null : " + bindValue;
                }

                builder.AppendLine("            " + member.ParameterName + ": " + bindValue + ",");
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
        var syntaxNodes = plan.SyntaxNodes.ToArray();
        foreach (var node in syntaxNodes)
        {
            EmitBuildOperationNode(builder, plan, node, "        ");
        }

        builder.Append("        var body = new " + SyntaxClassName + "(");
        for (var i = 0; i < syntaxNodes.Length; i++)
        {
            if (i > 0)
            {
                builder.Append(", ");
            }

            builder.Append(syntaxNodes[i].ParameterName);
        }

        builder.AppendLine(");");
        builder.AppendLine("        return context.RewriteOperation(operation, body, global::MLIR.Syntax.TokenFactory.Identifier(" + EmitterHelpers.ToCSharpStringLiteral(operation.Name) + "));");
        builder.AppendLine("    }");
    }

    private void EmitBuildOperationNode(StringBuilder builder, AssemblyFormatPlan plan, FormatNode node, string indent)
    {
        if (node is FormatSlot slot)
        {
            builder.AppendLine(indent + "var " + slot.ParameterName + " = " + BuildOperationSlotExpression(plan, slot) + ";");
            return;
        }

        if (node is not OptionalGroupNode group)
        {
            throw new InvalidOperationException("Unsupported operation format node '" + node.GetType().Name + "'.");
        }

        builder.AppendLine(indent + group.CsType + " " + group.ParameterName + " = null;");
        builder.AppendLine(indent + "if (" + GetOperationGroupPresenceExpression(group) + ")");
        builder.AppendLine(indent + "{");
        foreach (var child in group.Nodes.Where(static child => child.IsSyntaxNode))
        {
            EmitBuildOperationNode(builder, plan, child, indent + "    ");
        }

        builder.Append(indent + "    " + group.ParameterName + " = new " + group.SyntaxClassName + "(");
        var needsComma = false;
        foreach (var child in group.Nodes.Where(static child => child.IsSyntaxNode))
        {
            if (needsComma)
            {
                builder.Append(", ");
            }

            needsComma = true;
            builder.Append(child.ParameterName);
        }

        builder.AppendLine(");");
        builder.AppendLine(indent + "}");
    }

    private string GetOperationGroupPresenceExpression(OptionalGroupNode group)
    {
        var anchor = group.AnchorSlot;
        if (anchor == null)
        {
            return "false";
        }

        return anchor.Kind switch
        {
            FormatSlotKind.AttributeValue => "typed.Attributes.Contains(" + EmitterHelpers.ToCSharpStringLiteral(anchor.SourceName) + ")",
            FormatSlotKind.SsaValue => GetOperandPresenceExpression(anchor.SourceName),
            FormatSlotKind.SsaValueList => GetOperandListPresenceExpression(anchor.SourceName),
            FormatSlotKind.Type => "typed.TypeSignatureReference != null",
            _ => "true",
        };
    }

    private string GetOperandPresenceExpression(string sourceName)
    {
        var member = memberPlan.Operands.FirstOrDefault(m => string.Equals(m.SourceName, sourceName, StringComparison.Ordinal));
        if (member == null)
        {
            return "false";
        }

        return "typed." + member.PropertyName + " != null";
    }

    private string GetOperandListPresenceExpression(string sourceName)
    {
        var member = memberPlan.Operands.FirstOrDefault(m => string.Equals(m.SourceName, sourceName, StringComparison.Ordinal));
        if (member == null)
        {
            return "false";
        }

        return "typed." + member.PropertyName + ".Count > 0";
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
            case FormatSlotKind.SsaValueList:
            {
                var member = memberPlan.Operands.FirstOrDefault(m => string.Equals(m.SourceName, slot.SourceName, StringComparison.Ordinal));
                var valueExpr = member != null ? "typed." + member.PropertyName : "global::System.Array.Empty<global::MLIR.Semantics.Value>()";
                return "new global::MLIR.Syntax.SeparatedSyntaxList<global::MLIR.Syntax.Token>("
                    + "global::System.Linq.Enumerable.ToArray(global::System.Linq.Enumerable.Select(" + valueExpr + ", static value => value.Token ?? global::MLIR.Syntax.TokenFactory.SsaName(value.Name))), "
                    + "global::System.Linq.Enumerable.ToArray(global::System.Linq.Enumerable.Select(global::System.Linq.Enumerable.Range(0, global::System.Math.Max(0, " + valueExpr + ".Count - 1)), static _ => global::MLIR.Syntax.TokenFactory.Comma())))";
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
