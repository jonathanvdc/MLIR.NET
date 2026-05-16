namespace MLIR.Generators.Emitters.AssemblyFormat;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using MLIR.Generators.Emitters.Common;
using MLIR.Generators.Emitters.Operation;
using MLIR.ODS.Model;
using MLIR.ODS.Model.AssemblyFormat;

/// <summary>
/// Compiles declarative assembly formats through a single subject-neutral plan.
/// Subject adapters resolve what variables mean for operations, attributes, and types; the
/// parser/binder/builder emission then walks the same slot list for every subject kind.
/// </summary>
internal static class UnifiedAssemblyFormatEmitter
{
    public static void EmitAttribute(StringBuilder builder, AttributeModel attribute, string className, IList<Diagnostic> diagnostics)
    {
        var subject = new AttributeFormatSubject(attribute, className);
        var plan = Compile(subject, diagnostics);
        EmitSyntaxClass(builder, subject, plan);
        builder.AppendLine();
        EmitAssemblyFormatClass(builder, subject, plan);
    }

    public static void EmitType(StringBuilder builder, TypeModel type, string className, IList<Diagnostic> diagnostics)
    {
        var subject = new TypeFormatSubject(type, className);
        var plan = Compile(subject, diagnostics);
        EmitSyntaxClass(builder, subject, plan);
        builder.AppendLine();
        EmitAssemblyFormatClass(builder, subject, plan);
    }

    public static void EmitOperation(StringBuilder builder, OperationModel operation, string className, DialectSymbolResolver resolver, IList<Diagnostic> diagnostics)
    {
        var subject = new OperationFormatSubject(operation, className, resolver);
        var plan = Compile(subject, diagnostics);
        EmitSyntaxClass(builder, subject, plan);
        builder.AppendLine();
        EmitAssemblyFormatClass(builder, subject, plan);
    }

    public static string GetResolvedCSharpType(AttrOrTypeParameterModel? param)
        => !string.IsNullOrEmpty(param?.CsharpType) ? param!.CsharpType! : "AttributeValueSyntax";

    private static AssemblyFormatPlan Compile(FormatSubject subject, IList<Diagnostic> diagnostics)
    {
        var compiler = new AssemblyFormatPlanCompiler(subject);
        var plan = compiler.Compile();
        foreach (var unsupported in plan.UnsupportedFeatures)
        {
            diagnostics.Add(Diagnostic.Create(
                DialectGeneratorDiagnostics.UnsupportedAssemblyFormatFeature,
                Location.None,
                subject.SubjectKind,
                subject.DisplayName,
                unsupported));
        }

        return plan;
    }

