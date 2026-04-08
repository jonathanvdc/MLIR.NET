namespace MLIR.Generators.Emitters.AssemblyFormat;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MLIR.Generators.Emitters;
using MLIR.Generators.Emitters.Common;
using MLIR.Generators.Emitters.Operation;
using MLIR.ODS.Model;
using MLIR.ODS.Model.AssemblyFormat;
using MLIR.Text;

/// <summary>
/// Generates the <c>BuildCustomAssemblySyntax</c> method body for a declarative assembly format.
/// </summary>
/// <remarks>
/// The generated method casts the supplied semantic <c>Operation</c> to the typed subclass,
/// reconstructs each field of the corresponding <c>*BodySyntax</c> from the operation's
/// semantic properties, then returns an <c>OperationSyntax</c> whose body is that typed syntax node.
/// </remarks>
internal sealed class BuildCustomAssemblySyntaxEmitter
{
    private readonly OperationModel operation;
    private readonly OperationBodySyntaxMetadata metadata;
    private readonly string className;
    private readonly DialectSymbolResolver resolver;
    private readonly System.Collections.Generic.HashSet<string> requiredVariables;
    private int fieldIndex;

    private BuildCustomAssemblySyntaxEmitter(OperationModel operation, OperationBodySyntaxMetadata metadata, DialectSymbolResolver resolver)
    {
        this.operation = operation;
        this.metadata = metadata;
        this.resolver = resolver;
        this.requiredVariables = AssemblyFormatAnalyzer.GetRequiredVariables(operation);
        className = DialectGeneratorNaming.GetOperationClassName(operation);
    }

    /// <summary>
    /// Emits the full <c>BuildCustomAssemblySyntax</c> method, including signature and closing
    /// brace, into <paramref name="builder"/>.
    /// </summary>
    public static void Emit(StringBuilder builder, OperationModel operation, OperationBodySyntaxMetadata metadata, DialectSymbolResolver resolver)
    {
        var emitter = new BuildCustomAssemblySyntaxEmitter(operation, metadata, resolver);
        emitter.EmitMethod(builder);
    }

    // -----------------------------------------------------------------------
    // Method emission
    // -----------------------------------------------------------------------

    private void EmitMethod(StringBuilder builder)
    {
        builder.AppendLine("    public OperationSyntax BuildCustomAssemblySyntax(Operation operation, ConcreteSyntaxBuilderContext context)");
        builder.AppendLine("    {");

        var format = operation.AssemblyFormat!;

        if (!TryParseEmitter.CanHandleFormat(format, operation))
        {
            // Unsupported directives – fall back to generic body.
            builder.AppendLine("        return context.RewriteOperation(operation, context.TransformGenericBody(operation));");
            builder.AppendLine("    }");
            return;
        }

        builder.AppendLine("        var op = (" + className + ")operation;");

        fieldIndex = 0;

        var elements = format.Elements;
        AssemblyFormatTraversal.ForEachElement(elements, (i, element) => EmitElement(builder, element, indent: "        ", declare: true));

        EmitBodyConstruction(builder);
        builder.AppendLine("        return context.RewriteOperation(operation, body, SyntaxTokenFactory.Identifier(" + EmitterHelpers.ToCSharpStringLiteral(operation.Name) + "));");
        builder.AppendLine("    }");
    }

    // -----------------------------------------------------------------------
    // Element dispatch
    // -----------------------------------------------------------------------

    private void EmitElement(StringBuilder builder, Element element, string indent, bool declare)
    {
        switch (element)
        {
            case LiteralChunk literal:
                EmitLiteral(builder, literal, indent, declare);
                break;

            case VariableChunk variable:
                EmitVariable(builder, variable, indent, declare);
                break;

            case AttrDictDirectiveChunk _:
            case AttrDictWithKeywordDirectiveChunk _:
            case PropDictDirectiveChunk _:
                EmitAttrDict(builder, indent, declare);
                break;

            case TypeDirectiveChunk _:
            case QualifiedDirectiveChunk _:
            case ResultsDirectiveChunk _:
            case FunctionalTypeDirectiveChunk _:
                EmitType(builder, indent, declare);
                break;

            case RegionsDirectiveChunk _:
                EmitRegions(builder, indent, declare);
                break;

            case SuccessorsDirectiveChunk _:
                EmitSuccessors(builder, indent, declare);
                break;

            case OperandsDirectiveChunk _:
                EmitOperands(builder, indent, declare);
                break;

            case OptionalGroup optionalGroup:
                EmitOptionalGroup(builder, optionalGroup, indent);
                break;

            case OilistDirectiveChunk oilist:
                EmitOilist(builder, oilist, indent);
                break;
        }
    }

