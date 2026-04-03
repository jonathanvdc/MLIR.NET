namespace MLIR.Generators.Emitters;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        for (var i = 0; i < elements.Count; i++)
        {
            EmitElement(builder, elements[i], indent: "        ", declare: true);
        }

        EmitBodyConstruction(builder);
        builder.AppendLine("        return context.RewriteOperation(operation, body, new SyntaxToken(operation.Name));");
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
                    var field = NextField();
                    var varName = EmitterHelpers.LowerFirst(field.Name);
                    var text = EmitterHelpers.ToCSharpStringLiteral(GetPunctuationText(punc.TokenKind));
                    builder.AppendLine(indent + DeclareOrAssign(varName, "new SyntaxToken(" + text + ")", declare, field.CsType) + ";");
                    break;
                }

                case KeywordLiteral kw:
                {
                    var field = NextField();
                    var varName = EmitterHelpers.LowerFirst(field.Name);
                    var text = EmitterHelpers.ToCSharpStringLiteral(kw.Spelling);
                    builder.AppendLine(indent + DeclareOrAssign(varName, "new SyntaxToken(" + text + ")", declare, field.CsType) + ";");
                    break;
                }

                // WhitespaceLiteral, NewlineLiteral, EmptyLiteral → no field
            }
        }
    }

    private void EmitVariable(StringBuilder builder, VariableChunk variable, string indent, bool declare)
    {
        var field = NextField();
        var varName = EmitterHelpers.LowerFirst(field.Name);
        var expr = BuildVariableExpression(variable.Name, nullable: false);
        builder.AppendLine(indent + DeclareOrAssign(varName, expr, declare, field.CsType) + ";");
    }

    private void EmitAttrDict(StringBuilder builder, string indent, bool declare)
    {
        var field = NextField();
        var varName = EmitterHelpers.LowerFirst(field.Name);
        var attrExpr = BuildAttrDictExpression();
        builder.AppendLine(indent + DeclareOrAssign(varName, "context.BuildAttrDict(" + attrExpr + ")", declare, field.CsType) + ";");
    }

    private void EmitType(StringBuilder builder, string indent, bool declare)
    {
        var field = NextField();
        var varName = EmitterHelpers.LowerFirst(field.Name);
        builder.AppendLine(indent + DeclareOrAssign(varName, "op.TypeSignatureReference?.Syntax ?? new RawTypeSyntax(new RawSyntaxText(\"?\"))", declare, field.CsType) + ";");
    }

    private void EmitRegions(StringBuilder builder, string indent, bool declare)
    {
        var field = NextField();
        var varName = EmitterHelpers.LowerFirst(field.Name);
        builder.AppendLine(indent + DeclareOrAssign(varName, "op.Regions.Select(r => context.TransformRegion(r)).ToList()", declare, field.CsType) + ";");
    }

    private void EmitSuccessors(StringBuilder builder, string indent, bool declare)
    {
        var field = NextField();
        var varName = EmitterHelpers.LowerFirst(field.Name);
        builder.AppendLine(indent + DeclareOrAssign(varName, "new DelimitedSyntaxList<SyntaxToken>(null, global::System.Array.Empty<SyntaxToken>(), global::System.Array.Empty<SyntaxToken>(), null)", declare, field.CsType) + ";");
    }

    private void EmitOperands(StringBuilder builder, string indent, bool declare)
    {
        var field = NextField();
        var varName = EmitterHelpers.LowerFirst(field.Name);
        builder.AppendLine(indent + DeclareOrAssign(varName,
            "new DelimitedSyntaxList<SyntaxToken>(new SyntaxToken(\"(\"), op.OperandValues.Select(v => v.Token ?? new SyntaxToken(v.Name)).ToList(), Enumerable.Repeat(new SyntaxToken(\",\"), global::System.Math.Max(0, op.OperandValues.Count - 1)).ToList(), new SyntaxToken(\")\"))",
            declare, field.CsType) + ";");
    }

    // -----------------------------------------------------------------------
    // Optional group
    // -----------------------------------------------------------------------

    private void EmitOptionalGroup(StringBuilder builder, OptionalGroup group, string indent)
    {
        var thenFieldCount = CountInnerFields(group.ThenElements);
        var elseFieldCount = group.ElseElements != null ? CountInnerFields(group.ElseElements) : 0;
        var groupStart = fieldIndex;

        // Pre-declare all group fields as nullable locals initialised to their default value.
        for (var i = 0; i < thenFieldCount + elseFieldCount; i++)
        {
            var f = metadata.Fields[groupStart + i];
            builder.AppendLine(indent + f.CsType + " " + EmitterHelpers.LowerFirst(f.Name) + " = default;");
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
        for (var i = 0; i < group.ThenElements.Count; i++)
        {
            EmitOptionalGroupElement(builder, group, group.ThenElements[i], i, indent + "    ");
        }

        builder.AppendLine(indent + "}");

        if (group.ElseElements != null && group.ElseElements.Count > 0)
        {
            builder.AppendLine(indent + "else");
            builder.AppendLine(indent + "{");
            fieldIndex = groupStart + thenFieldCount;
            for (var i = 0; i < group.ElseElements.Count; i++)
            {
                EmitOptionalGroupElement(builder, group, group.ElseElements[i], i, indent + "    ");
            }

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
                var field = NextField();
                var varName = EmitterHelpers.LowerFirst(field.Name);
                var expr = IsImplicitUnitAttributeAnchor(group, elementIndex, variable)
                    ? "new UnitAttributeValueSyntax(" + EmitterHelpers.LowerFirst(metadata.Fields[fieldIndex - 2].Name) + ".Value)"
                    : BuildNullableVariableExpression(variable.Name);
                builder.AppendLine(indent + varName + " = " + expr + ";");
                break;
            }

            case TypeDirectiveChunk _:
            case QualifiedDirectiveChunk _:
            case ResultsDirectiveChunk _:
            {
                var field = NextField();
                var varName = EmitterHelpers.LowerFirst(field.Name);
                builder.AppendLine(indent + varName + " = op.TypeSignatureReference?.Syntax ?? new RawTypeSyntax(new RawSyntaxText(\"?\"));");
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
            && resolver.TryResolveAttributeConstraintKind(constraintRecordName!) == AttributeConstraintKind.UnitAttribute;
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
                builder.AppendLine(indent + "    " + kwVarName + " = new SyntaxToken(" + EmitterHelpers.ToCSharpStringLiteral(clause.Keyword) + ");");

                foreach (var elem in clause.Elements)
                {
                    EmitOilistElement(builder, elem, indent + "    ");
                }

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
                var field = NextField();
                var varName = EmitterHelpers.LowerFirst(field.Name);
                var expr = BuildNullableVariableExpression(variable.Name);
                builder.AppendLine(indent + varName + " = " + expr + ";");
                break;
            }

            case OilistTypeDirectiveElement _:
            {
                var field = NextField();
                var varName = EmitterHelpers.LowerFirst(field.Name);
                builder.AppendLine(indent + varName + " = op.TypeSignatureReference?.Syntax ?? new RawTypeSyntax(new RawSyntaxText(\"?\"));");
                break;
            }

            case OilistLiteralElement literal:
            {
                var field = NextField();
                var varName = EmitterHelpers.LowerFirst(field.Name);
                builder.AppendLine(indent + varName + " = new SyntaxToken(" + EmitterHelpers.ToCSharpStringLiteral(literal.Value) + ");");
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

        // Check if this is a variadic operand (the generated property returns IReadOnlyList<Value>).
        if (IsVariadicOperand(variableName))
        {
            // Produce a List<SyntaxToken> from the variadic value list.
            return "op." + propName + ".Select(v => v.Token ?? new SyntaxToken(v.Name)).ToList()";
        }

        if (nullable)
        {
            // Nullable operand: op.Rhs is Value?
            return "op." + propName + "!.Token ?? new SyntaxToken(op." + propName + "!.Name)";
        }
        else
        {
            // Required operand/result: op.Lhs is Value (non-nullable)
            return "op." + propName + ".Token ?? new SyntaxToken(op." + propName + ".Name)";
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

    /// <summary>
    /// Builds an expression to reconstruct a nullable variable field (inside an optional group
    /// or oilist clause, where the containing guard has already confirmed presence).
    /// </summary>
    private string BuildNullableVariableExpression(string variableName)
    {
        return BuildVariableExpression(variableName, nullable: true);
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

    private BodySyntaxField NextField()
    {
        return metadata.Fields[fieldIndex++];
    }

    private static string DeclareOrAssign(string varName, string expr, bool declare, string csType)
    {
        return declare ? "var " + varName + " = " + expr : varName + " = " + expr;
    }

    private static int CountInnerFields(IReadOnlyList<Element> elements)
    {
        var count = 0;
        foreach (var e in elements)
        {
            count += CountFieldsForElement(e);
        }

        return count;
    }

    private static int CountFieldsForElement(Element element)
    {
        switch (element)
        {
            case LiteralChunk literal:
            {
                var n = 0;
                foreach (var lit in literal.Value)
                {
                    if (lit is PunctuationLiteral || lit is KeywordLiteral)
                    {
                        n++;
                    }
                }

                return n;
            }

            case VariableChunk _: return 1;
            case AttrDictDirectiveChunk _: return 1;
            case AttrDictWithKeywordDirectiveChunk _: return 1;
            case PropDictDirectiveChunk _: return 1;
            case TypeDirectiveChunk _: return 1;
            case QualifiedDirectiveChunk _: return 1;
            case ResultsDirectiveChunk _: return 1;
            case RegionsDirectiveChunk _: return 1;
            case SuccessorsDirectiveChunk _: return 1;
            case OperandsDirectiveChunk _: return 1;
            case OptionalGroup group:
                return CountInnerFields(group.ThenElements) +
                       (group.ElseElements != null ? CountInnerFields(group.ElseElements) : 0);
            default: return 0;
        }
    }

    private void CollectExplicitAttributes(IReadOnlyList<Element> elements, List<string> result)
    {
        foreach (var element in elements)
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
                    foreach (var elem in clause.Elements)
                    {
                        if (elem is OilistVariableElement ov && EmitterHelpers.ContainsName(operation.Attributes, ov.Name, static attribute => attribute.Name))
                        {
                            result.Add(ov.Name);
                        }
                    }
                }
            }
        }
    }

    private static string GetPunctuationText(TokenKind tokenKind)
    {
        return tokenKind switch
        {
            TokenKind.Comma => ",",
            TokenKind.LParen => "(",
            TokenKind.RParen => ")",
            TokenKind.LBracket => "[",
            TokenKind.RBracket => "]",
            TokenKind.LBrace => "{",
            TokenKind.RBrace => "}",
            TokenKind.Arrow => "->",
            TokenKind.Colon => ":",
            TokenKind.Equal => "=",
            TokenKind.LessThan => "<",
            TokenKind.GreaterThan => ">",
            TokenKind.Question => "?",
            TokenKind.Star => "*",
            TokenKind.Plus => "+",
            TokenKind.Minus => "-",
            TokenKind.Dot => ".",
            TokenKind.At => "@",
            TokenKind.Hash => "#",
            _ => tokenKind.ToString()
        };
    }
}