    private static void EmitSyntaxClass(StringBuilder builder, FormatSubject subject, AssemblyFormatPlan plan)
    {
        builder.AppendLine("internal sealed class " + subject.SyntaxClassName + " : " + subject.SyntaxBaseType);
        builder.AppendLine("{");
        builder.Append("    public " + subject.SyntaxClassName + "(");
        if (subject.HasPrefix)
        {
            builder.Append(subject.PrefixType + " prefix");
        }

        var firstParameter = !subject.HasPrefix;
        foreach (var slot in plan.Slots)
        {
            if (!firstParameter)
            {
                builder.Append(", ");
            }

            firstParameter = false;
            builder.Append(slot.CsType + " " + slot.ParameterName);
        }

        builder.AppendLine(")");
        if (subject.HasPrefix)
        {
            builder.AppendLine("        : base(prefix)");
        }

        builder.AppendLine("    {");
        foreach (var slot in plan.Slots)
        {
            builder.AppendLine("        " + slot.PropertyName + " = " + slot.ParameterName + ";");
        }

        builder.AppendLine("    }");
        builder.AppendLine();

        foreach (var slot in plan.Slots)
        {
            builder.AppendLine("    public " + slot.CsType + " " + slot.PropertyName + " { get; }");
        }

        if (plan.Slots.Count > 0)
        {
            builder.AppendLine();
        }

        EmitLocationProperty(builder, subject, plan);
        builder.AppendLine();
        builder.AppendLine("    public override void WriteTo(global::MLIR.Text.SyntaxWriter writer)");
        builder.AppendLine("    {");
        if (subject.HasPrefix)
        {
            builder.AppendLine("        WritePrefix(writer);");
        }

        for (var slotIndex = 0; slotIndex < plan.Slots.Count; slotIndex++)
        {
            var slot = plan.Slots[slotIndex];
            var trivia = GetLeadingTrivia(subject, plan.Slots, slotIndex);
            switch (slot.Kind)
            {
                case FormatSlotKind.LiteralToken:
                    builder.AppendLine("        writer.WriteToken(" + slot.PropertyName + ", " + EmitterHelpers.ToCSharpStringLiteral(trivia) + ");");
                    break;
                case FormatSlotKind.AttributeValue:
                    builder.AppendLine("        writer.SuggestTrivia(" + EmitterHelpers.ToCSharpStringLiteral(trivia) + ");");
                    builder.AppendLine("        " + slot.PropertyName + ".WriteTo(writer);");
                    break;
                case FormatSlotKind.Type:
                    builder.AppendLine("        writer.SuggestTrivia(" + EmitterHelpers.ToCSharpStringLiteral(trivia) + ");");
                    builder.AppendLine("        " + slot.PropertyName + ".WriteTo(writer);");
                    break;
                case FormatSlotKind.SsaValue:
                    builder.AppendLine("        writer.WriteToken(" + slot.PropertyName + ", " + EmitterHelpers.ToCSharpStringLiteral(trivia) + ");");
                    break;
                case FormatSlotKind.AttrDict:
                    builder.AppendLine("        writer.WriteDelimitedList(" + slot.PropertyName + ", " + EmitterHelpers.ToCSharpStringLiteral(trivia) + ");");
                    break;
            }
        }

        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    public override global::MLIR.Syntax.SyntaxNode Rewrite(global::MLIR.Syntax.SyntaxRewriter rewriter)");
        builder.AppendLine("    {");
        builder.Append("        return new " + subject.SyntaxClassName + "(");
        var needsComma = false;
        if (subject.HasPrefix)
        {
            builder.Append("Prefix");
            needsComma = true;
        }

        foreach (var slot in plan.Slots)
        {
            if (needsComma)
            {
                builder.Append(", ");
            }

            needsComma = true;
            builder.Append(slot.RewriteExpression);
        }

        builder.AppendLine(");");
        builder.AppendLine("    }");
        builder.AppendLine("}");
    }

    private static string GetLeadingTrivia(FormatSubject subject, IReadOnlyList<FormatSlot> slots, int slotIndex)
    {
        var slot = slots[slotIndex];
        if (slot.Kind == FormatSlotKind.LiteralToken && IsTightClosingLiteral(slot.TokenText))
        {
            return string.Empty;
        }

        if (slotIndex == 0)
        {
            return subject.HasPrefix ? string.Empty : " ";
        }

        var previous = slots[slotIndex - 1];
        if (previous.Kind == FormatSlotKind.LiteralToken && IsTightOpeningLiteral(previous.TokenText))
        {
            return string.Empty;
        }

        if (slot.Kind == FormatSlotKind.LiteralToken && string.Equals(slot.TokenText, ",", StringComparison.Ordinal))
        {
            return string.Empty;
        }

        return " ";
    }

    private static bool IsTightOpeningLiteral(string? text)
        => string.Equals(text, "<", StringComparison.Ordinal)
        || string.Equals(text, "(", StringComparison.Ordinal)
        || string.Equals(text, "[", StringComparison.Ordinal)
        || string.Equals(text, "{", StringComparison.Ordinal);

    private static bool IsTightClosingLiteral(string? text)
        => string.Equals(text, ">", StringComparison.Ordinal)
        || string.Equals(text, ")", StringComparison.Ordinal)
        || string.Equals(text, "]", StringComparison.Ordinal)
        || string.Equals(text, "}", StringComparison.Ordinal);