    // -----------------------------------------------------------------------
    // Leaf element emitters
    // -----------------------------------------------------------------------

    private void EmitLiteral(StringBuilder builder, LiteralChunk literal, string indent, bool declare)
    {
        foreach (var lit in literal.Value)
        {
            switch (lit)
            {
                case PunctuationLiteral punc:
                {
                    var field = EmitterHelpers.NextBodySyntaxField(metadata.Fields, ref fieldIndex);
                    var varName = EmitterHelpers.GetBodySyntaxFieldLocalName(field);
                    var expr = EmitterHelpers.GetSyntaxTokenFactoryExpression(punc.TokenKind);
                    builder.AppendLine(indent + DeclareOrAssign(varName, expr, declare, field.CsType) + ";");
                    break;
                }

                case KeywordLiteral kw:
                {
                    var field = EmitterHelpers.NextBodySyntaxField(metadata.Fields, ref fieldIndex);
                    var varName = EmitterHelpers.GetBodySyntaxFieldLocalName(field);
                    var expr = "SyntaxTokenFactory.Identifier(" + EmitterHelpers.ToCSharpStringLiteral(kw.Spelling) + ")";
                    builder.AppendLine(indent + DeclareOrAssign(varName, expr, declare, field.CsType) + ";");
                    break;
                }

                // WhitespaceLiteral, NewlineLiteral, EmptyLiteral → no field
            }
        }
    }

    private void EmitVariable(StringBuilder builder, VariableChunk variable, string indent, bool declare)
    {
        var field = EmitterHelpers.NextBodySyntaxField(metadata.Fields, ref fieldIndex);
        var varName = EmitterHelpers.GetBodySyntaxFieldLocalName(field);
        var expr = BuildVariableExpression(variable.Name, nullable: false);
        builder.AppendLine(indent + DeclareOrAssign(varName, expr, declare, field.CsType) + ";");
    }

    private void EmitAttrDict(StringBuilder builder, string indent, bool declare)
    {
        var field = EmitterHelpers.NextBodySyntaxField(metadata.Fields, ref fieldIndex);
        var varName = EmitterHelpers.GetBodySyntaxFieldLocalName(field);
        var attrExpr = BuildAttrDictExpression();
        builder.AppendLine(indent + DeclareOrAssign(varName, "context.BuildAttrDict(" + attrExpr + ")", declare, field.CsType) + ";");
    }

    private void EmitType(StringBuilder builder, string indent, bool declare)
    {
        var field = EmitterHelpers.NextBodySyntaxField(metadata.Fields, ref fieldIndex);
        var varName = EmitterHelpers.GetBodySyntaxFieldLocalName(field);
        builder.AppendLine(indent + DeclareOrAssign(varName, BuildTypeExpression(field), declare, field.CsType) + ";");
    }

    private void EmitRegions(StringBuilder builder, string indent, bool declare)
    {
        var field = EmitterHelpers.NextBodySyntaxField(metadata.Fields, ref fieldIndex);
        var varName = EmitterHelpers.GetBodySyntaxFieldLocalName(field);
        builder.AppendLine(indent + DeclareOrAssign(varName, "op.Regions.Select(r => context.TransformRegion(r)).ToList()", declare, field.CsType) + ";");
    }

    private void EmitSuccessors(StringBuilder builder, string indent, bool declare)
    {
        var field = EmitterHelpers.NextBodySyntaxField(metadata.Fields, ref fieldIndex);
        var varName = EmitterHelpers.GetBodySyntaxFieldLocalName(field);
        builder.AppendLine(indent + DeclareOrAssign(varName, "new DelimitedSyntaxList<SyntaxToken>(null, global::System.Array.Empty<SyntaxToken>(), global::System.Array.Empty<SyntaxToken>(), null)", declare, field.CsType) + ";");
    }