    private static void EmitLocationProperty(StringBuilder builder, FormatSubject subject, AssemblyFormatPlan plan)
    {
        var locations = new List<string>();
        if (subject.HasPrefix)
        {
            locations.Add("Prefix.Location");
        }

        foreach (var slot in plan.Slots)
        {
            locations.Add(slot.LocationExpression);
        }

        builder.AppendLine("    public override SourceLocation Location");
        builder.AppendLine("    {");
        builder.AppendLine("        get");
        builder.AppendLine("        {");
        if (locations.Count == 0)
        {
            builder.AppendLine("            return SourceLocation.Unknown;");
        }
        else
        {
            builder.AppendLine("            var result = " + locations[0] + ";");
            foreach (var location in locations.Skip(1))
            {
                builder.AppendLine("            result = SourceLocation.Merge(result, " + location + ");");
            }

            builder.AppendLine("            return result;");
        }

        builder.AppendLine("        }");
        builder.AppendLine("    }");
    }

    private static void EmitAssemblyFormatClass(StringBuilder builder, FormatSubject subject, AssemblyFormatPlan plan)
    {
        builder.AppendLine("internal sealed class " + subject.FormatClassName + " : " + subject.FormatBaseType);
        builder.AppendLine("{");
        builder.AppendLine("    public " + subject.FormatClassName + "()");
        if (subject.HasFormatMnemonicConstructor)
        {
            builder.AppendLine("        : base(" + EmitterHelpers.ToCSharpStringLiteral(subject.FormatMnemonic) + ")");
        }

        builder.AppendLine("    {");
        builder.AppendLine("    }");
        builder.AppendLine();
        subject.EmitTryParseSignature(builder);
        builder.AppendLine("    {");
        if (!plan.IsSupported)
        {
            EmitUnsupportedParseFailure(builder, subject, plan);
        }
        else
        {
            EmitTryParseBody(builder, subject, plan);
        }

        builder.AppendLine("    }");
        builder.AppendLine();
        subject.EmitBindMethod(builder, plan);
        builder.AppendLine();
        subject.EmitBuildMethod(builder, plan);
        builder.AppendLine("}");
    }

    private static void EmitUnsupportedParseFailure(StringBuilder builder, FormatSubject subject, AssemblyFormatPlan plan)
    {
        var message = "Unsupported declarative assembly format construct: " + plan.UnsupportedFeatures[0] + ".";
        builder.AppendLine("        return global::MLIR.Text.ParseResult<" + subject.SyntaxReturnType + ">.Failure(context.CreateDiagnostic(" + EmitterHelpers.ToCSharpStringLiteral(message) + "));");
    }

    private static void EmitUnsupportedThrow(StringBuilder builder, AssemblyFormatPlan plan, string action)
    {
        var message = "Unsupported declarative assembly format construct during " + action + ": " + plan.UnsupportedFeatures[0] + ".";
        builder.AppendLine("        throw new global::System.NotSupportedException(" + EmitterHelpers.ToCSharpStringLiteral(message) + ");");
    }

    private static void EmitTryParseBody(StringBuilder builder, FormatSubject subject, AssemblyFormatPlan plan)
    {
        foreach (var slot in plan.Slots)
        {
            builder.AppendLine("        var " + slot.ParameterName + "Result = " + slot.ParseExpression + ";");
            builder.AppendLine("        if (!" + slot.ParameterName + "Result.IsSuccess)");
            builder.AppendLine("            return global::MLIR.Text.ParseResult<" + subject.SyntaxReturnType + ">.Failure(" + slot.ParameterName + "Result.Diagnostic!);");
            builder.AppendLine("        var " + slot.ParameterName + " = " + slot.ParseValueExpression + ";");
        }

        builder.Append("        return global::MLIR.Text.ParseResult<" + subject.SyntaxReturnType + ">.Success(new " + subject.SyntaxClassName + "(");
        var needsComma = false;
        if (subject.HasPrefix)
        {
            builder.Append("prefix");
            needsComma = true;
        }

        foreach (var slot in plan.Slots)
        {
            if (needsComma)
            {
                builder.Append(", ");
            }

            needsComma = true;
            builder.Append(slot.ParameterName);
        }

        builder.AppendLine("));");
    }