    private void EmitOperands(StringBuilder builder, string indent, bool declare)
    {
        var field = EmitterHelpers.NextBodySyntaxField(metadata.Fields, ref fieldIndex);
        var varName = EmitterHelpers.GetBodySyntaxFieldLocalName(field);
        builder.AppendLine(indent + DeclareOrAssign(varName,
            "new DelimitedSyntaxList<SyntaxToken>(SyntaxTokenFactory.LParen(), op.OperandValues.Select(v => context.NormalizeToken(v.Token ?? SyntaxTokenFactory.SsaName(v.Name))).ToList(), Enumerable.Repeat(SyntaxTokenFactory.Comma(), global::System.Math.Max(0, op.OperandValues.Count - 1)).ToList(), SyntaxTokenFactory.RParen())",
            declare, field.CsType) + ";");
    }

    // -----------------------------------------------------------------------
    // Optional group
    // -----------------------------------------------------------------------

    private void EmitOptionalGroup(StringBuilder builder, OptionalGroup group, string indent)
    {
        var thenFieldCount = AssemblyFormatTraversal.CountFields(group.ThenElements);
        var elseFieldCount = group.ElseElements != null ? AssemblyFormatTraversal.CountFields(group.ElseElements) : 0;
        var groupStart = fieldIndex;

        // Pre-declare all group fields as nullable locals initialised to their default value.
        for (var i = 0; i < thenFieldCount + elseFieldCount; i++)
        {
            var f = metadata.Fields[groupStart + i];
            // Variadic SSA-list fields use IReadOnlyList<SyntaxToken>; initialize to an empty
            // list so WriteTo can safely iterate when the optional group is not entered.
            var defaultExpr = GetFieldDefaultExpression(f.CsType);
            builder.AppendLine(indent + f.CsType + " " + EmitterHelpers.LowerFirst(f.Name) + " = " + defaultExpr + ";");
        }

        if (thenFieldCount == 0)
        {
            fieldIndex += elseFieldCount;
            return;
        }

        // Anchor condition: check whether the anchor variable is present on the typed operation.
        var anchorCondition = BuildAnchorCondition(group.AnchorName);

        builder.AppendLine(indent + "if (" + anchorCondition + ")");
        builder.AppendLine(indent + "{");

        fieldIndex = groupStart;
        AssemblyFormatTraversal.ForEachElement(group.ThenElements, (i, element) => EmitOptionalGroupElement(builder, group, element, i, indent + "    "));

        builder.AppendLine(indent + "}");

        if (group.ElseElements != null && group.ElseElements.Count > 0)
        {
            builder.AppendLine(indent + "else");
            builder.AppendLine(indent + "{");
            fieldIndex = groupStart + thenFieldCount;
            AssemblyFormatTraversal.ForEachElement(group.ElseElements, (i, element) => EmitOptionalGroupElement(builder, group, element, i, indent + "    "));

            builder.AppendLine(indent + "}");
        }

        fieldIndex = groupStart + thenFieldCount + elseFieldCount;
    }

    private void EmitOptionalGroupElement(StringBuilder builder, OptionalGroup group, Element element, int elementIndex, string indent)
    {
        switch (element)
        {
            case LiteralChunk literal:
                EmitLiteral(builder, literal, indent, declare: false);
                break;

            case VariableChunk variable:
            {
                var field = EmitterHelpers.NextBodySyntaxField(metadata.Fields, ref fieldIndex);
                var varName = EmitterHelpers.GetBodySyntaxFieldLocalName(field);
                var expr = IsImplicitUnitAttributeAnchor(group, elementIndex, variable)
                    ? "new UnitAttributeValueSyntax(" + EmitterHelpers.LowerFirst(metadata.Fields[fieldIndex - 2].Name) + ".Value)"
                    : BuildNullableVariableExpression(variable.Name);
                builder.AppendLine(indent + varName + " = " + expr + ";");
                break;
            }

            case TypeDirectiveChunk _:
            case QualifiedDirectiveChunk _:
            case ResultsDirectiveChunk _:
            case FunctionalTypeDirectiveChunk _:
            {
                var field = EmitterHelpers.NextBodySyntaxField(metadata.Fields, ref fieldIndex);
                var varName = EmitterHelpers.GetBodySyntaxFieldLocalName(field);
                builder.AppendLine(indent + varName + " = " + BuildTypeExpression(field) + ";");
                break;
            }
        }
    }

    private bool IsImplicitUnitAttributeAnchor(OptionalGroup group, int elementIndex, VariableChunk variable)
    {
        if (elementIndex == 0 || !variable.IsAnchor)
        {
            return false;
        }

        return EmitterHelpers.ContainsName(operation.Attributes, variable.Name, static attribute => attribute.Name)
            && IsUnitAttribute(variable.Name);
    }

    private bool IsUnitAttribute(string attributeName)
    {
        var constraintRecordName = EmitterHelpers.TryGetAttributeConstraint(operation, attributeName);
        return !string.IsNullOrEmpty(constraintRecordName)
            && resolver.TryResolveAttributeConstraintStrategy(constraintRecordName!).IsUnit;
    }

    // -----------------------------------------------------------------------
    // Oilist
    // -----------------------------------------------------------------------

    private void EmitOilist(StringBuilder builder, OilistDirectiveChunk oilist, string indent)
    {
        var oilistStart = fieldIndex;

        // Count total fields for all clauses.
        var totalFields = 0;
        foreach (var clause in oilist.Clauses)
        {
            totalFields += 1 + clause.Elements.Count;
        }

        // Pre-declare all oilist fields as nullable locals.
        for (var i = 0; i < totalFields; i++)
        {
            var f = metadata.Fields[oilistStart + i];
            builder.AppendLine(indent + f.CsType + " " + EmitterHelpers.LowerFirst(f.Name) + " = default;");
        }

        fieldIndex = oilistStart;
        foreach (var clause in oilist.Clauses)
        {
            var trigger = GetOilistClauseTrigger(clause);
            var kwField = metadata.Fields[fieldIndex];
            var kwVarName = EmitterHelpers.LowerFirst(kwField.Name);

            if (trigger != null)
            {
                builder.AppendLine(indent + "if (" + trigger + ")");
                builder.AppendLine(indent + "{");
                fieldIndex++; // advance past keyword field
                builder.AppendLine(indent + "    " + kwVarName + " = SyntaxTokenFactory.Identifier(" + EmitterHelpers.ToCSharpStringLiteral(clause.Keyword) + ");");

                AssemblyFormatTraversal.ForEachElement(clause.Elements, (_, element) => EmitOilistElement(builder, element, indent + "    "));

                builder.AppendLine(indent + "}");
            }
            else
            {
                // No trigger – skip the clause: advance past keyword + element fields.
                fieldIndex++;
                fieldIndex += clause.Elements.Count;
            }
        }

        fieldIndex = oilistStart + totalFields;
    }

    private void EmitOilistElement(StringBuilder builder, OilistElement element, string indent)
    {
        switch (element)
        {
            case OilistVariableElement variable:
            {
                var field = EmitterHelpers.NextBodySyntaxField(metadata.Fields, ref fieldIndex);
                var varName = EmitterHelpers.GetBodySyntaxFieldLocalName(field);
                var expr = BuildNullableVariableExpression(variable.Name);
                builder.AppendLine(indent + varName + " = " + expr + ";");
                break;
            }

            case OilistTypeDirectiveElement _:
            {
                var field = EmitterHelpers.NextBodySyntaxField(metadata.Fields, ref fieldIndex);
                var varName = EmitterHelpers.GetBodySyntaxFieldLocalName(field);
                builder.AppendLine(indent + varName + " = " + BuildTypeExpression(field) + ";");
                break;
            }

            case OilistLiteralElement literal:
            {
                var field = EmitterHelpers.NextBodySyntaxField(metadata.Fields, ref fieldIndex);
                var varName = EmitterHelpers.GetBodySyntaxFieldLocalName(field);
                builder.AppendLine(indent + varName + " = SyntaxTokenFactory.Identifier(" + EmitterHelpers.ToCSharpStringLiteral(literal.Value) + ");");
                break;
            }
        }
    }

    // -----------------------------------------------------------------------
    // Body construction
    // -----------------------------------------------------------------------

    private void EmitBodyConstruction(StringBuilder builder)
    {
        var bodyClassName = className + "BodySyntax";
        if (metadata.Fields.Count == 0)
        {
            builder.AppendLine("        var body = new " + bodyClassName + "();");
            return;
        }

        builder.Append("        var body = new " + bodyClassName + "(");
        for (var i = 0; i < metadata.Fields.Count; i++)
        {
            if (i > 0)
            {
                builder.Append(", ");
            }

            builder.Append(EmitterHelpers.LowerFirst(metadata.Fields[i].Name));
        }

        builder.AppendLine(");");
    }