    private abstract class FormatSubject
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

    private sealed class AttributeFormatSubject : FormatSubject
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
                EmitUnsupportedThrow(builder, plan, "attribute binding");
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
                EmitUnsupportedThrow(builder, plan, "attribute syntax building");
                builder.AppendLine("    }");
                return;
            }

            EmitAttrOrTypeBuildBody(builder, plan, "attribute", "attr", "global::MLIR.Syntax.DialectAttributePrefix.Synthetic(" + EmitterHelpers.ToCSharpStringLiteral(attribute.Name) + ")");
        }
    }

    private sealed class TypeFormatSubject : FormatSubject
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
                EmitUnsupportedThrow(builder, plan, "type binding");
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
                EmitUnsupportedThrow(builder, plan, "type syntax building");
                builder.AppendLine("    }");
                return;
            }

            EmitAttrOrTypeBuildBody(builder, plan, "type", "typed", "global::MLIR.Syntax.DialectTypePrefix.Synthetic(" + EmitterHelpers.ToCSharpStringLiteral(type.Name ?? string.Empty) + ")");
        }
    }

    private sealed class OperationFormatSubject : FormatSubject
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
                EmitUnsupportedThrow(builder, plan, "operation binding");
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
                EmitUnsupportedThrow(builder, plan, "operation syntax building");
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

    private static void EmitAttrOrTypeBuildBody(StringBuilder builder, AssemblyFormatPlan plan, string valueParameterName, string typedLocalName, string prefixExpression)
    {
        builder.AppendLine("    {");
        builder.AppendLine("        var " + typedLocalName + " = (" + plan.Subject.ClassName + ")" + valueParameterName + ";");
        builder.AppendLine("        if (" + typedLocalName + ".Syntax is " + plan.Subject.SyntaxClassName + " existingSyntax)");
        builder.AppendLine("            return existingSyntax;");
        foreach (var slot in plan.Slots)
        {
            builder.AppendLine("        var " + slot.ParameterName + " = " + slot.BuildExpression(typedLocalName) + ";");
        }

        builder.Append("        return new " + plan.Subject.SyntaxClassName + "(" + prefixExpression);
        foreach (var slot in plan.Slots)
        {
            builder.Append(", " + slot.ParameterName);
        }

        builder.AppendLine(");");
        builder.AppendLine("    }");
    }

    private sealed class AssemblyFormatPlanCompiler
    {
        private readonly FormatSubject subject;
        private readonly List<FormatSlot> slots = [];
        private readonly List<string> unsupported = [];
        private int ordinal;

        public AssemblyFormatPlanCompiler(FormatSubject subject)
        {
            this.subject = subject;
        }

        public AssemblyFormatPlan Compile()
        {
            foreach (var element in subject.Elements)
            {
                Lower(element);
            }

            return new AssemblyFormatPlan(subject, slots, unsupported.Distinct(StringComparer.Ordinal).ToArray());
        }

        private void Lower(Element element)
        {
            switch (element)
            {
                case LiteralChunk literal:
                    foreach (var literalElement in literal.Value)
                    {
                        LowerLiteral(literalElement);
                    }
                    break;
                case VariableChunk variable:
                    AddResolved(subject.ResolveVariable(variable, ordinal), "variable $" + variable.Name);
                    ordinal++;
                    break;
                case OilistDirectiveChunk:
                    unsupported.Add("oilist");
                    break;
                case DirectiveChunk directive:
                    AddResolved(subject.ResolveDirective(directive, ordinal), GetFeatureName(directive));
                    ordinal++;
                    break;
                case OptionalGroup:
                    unsupported.Add("optional group");
                    break;
                default:
                    unsupported.Add(element.GetType().Name);
                    break;
            }
        }

        private void LowerLiteral(Literal literal)
        {
            switch (literal)
            {
                case PunctuationLiteral punctuation:
                    slots.Add(FormatSlot.ForLiteral("literal" + ordinal.ToString(CultureInfo.InvariantCulture), ordinal, EmitterHelpers.GetPunctuationText(punctuation.TokenKind), "global::MLIR.Text.TokenKind." + punctuation.TokenKind));
                    ordinal++;
                    break;
                case KeywordLiteral keyword:
                    slots.Add(FormatSlot.ForLiteral("literal" + ordinal.ToString(CultureInfo.InvariantCulture), ordinal, keyword.Spelling, "global::MLIR.Text.TokenKind.Identifier", isKeyword: true));
                    ordinal++;
                    break;
                case WhitespaceLiteral:
                case NewlineLiteral:
                case EmptyLiteral:
                    break;
                default:
                    unsupported.Add(literal.GetType().Name);
                    break;
            }
        }

        private void AddResolved(FormatSlot? slot, string featureName)
        {
            if (slot == null)
            {
                unsupported.Add(featureName);
                return;
            }

            slots.Add(slot);
        }

        private static string GetFeatureName(DirectiveChunk directive)
        {
            return directive switch
            {
                AttrDictDirectiveChunk => "attr-dict",
                TypeDirectiveChunk => "type directive",
                _ => directive.GetType().Name,
            };
        }
    }

    private sealed class AssemblyFormatPlan
    {
        public AssemblyFormatPlan(FormatSubject subject, IReadOnlyList<FormatSlot> slots, IReadOnlyList<string> unsupportedFeatures)
        {
            Subject = subject;
            Slots = slots;
            UnsupportedFeatures = unsupportedFeatures;
        }

        public FormatSubject Subject { get; }
        public IReadOnlyList<FormatSlot> Slots { get; }
        public IReadOnlyList<string> UnsupportedFeatures { get; }
        public bool IsSupported => UnsupportedFeatures.Count == 0;
    }

    private enum FormatSlotKind
    {
        LiteralToken,
        AttributeValue,
        Type,
        SsaValue,
        AttrDict,
    }

    private sealed class FormatSlot
    {
        private FormatSlot(
            string sourceName,
            string baseName,
            FormatSlotKind kind,
            string csType,
            string parseExpression,
            AttrOrTypeParameterModel? parameterModel = null,
            string? tokenText = null,
            string? tokenKindExpression = null,
            bool isKeyword = false)
        {
            SourceName = sourceName;
            BaseName = baseName;
            Kind = kind;
            CsType = csType;
            ParseExpression = parseExpression;
            ParameterModel = parameterModel;
            TokenText = tokenText;
            TokenKindExpression = tokenKindExpression;
            IsKeyword = isKeyword;
        }

        public string SourceName { get; }
        public string BaseName { get; }
        public FormatSlotKind Kind { get; }
        public string CsType { get; }
        public string ParseExpression { get; }
        public AttrOrTypeParameterModel? ParameterModel { get; }
        public string? TokenText { get; }
        public string? TokenKindExpression { get; }
        public bool IsKeyword { get; }
        public string PropertyName => DialectGeneratorNaming.ToPascalCase(BaseName);
        public string ParameterName => EmitterHelpers.LowerFirst(PropertyName);

        public string ParseValueExpression
        {
            get
            {
                var value = ParameterName + "Result.Value";
                return Kind == FormatSlotKind.AttributeValue || Kind == FormatSlotKind.Type
                    ? "(" + CsType + ")" + value
                    : value;
            }
        }

        public string RewriteExpression => Kind switch
        {
            FormatSlotKind.LiteralToken => "rewriter.VisitToken(" + PropertyName + ")",
            FormatSlotKind.AttributeValue => "(" + CsType + ")rewriter.Visit(" + PropertyName + ")",
            FormatSlotKind.Type => "(" + CsType + ")rewriter.Visit(" + PropertyName + ")",
            FormatSlotKind.SsaValue => "rewriter.VisitToken(" + PropertyName + ")",
            FormatSlotKind.AttrDict => "rewriter.VisitDelimitedList(" + PropertyName + ")",
            _ => PropertyName,
        };

        public string LocationExpression => PropertyName + ".Location";

        public static FormatSlot ForLiteral(string name, int ordinal, string text, string tokenKindExpression, bool isKeyword = false)
        {
            var parseExpression = isKeyword
                ? "context.ExpectKeyword(" + EmitterHelpers.ToCSharpStringLiteral(text) + ", " + EmitterHelpers.ToCSharpStringLiteral("Expected '" + text + "'.") + ")"
                : "context.Expect(" + tokenKindExpression + ", " + EmitterHelpers.ToCSharpStringLiteral("Expected '" + text + "'.") + ")";
            return new FormatSlot(name, name, FormatSlotKind.LiteralToken, "global::MLIR.Syntax.Token", parseExpression, tokenText: text, tokenKindExpression: tokenKindExpression, isKeyword: isKeyword);
        }

        public static FormatSlot ForParameter(string name, int ordinal, AttrOrTypeParameterModel parameter)
        {
            var syntaxType = !string.IsNullOrEmpty(parameter.CsharpSyntaxType)
                ? parameter.CsharpSyntaxType!
                : "global::MLIR.Syntax.AttributeValueSyntax";
            if (syntaxType == "TypeSyntax" || syntaxType == "global::MLIR.Syntax.TypeSyntax")
            {
                return new FormatSlot(name, name, FormatSlotKind.Type, "global::MLIR.Syntax.TypeSyntax", "context.TryParseTypeSyntax()", parameter);
            }

            var parseExpression = parameter.CsharpParserTemplate != null
                ? parameter.CsharpParserTemplate.Render("parser", "context")
                : "context.TryParseAttributeValueSyntax()";
            return new FormatSlot(name, name, FormatSlotKind.AttributeValue, syntaxType, parseExpression, parameter);
        }

        public static FormatSlot ForOperationVariable(string name, int ordinal, FormatSlotKind kind, string parseExpression)
        {
            var csType = kind == FormatSlotKind.SsaValue
                ? "global::MLIR.Syntax.Token"
                : "global::MLIR.Syntax.AttributeValueSyntax";
            return new FormatSlot(name, name, kind, csType, parseExpression);
        }

        public static FormatSlot ForDirective(string name, int ordinal, FormatSlotKind kind, string parseExpression, string csType)
            => new(name, name, kind, csType, parseExpression);

        public string BuildExpression(string typedLocalName)
        {
            if (Kind == FormatSlotKind.LiteralToken)
            {
                return IsKeyword
                    ? "global::MLIR.Syntax.TokenFactory.Identifier(" + EmitterHelpers.ToCSharpStringLiteral(TokenText ?? string.Empty) + ")"
                    : "new global::MLIR.Syntax.Token(" + TokenKindExpression + ", " + EmitterHelpers.ToCSharpStringLiteral(TokenText ?? string.Empty) + ")";
            }

            var propertyExpression = typedLocalName + "." + DialectGeneratorNaming.ToPascalCase(SourceName);
            if (Kind == FormatSlotKind.Type)
            {
                return ParameterModel?.CsharpPrinterTemplate != null
                    ? ParameterModel.CsharpPrinterTemplate.Render("self", propertyExpression)
                    : propertyExpression;
            }

            if (ParameterModel?.CsharpPrinterTemplate != null)
            {
                return ParameterModel.CsharpPrinterTemplate.Render("self", propertyExpression);
            }

            return propertyExpression;
        }
    }
}