    // -----------------------------------------------------------------------
    // Expression builders
    // -----------------------------------------------------------------------

    /// <summary>
    /// Builds an expression to reconstruct a required (non-nullable) variable field.
    /// For operands/results: produces a <c>SyntaxToken</c> from the value reference.
    /// For attributes: produces an <c>AttributeValueSyntax</c> by building the attribute value.
    /// </summary>
    private string BuildVariableExpression(string variableName, bool nullable)
    {
        var propName = DialectGeneratorNaming.ToPascalCase(variableName);
        if (EmitterHelpers.ContainsName(operation.Attributes, variableName, static attribute => attribute.Name))
        {
            // Required attribute: access via Attributes collection to get the AttributeValue,
            // independent of the narrowed property type.
            return "context.BuildAttributeValueSyntax(op.Attributes[" + EmitterHelpers.ToCSharpStringLiteral(variableName) + "].Value)";
        }

        if (EmitterHelpers.ContainsName(operation.Regions, variableName, static region => region.Name))
        {
            if (IsVariadicRegion(variableName))
            {
                return "op." + propName + ".Select(region => context.TransformRegion(region)).ToList()";
            }

            if (nullable)
            {
                return "op." + propName + " is not null ? context.TransformRegion(op." + propName + ") : null";
            }

            return "context.TransformRegion(op." + propName + ")";
        }

        // Check if this is a variadic operand (the generated property returns IReadOnlyList<Value>).
        if (IsVariadicOperand(variableName))
        {
            // Produce a List<SyntaxToken> from the variadic value list.
            return "op." + propName + ".Select(v => context.NormalizeToken(v.Token ?? SyntaxTokenFactory.SsaName(v.Name))).ToList()";
        }

        if (nullable)
        {
            // Nullable operand: op.Rhs is Value?
            return "context.NormalizeToken(op." + propName + "!.Token ?? SyntaxTokenFactory.SsaName(op." + propName + "!.Name))";
        }
        else
        {
            // Required operand/result: op.Lhs is Value (non-nullable)
            return "context.NormalizeToken(op." + propName + ".Token ?? SyntaxTokenFactory.SsaName(op." + propName + ".Name))";
        }
    }

    private bool IsVariadicOperand(string variableName)
    {
        foreach (var operand in operation.Operands)
        {
            if (string.Equals(operand.Name, variableName, StringComparison.Ordinal))
            {
                return operand.IsVariadic;
            }
        }

        return false;
    }

    private bool IsVariadicRegion(string variableName)
    {
        foreach (var region in operation.Regions)
        {
            if (string.Equals(region.Name, variableName, StringComparison.Ordinal))
            {
                return region.IsVariadic;
            }
        }

        return false;
    }

    /// <summary>
    /// Builds an expression to reconstruct a nullable variable field (inside an optional group
    /// or oilist clause, where the containing guard has already confirmed presence).
    /// </summary>
    private string BuildNullableVariableExpression(string variableName)
    {
        return BuildVariableExpression(variableName, nullable: true);
    }

    private string BuildTypeExpression(BodySyntaxField field)
    {
        if (field.CsType == "IReadOnlyList<TypeSyntax>")
        {
            return "op.TypeSignatureReference?.Syntax is global::MLIR.Syntax.Types.Collections.FunctionTypeSyntax functionType ? " +
                "(global::System.Collections.Generic.IReadOnlyList<global::MLIR.Syntax.TypeSyntax>)functionType.InputTypes.Items.ToList() : global::System.Array.Empty<global::MLIR.Syntax.TypeSyntax>()";
        }

        return "op.TypeSignatureReference?.Syntax ?? new RawTypeSyntax(new RawSyntaxText(\"?\"))";
    }

    private static string GetFieldDefaultExpression(string csType)
    {
        if (csType.Contains("IReadOnlyList<SyntaxToken>", StringComparison.Ordinal))
        {
            return "global::System.Array.Empty<SyntaxToken>()";
        }

        if (csType.Contains("IReadOnlyList<TypeSyntax>", StringComparison.Ordinal))
        {
            return "global::System.Array.Empty<global::MLIR.Syntax.TypeSyntax>()";
        }

        if (csType.Contains("IReadOnlyList<RegionSyntax>", StringComparison.Ordinal))
        {
            return "global::System.Array.Empty<global::MLIR.Syntax.RegionSyntax>()";
        }

        return "default";
    }

    /// <summary>
    /// Builds the expression to pass to <c>context.BuildAttrDict</c>, removing any attributes
    /// that are already explicitly represented by named variables in the assembly format.
    /// </summary>
    private string BuildAttrDictExpression()
    {
        var named = new List<string>();
        CollectExplicitAttributes(operation.AssemblyFormat!.Elements, named);

        var expr = "op.Attributes";
        foreach (var name in named)
        {
            expr += ".Remove(" + EmitterHelpers.ToCSharpStringLiteral(name) + ")";
        }

        return expr;

        void CollectExplicitAttributes(IReadOnlyList<Element> elements, List<string> result)
        {
            AssemblyFormatTraversal.ForEachElement(elements, (_, element) =>
            {
                if (element is VariableChunk var && EmitterHelpers.ContainsName(operation.Attributes, var.Name, static attribute => attribute.Name))
                {
                    result.Add(var.Name);
                }
                else if (element is OptionalGroup og)
                {
                    CollectExplicitAttributes(og.ThenElements, result);
                    if (og.ElseElements != null)
                    {
                        CollectExplicitAttributes(og.ElseElements, result);
                    }
                }
                else if (element is OilistDirectiveChunk oilist)
                {
                    foreach (var clause in oilist.Clauses)
                    {
                        AssemblyFormatTraversal.ForEachElement(clause.Elements, (_, clauseElement) =>
                        {
                            if (clauseElement is OilistVariableElement ov && EmitterHelpers.ContainsName(operation.Attributes, ov.Name, static attribute => attribute.Name))
                            {
                                result.Add(ov.Name);
                            }
                        });
                    }
                }
            });
        }
    }

    /// <summary>
    /// Builds the anchor presence condition for an optional group.
    /// </summary>
    private string BuildAnchorCondition(string anchorName)
    {
        var propName = DialectGeneratorNaming.ToPascalCase(anchorName);
        if (EmitterHelpers.ContainsName(operation.Attributes, anchorName, static attribute => attribute.Name))
        {
            // Non-required UnitAttributes are represented as 'bool' (a value type).
            // Comparing a bool to null always evaluates to true (CS0472), so emit the
            // value directly without '!= null'.
            if (IsUnitAttribute(anchorName) && !requiredVariables.Contains(anchorName))
            {
                return "op." + propName;
            }

            return "op." + propName + " != null";
        }
        else if (EmitterHelpers.ContainsName(operation.Regions, anchorName, static region => region.Name))
        {
            if (IsVariadicRegion(anchorName))
            {
                return "op." + propName + ".Count > 0";
            }

            return "op." + propName + " != null";
        }
        else if (IsVariadicOperand(anchorName))
        {
            // Variadic operands are always present as a list; the anchor is true when there is
            // at least one element (the optional group should only be emitted when non-empty).
            return "op." + propName + ".Count > 0";
        }
        else
        {
            // Operand or result: Value?
            return "op." + propName + " != null";
        }
    }

    /// <summary>
    /// Returns a C# condition expression that is true when an oilist clause should be emitted,
    /// based on the presence of the first variable element found in the clause.
    /// Returns <see langword="null"/> when no suitable trigger variable exists.
    /// </summary>
    private string? GetOilistClauseTrigger(OilistClause clause)
    {
        foreach (var elem in clause.Elements)
        {
            if (elem is OilistVariableElement variable)
            {
                var propName = DialectGeneratorNaming.ToPascalCase(variable.Name);
                if (EmitterHelpers.ContainsName(operation.Attributes, variable.Name, static attribute => attribute.Name))
                {
                    // Non-required UnitAttributes are 'bool': emit the value directly.
                    if (IsUnitAttribute(variable.Name) && !requiredVariables.Contains(variable.Name))
                    {
                        return "op." + propName;
                    }

                    return "op." + propName + " != null";
                }
                else
                {
                    return "op." + propName + " != null";
                }
            }
        }

        return null;
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private static string DeclareOrAssign(string varName, string expr, bool declare, string csType)
    {
        return declare ? "var " + varName + " = " + expr : varName + " = " + expr;
    }

}
