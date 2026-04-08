namespace MLIR.Generators.Emitters.AssemblyFormat;

using System.Collections.Generic;
using System.Text;
using MLIR.Generators.Emitters;
using MLIR.Generators.Emitters.Common;
using MLIR.Generators.Emitters.Operation;
using MLIR.ODS.Model;
using MLIR.ODS.Model.AssemblyFormat;
using MLIR.Text;

/// <summary>
/// Generates the <c>TryParse</c> method body for a declarative assembly format.
/// </summary>
/// <remarks>
/// Each supported assembly format element is translated into a call on
/// <c>MLIR.Text.OperationParsingContext</c>. After all elements have been parsed the generated
/// code constructs the typed <c>OperationBodySyntax</c> subclass and returns a parsed result.
///
/// Formats that contain directives that are not yet supported produce a fallback
/// implementation that immediately returns a no-match result so that the parser falls back to the
/// generic format.
/// </remarks>
internal sealed class TryParseEmitter
{
    private readonly OperationModel operation;
    private readonly OperationBodySyntaxMetadata metadata;
    private readonly string className;
    private readonly DialectSymbolResolver resolver;
    private int fieldIndex;

    private TryParseEmitter(OperationModel operation, OperationBodySyntaxMetadata metadata, DialectSymbolResolver resolver)
    {
        this.operation = operation;
        this.metadata = metadata;
        this.resolver = resolver;
        className = DialectGeneratorNaming.GetOperationClassName(operation);
        fieldIndex = 0;
    }

    /// <summary>
    /// Emits the full <c>TryParse</c> method, including signature and closing brace, into
    /// <paramref name="builder"/>.
    /// </summary>
    public static void Emit(StringBuilder builder, OperationModel operation, OperationBodySyntaxMetadata metadata, DialectSymbolResolver resolver)
    {
        var emitter = new TryParseEmitter(operation, metadata, resolver);
        emitter.EmitMethod(builder);
    }

    // -----------------------------------------------------------------------
    // Public surface – determines whether a format is fully supported.
    // -----------------------------------------------------------------------

    /// <summary>
    /// Returns <see langword="true"/> when every element in <paramref name="format"/>
    /// can be translated into a parsing statement.
    /// </summary>
    public static bool CanHandleFormat(AssemblyFormatModel format, OperationModel operation)
    {
        foreach (var element in format.Elements)
        {
            if (!CanHandleElement(element))
            {
                return false;
            }
        }

        return true;
    }

    // -----------------------------------------------------------------------
    // Method emission
    // -----------------------------------------------------------------------

    private void EmitMethod(StringBuilder builder)
    {
        builder.AppendLine("    public ParseResult<OperationBodySyntax> TryParse(SyntaxToken nameToken, SeparatedSyntaxList<SyntaxToken> resultList, SyntaxToken? equalsToken, OperationParsingContext context)");
        builder.AppendLine("    {");

        var format = operation.AssemblyFormat!;

        if (!CanHandleFormat(format, operation))
        {
            // Unsupported directives – fall back to generic parsing.
            builder.AppendLine("        return ParseResult<OperationBodySyntax>.NoMatch();");
            builder.AppendLine("    }");
            return;
        }

        fieldIndex = 0;

        var elements = format.Elements;
        AssemblyFormatTraversal.ForEachElement(elements, (i, element) => EmitElement(builder, element, i, elements, indent: "        ", declare: true));

        EmitBodyConstruction(builder);
        builder.AppendLine("    }");
    }

    // -----------------------------------------------------------------------
    // Element dispatch
    // -----------------------------------------------------------------------

    /// <param name="builder">The <see cref="StringBuilder"/> to append generated code to.</param>
    /// <param name="element">The assembly-format element to emit parsing code for.</param>
    /// <param name="elementIndex">The index of <paramref name="element"/> within <paramref name="allElements"/>.</param>
    /// <param name="allElements">All sibling elements in the current sequence (used for lookahead context).</param>
    /// <param name="indent">The indentation string to prepend to each emitted line.</param>
    /// <param name="declare">
    /// When <see langword="true"/>, emit <c>var name = expr;</c>.
    /// When <see langword="false"/>, emit <c>name = expr;</c> (assignment into a pre-declared nullable local).
    /// </param>
    private void EmitElement(StringBuilder builder, Element element, int elementIndex, IReadOnlyList<Element> allElements, string indent, bool declare)
    {
        switch (element)
        {
            case LiteralChunk literal:
                EmitLiteral(builder, literal, indent, declare);
                break;
            case VariableChunk variable:
                EmitVariable(builder, variable, elementIndex, allElements, indent, declare);
                break;
            case AttrDictDirectiveChunk _:
                EmitAttrDict(builder, indent, declare);
                break;
            case AttrDictWithKeywordDirectiveChunk _:
                EmitAttrDictWithKeyword(builder, indent, declare);
                break;
            case PropDictDirectiveChunk _:
                EmitPropDict(builder, indent, declare);
                break;
            case TypeDirectiveChunk typeDir:
                EmitType(builder, typeDir, elementIndex, allElements, indent, declare);
                break;
            case QualifiedDirectiveChunk qualified:
                EmitQualifiedType(builder, qualified, elementIndex, allElements, indent, declare);
                break;
            case ResultsDirectiveChunk _:
                EmitResultsType(builder, elementIndex, allElements, indent, declare);
                break;
            case FunctionalTypeDirectiveChunk functionalType:
                EmitFunctionalType(builder, functionalType, elementIndex, allElements, indent, declare);
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
                EmitOptionalGroup(builder, optionalGroup, elementIndex, allElements, indent);
                break;
            case OilistDirectiveChunk oilist:
                EmitOilist(builder, oilist, indent);
                break;
        }
    }

    // -----------------------------------------------------------------------
    // Per-element emission – mandatory (non-optional) elements
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
                    var expr = "context.Expect(TokenKind." + punc.TokenKind +
                               ", \"Expected '" + EmitterHelpers.EscapeForStringLiteral(EmitterHelpers.GetPunctuationText(punc.TokenKind)) + "'.\")";
                    EmitParseResultAssignment(builder, indent, varName, expr, declare, field.CsType);
                    break;
                }

                case KeywordLiteral kw:
                {
                    var field = EmitterHelpers.NextBodySyntaxField(metadata.Fields, ref fieldIndex);
                    var varName = EmitterHelpers.GetBodySyntaxFieldLocalName(field);
                    var expr = "context.ExpectKeyword(" +
                               EmitterHelpers.ToCSharpStringLiteral(kw.Spelling) +
                               ", \"Expected '" + EmitterHelpers.EscapeForStringLiteral(kw.Spelling) + "'.\")";
                    EmitParseResultAssignment(builder, indent, varName, expr, declare, field.CsType);
                    break;
                }

                // WhitespaceLiteral, NewlineLiteral, EmptyLiteral: no field, no parse call
            }
        }
    }

    private void EmitVariable(StringBuilder builder, VariableChunk variable, int elementIndex, IReadOnlyList<Element> allElements, string indent, bool declare)
    {
        var field = EmitterHelpers.NextBodySyntaxField(metadata.Fields, ref fieldIndex);
        var varName = EmitterHelpers.GetBodySyntaxFieldLocalName(field);

        if (EmitterHelpers.ContainsName(operation.Attributes, variable.Name, static attribute => attribute.Name))
        {
            var delimiters = AssemblyFormatTraversal.FindNextPunctuationDelimiters(elementIndex, allElements);
            var expectedConstraint = EmitterHelpers.TryGetAttributeConstraint(operation, variable.Name);
            var expr = BuildAttributeParseExpr(expectedConstraint, delimiters);
            EmitParseResultAssignment(builder, indent, varName, expr, declare, field.CsType);
        }
        else
        {
            if (EmitterHelpers.ContainsName(operation.Regions, variable.Name, static region => region.Name))
            {
                var isVariadicRegion = false;
                foreach (var region in operation.Regions)
                {
                    if (string.Equals(region.Name, variable.Name, System.StringComparison.Ordinal))
                    {
                        isVariadicRegion = region.IsVariadic;
                        break;
                    }
                }

                if (isVariadicRegion)
                {
                    EmitParseResultAssignment(builder, indent, varName, "context.TryParseRegions()", declare, field.CsType);
                }
                else
                {
                    EmitParseResultAssignment(builder, indent, varName, "context.TryParseRegion()", declare, field.CsType);
                }

                return;
            }

            var isVariadic = false;
            foreach (var operand in operation.Operands)
            {
                if (string.Equals(operand.Name, variable.Name, System.StringComparison.Ordinal))
                {
                    isVariadic = operand.IsVariadic;
                    break;
                }
            }

            if (isVariadic)
            {
                EmitParseResultAssignment(builder, indent, varName, "context.TryParseSsaTokenList()", declare, field.CsType);
            }
            else
            {
                EmitParseResultAssignment(builder, indent, varName, "context.TryParseSsaToken()", declare, field.CsType);
            }
        }
    }

    private void EmitAttrDict(StringBuilder builder, string indent, bool declare)
    {
        var field = EmitterHelpers.NextBodySyntaxField(metadata.Fields, ref fieldIndex);
        var varName = EmitterHelpers.GetBodySyntaxFieldLocalName(field);
        EmitParseResultAssignment(builder, indent, varName, "context.TryParseAttrDict()", declare, field.CsType);
    }

    private void EmitAttrDictWithKeyword(StringBuilder builder, string indent, bool declare)
    {
        var field = EmitterHelpers.NextBodySyntaxField(metadata.Fields, ref fieldIndex);
        var varName = EmitterHelpers.GetBodySyntaxFieldLocalName(field);
        EmitParseResultAssignment(builder, indent, varName, "context.TryParseAttrDictWithKeyword()", declare, field.CsType);
    }

    private void EmitPropDict(StringBuilder builder, string indent, bool declare)
    {
        var field = EmitterHelpers.NextBodySyntaxField(metadata.Fields, ref fieldIndex);
        var varName = EmitterHelpers.GetBodySyntaxFieldLocalName(field);
        EmitParseResultAssignment(builder, indent, varName, "context.TryParseAttrDict()", declare, field.CsType);
    }

    private void EmitType(StringBuilder builder, TypeDirectiveChunk typeDir, int elementIndex, IReadOnlyList<Element> allElements, string indent, bool declare)
    {
        var field = EmitterHelpers.NextBodySyntaxField(metadata.Fields, ref fieldIndex);
        var varName = EmitterHelpers.GetBodySyntaxFieldLocalName(field);
        var expr = BuildTypeParseExpr(elementIndex, allElements);
        EmitTypeAssignment(builder, indent, varName, expr, declare, field.CsType);
    }

    private void EmitQualifiedType(StringBuilder builder, QualifiedDirectiveChunk qualified, int elementIndex, IReadOnlyList<Element> allElements, string indent, bool declare)
    {
        // qualified(...) does not change parsing behaviour; treat the same as a plain type.
        var field = EmitterHelpers.NextBodySyntaxField(metadata.Fields, ref fieldIndex);
        var varName = EmitterHelpers.GetBodySyntaxFieldLocalName(field);
        var expr = BuildTypeParseExpr(elementIndex, allElements);
        EmitTypeAssignment(builder, indent, varName, expr, declare, field.CsType);
    }

    private void EmitResultsType(StringBuilder builder, int elementIndex, IReadOnlyList<Element> allElements, string indent, bool declare)
    {
        var field = EmitterHelpers.NextBodySyntaxField(metadata.Fields, ref fieldIndex);
        var varName = EmitterHelpers.GetBodySyntaxFieldLocalName(field);
        var expr = BuildTypeParseExpr(elementIndex, allElements);
        EmitTypeAssignment(builder, indent, varName, expr, declare, field.CsType);
    }

    private void EmitFunctionalType(StringBuilder builder, FunctionalTypeDirectiveChunk functionalType, int elementIndex, IReadOnlyList<Element> allElements, string indent, bool declare)
    {
        var field = EmitterHelpers.NextBodySyntaxField(metadata.Fields, ref fieldIndex);
        var varName = EmitterHelpers.GetBodySyntaxFieldLocalName(field);
        var expr = BuildTypeParseExpr(elementIndex, allElements);
        EmitTypeAssignment(builder, indent, varName, expr, declare, field.CsType);
    }

    private void EmitRegions(StringBuilder builder, string indent, bool declare)
    {
        var field = EmitterHelpers.NextBodySyntaxField(metadata.Fields, ref fieldIndex);
        var varName = EmitterHelpers.GetBodySyntaxFieldLocalName(field);
        EmitParseResultAssignment(builder, indent, varName, "context.TryParseRegions()", declare, field.CsType);
    }

    private void EmitSuccessors(StringBuilder builder, string indent, bool declare)
    {
        var field = EmitterHelpers.NextBodySyntaxField(metadata.Fields, ref fieldIndex);
        var varName = EmitterHelpers.GetBodySyntaxFieldLocalName(field);
        EmitParseResultAssignment(builder, indent, varName, "context.TryParseSuccessors()", declare, field.CsType);
    }

    private void EmitOperands(StringBuilder builder, string indent, bool declare)
    {
        var field = EmitterHelpers.NextBodySyntaxField(metadata.Fields, ref fieldIndex);
        var varName = EmitterHelpers.GetBodySyntaxFieldLocalName(field);
        EmitParseResultAssignment(builder, indent, varName, "context.TryParseOperands()", declare, field.CsType);
    }

    // -----------------------------------------------------------------------
    // Optional group – conditional parsing
    // -----------------------------------------------------------------------

    private void EmitOptionalGroup(StringBuilder builder, OptionalGroup group, int elementIndex, IReadOnlyList<Element> allElements, string indent)
    {
        var thenFieldCount = AssemblyFormatTraversal.CountFields(group.ThenElements);
        var elseFieldCount = group.ElseElements != null ? AssemblyFormatTraversal.CountFields(group.ElseElements) : 0;
        var groupStart = fieldIndex;

        // Pre-declare all group fields as nullable locals initialised to their default value.
        for (var i = 0; i < thenFieldCount + elseFieldCount; i++)
        {
            var f = metadata.Fields[groupStart + i];
            // Variadic SSA-list fields use IReadOnlyList<SyntaxToken>; initialize to an empty
            // array rather than null so callers can always iterate over the result safely.
            var defaultExpr = GetFieldDefaultExpression(f.CsType);
            builder.AppendLine(indent + f.CsType + " " + EmitterHelpers.LowerFirst(f.Name) + " = " + defaultExpr + ";");
        }

        if (thenFieldCount == 0)
        {
            fieldIndex += elseFieldCount;
            return;
        }

        // Build the guard for the optional group.
        fieldIndex = groupStart;
        var (guardExpr, firstFieldAssignExpr) = BuildOptionalGroupGuard(group.ThenElements);

        if (guardExpr == null)
        {
            // No supported guard – skip the group; fields stay at their default values.
            fieldIndex = groupStart + thenFieldCount + elseFieldCount;
            return;
        }

        builder.AppendLine(indent + "if (" + guardExpr + ")");
        builder.AppendLine(indent + "{");

        if (firstFieldAssignExpr != null)
        {
            // The guard evaluated using TryMatch which consumed and produced the first token.
            // Assign the first field from the local the TryMatch already populated.
            var firstField = metadata.Fields[groupStart];
            builder.AppendLine(indent + "    " + EmitterHelpers.LowerFirst(firstField.Name) + " = " + firstFieldAssignExpr + ";");
            fieldIndex = groupStart + 1;
            // Emit remaining then-elements as assignments.
            AssemblyFormatTraversal.ForEachElement(group.ThenElements.Skip(1).ToList(), (i, element) => EmitOptionalGroupElement(builder, group, element, i + 1, group.ThenElements, indent + "    "));
        }
        else
        {
            fieldIndex = groupStart;
            AssemblyFormatTraversal.ForEachElement(group.ThenElements, (i, element) => EmitOptionalGroupElement(builder, group, element, i, group.ThenElements, indent + "    "));
        }

        builder.AppendLine(indent + "}");

        if (group.ElseElements != null && group.ElseElements.Count > 0)
        {
            builder.AppendLine(indent + "else");
            builder.AppendLine(indent + "{");
            fieldIndex = groupStart + thenFieldCount;
            AssemblyFormatTraversal.ForEachElement(group.ElseElements, (i, element) => EmitOptionalGroupElement(builder, group, element, i, group.ElseElements, indent + "    "));

            builder.AppendLine(indent + "}");
        }

        fieldIndex = groupStart + thenFieldCount + elseFieldCount;
    }

    private void EmitOptionalGroupElement(
        StringBuilder builder,
        OptionalGroup group,
        Element element,
        int elementIndex,
        IReadOnlyList<Element> siblings,
        string indent)
    {
        if (element is VariableChunk variable && IsImplicitUnitAttributeAnchor(group, elementIndex, variable))
        {
            var field = EmitterHelpers.NextBodySyntaxField(metadata.Fields, ref fieldIndex);
            var varName = EmitterHelpers.GetBodySyntaxFieldLocalName(field);
            var keywordField = metadata.Fields[fieldIndex - 2];
            var keywordVarName = EmitterHelpers.LowerFirst(keywordField.Name);
            builder.AppendLine(indent + varName + " = new UnitAttributeValueSyntax(" + keywordVarName + ".Value);");
            return;
        }

        EmitElement(builder, element, elementIndex, siblings, indent, declare: false);
    }

    /// <summary>
    /// Builds the guard condition for an optional group.
    /// Returns <c>(null, null)</c> when no supported guard can be derived.
    /// The second item is the expression to assign the first field from when the guard
    /// used <c>TryMatch</c> (which consumes the token as a side-effect).
    /// </summary>
    private (string? guardExpr, string? firstFieldAssignExpr) BuildOptionalGroupGuard(IReadOnlyList<Element> thenElements)
    {
        if (thenElements.Count == 0)
        {
            return (null, null);
        }

        var first = thenElements[0];
        if (first is LiteralChunk lit)
        {
            foreach (var l in lit.Value)
            {
                if (l is PunctuationLiteral punc)
                {
                    // Use TryMatch so the token is both checked and consumed in one call.
                    var firstVarName = EmitterHelpers.LowerFirst(metadata.Fields[fieldIndex].Name);
                    var guardExpr = "context.TryMatch(TokenKind." + punc.TokenKind + ", out var " + firstVarName + "Parsed)";
                    return (guardExpr, firstVarName + "Parsed");
                }

                if (l is KeywordLiteral kw)
                {
                    return ("context.IsKeyword(" + EmitterHelpers.ToCSharpStringLiteral(kw.Spelling) + ")", null);
                }
            }
        }
        else if (first is VariableChunk variable && !EmitterHelpers.ContainsName(operation.Attributes, variable.Name, static attribute => attribute.Name))
        {
            // Guard on SSA name presence (operand or result variable).
            return ("context.Is(TokenKind.SsaName)", null);
        }

        return (null, null);
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
    // Oilist – order-independent keyword-guarded clauses
    // -----------------------------------------------------------------------

    private void EmitOilist(StringBuilder builder, OilistDirectiveChunk oilist, string indent)
    {
        var oilistStart = fieldIndex;

        // Count total fields for all clauses.
        var totalFields = 0;
        foreach (var clause in oilist.Clauses)
        {
            totalFields += 1 + AssemblyFormatTraversal.CountFields(clause.Elements);
        }

        // Pre-declare all oilist fields as nullable locals.
        for (var i = 0; i < totalFields; i++)
        {
            var f = metadata.Fields[oilistStart + i];
            var defaultExpr = GetFieldDefaultExpression(f.CsType);
            builder.AppendLine(indent + f.CsType + " " + EmitterHelpers.LowerFirst(f.Name) + " = " + defaultExpr + ";");
        }

        builder.AppendLine(indent + "bool foundOilist;");
        builder.AppendLine(indent + "do");
        builder.AppendLine(indent + "{");
        builder.AppendLine(indent + "    foundOilist = false;");

        fieldIndex = oilistStart;
        var first = true;
        foreach (var clause in oilist.Clauses)
        {
            var clauseFieldCount = 1 + AssemblyFormatTraversal.CountFields(clause.Elements);
            var kwField = metadata.Fields[fieldIndex];
            var kwVarName = EmitterHelpers.LowerFirst(kwField.Name);

            var ifKw = first ? "if" : "else if";
            builder.AppendLine(indent + "    " + ifKw + " (!" + kwVarName + ".HasValue && context.IsKeyword(" + EmitterHelpers.ToCSharpStringLiteral(clause.Keyword) + "))");
            builder.AppendLine(indent + "    {");
            EmitParseResultAssignment(
                builder,
                indent + "        ",
                kwVarName,
                "context.ExpectKeyword(" +
                EmitterHelpers.ToCSharpStringLiteral(clause.Keyword) +
                ", \"Expected '" + EmitterHelpers.EscapeForStringLiteral(clause.Keyword) + "'.\")",
                declare: false,
                kwField.CsType);
            fieldIndex++; // advance past keyword field

            // Emit parsing for each oilist element in this clause.
            AssemblyFormatTraversal.ForEachElement(clause.Elements, (i, element) => EmitOilistElement(builder, element, i, clause.Elements, indent + "        "));

            builder.AppendLine(indent + "        foundOilist = true;");
            builder.AppendLine(indent + "    }");
            first = false;
        }

        builder.AppendLine(indent + "}");
        builder.AppendLine(indent + "while (foundOilist);");

        fieldIndex = oilistStart + totalFields;
    }

    private void EmitOilistElement(StringBuilder builder, OilistElement element, int elementIndex, IReadOnlyList<OilistElement> siblings, string indent)
    {
        switch (element)
        {
            case OilistVariableElement variable:
            {
                var f = metadata.Fields[fieldIndex++];
                var varName = EmitterHelpers.LowerFirst(f.Name);
                if (EmitterHelpers.ContainsName(operation.Attributes, variable.Name, static attribute => attribute.Name))
                {
                    var expectedConstraint = EmitterHelpers.TryGetAttributeConstraint(operation, variable.Name);
                    EmitParseResultAssignment(builder, indent, varName, BuildAttributeParseExpr(expectedConstraint, Array.Empty<TokenKind>()), declare: false, f.CsType);
                }
                else
                {
                    EmitParseResultAssignment(builder, indent, varName, "context.TryParseSsaToken()", declare: false, f.CsType);
                }

                break;
            }

            case OilistTypeDirectiveElement _:
            {
                var f = metadata.Fields[fieldIndex++];
                var varName = EmitterHelpers.LowerFirst(f.Name);
                EmitParseResultAssignment(builder, indent, varName, "context.TryParseTypeSyntax()", declare: false, f.CsType);
                break;
            }

            case OilistLiteralElement literal:
            {
                var f = metadata.Fields[fieldIndex++];
                var varName = EmitterHelpers.LowerFirst(f.Name);
                EmitParseResultAssignment(builder, indent, varName, "context.Expect(TokenKind.Identifier, \"Expected '" + EmitterHelpers.EscapeForStringLiteral(literal.Value) + "'.\")", declare: false, f.CsType);
                break;
            }
        }
    }

    private string BuildAttributeParseExpr(string? expectedConstraintRecordName, IReadOnlyList<TokenKind> delimiters)
    {
        var expectedDefinitionExpr = !string.IsNullOrEmpty(expectedConstraintRecordName)
            ? resolver.TryResolveAttributeConstraintDefinitionExpression(expectedConstraintRecordName!)
            : null;
        var hasExpectedConstraint = !string.IsNullOrEmpty(expectedDefinitionExpr);
        var hasDelimiters = delimiters.Count > 0;

        if (hasExpectedConstraint && hasDelimiters)
        {
            return "context.TryParseAttributeValueSyntax(" +
                expectedDefinitionExpr +
                ", " +
                BuildDelimiterList(delimiters) +
                ")";
        }

        if (hasExpectedConstraint)
        {
            return "context.TryParseAttributeValueSyntax(" + expectedDefinitionExpr + ")";
        }

        if (hasDelimiters)
        {
            return "context.TryParseAttributeValueSyntax(" + BuildDelimiterList(delimiters) + ")";
        }

        return "context.TryParseAttributeValueSyntax()";
    }

    // -----------------------------------------------------------------------
    // Body construction
    // -----------------------------------------------------------------------

    private void EmitBodyConstruction(StringBuilder builder)
    {
        var bodyClassName = className + "BodySyntax";
        if (metadata.Fields.Count == 0)
        {
            builder.AppendLine("        return ParseResult<OperationBodySyntax>.Success(new " + bodyClassName + "());");
            return;
        }

        builder.Append("        return ParseResult<OperationBodySyntax>.Success(new " + bodyClassName + "(");
        for (var i = 0; i < metadata.Fields.Count; i++)
        {
            if (i > 0)
            {
                builder.Append(", ");
            }

            builder.Append(EmitterHelpers.LowerFirst(metadata.Fields[i].Name));
        }

        builder.AppendLine("));");
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private static bool CanHandleElement(Element element)
    {
        return element switch
        {
            LiteralChunk _ => true,
            VariableChunk _ => true,
            AttrDictDirectiveChunk _ => true,
            AttrDictWithKeywordDirectiveChunk _ => true,
            PropDictDirectiveChunk _ => true,
            TypeDirectiveChunk _ => true,
            QualifiedDirectiveChunk _ => true,
            ResultsDirectiveChunk _ => true,
            FunctionalTypeDirectiveChunk _ => true,
            RegionsDirectiveChunk _ => true,
            SuccessorsDirectiveChunk _ => true,
            OperandsDirectiveChunk _ => true,
            OptionalGroup _ => true,
            OilistDirectiveChunk _ => true,
            _ => false,
        };
    }

    private string BuildTypeParseExpr(int elementIndex, IReadOnlyList<Element> allElements)
    {
        var field = metadata.Fields[fieldIndex - 1];
        if (field.CsType == "IReadOnlyList<TypeSyntax>")
        {
            return "context.ParseTypeSyntaxList()";
        }

        var delimiters = AssemblyFormatTraversal.FindNextPunctuationDelimiters(elementIndex, allElements);
        var keywords = AssemblyFormatTraversal.FindNextKeywordDelimiters(elementIndex, allElements);
        if (delimiters.Count > 0 || keywords.Count > 0)
        {
            var keywordArray = keywords.Count > 0
                ? "new[] { " + BuildKeywordList(keywords) + " }"
                : "global::System.Array.Empty<string>()";
            var delimiterSuffix = delimiters.Count > 0 ? ", " + BuildDelimiterList(delimiters) : string.Empty;
            return "context.TryParseRawUntilDelimiterOrKeyword(" + keywordArray + delimiterSuffix + ").Map<TypeSyntax>(static raw => new RawTypeSyntax(raw))";
        }

        return "context.TryParseTypeSyntax()";
    }

    private void EmitTypeAssignment(StringBuilder builder, string indent, string varName, string expr, bool declare, string csType)
    {
        if (csType == "IReadOnlyList<TypeSyntax>")
        {
            builder.AppendLine(indent + DeclareOrAssign(varName, expr, declare, csType) + ";");
            return;
        }

        EmitParseResultAssignment(builder, indent, varName, expr, declare, csType);
    }

    private static string GetFieldDefaultExpression(string csType)
    {
        if (csType.Contains("IReadOnlyList<SyntaxToken>", System.StringComparison.Ordinal))
        {
            return "global::System.Array.Empty<SyntaxToken>()";
        }

        if (csType.Contains("IReadOnlyList<TypeSyntax>", System.StringComparison.Ordinal))
        {
            return "global::System.Array.Empty<global::MLIR.Syntax.TypeSyntax>()";
        }

        return "default";
    }

    private static string BuildDelimiterList(IReadOnlyList<TokenKind> delimiters)
    {
        var parts = new List<string>(delimiters.Count);
        foreach (var d in delimiters)
        {
            parts.Add("TokenKind." + d);
        }

        return string.Join(", ", parts);
    }

    private static string BuildKeywordList(IReadOnlyList<string> keywords)
    {
        var parts = new List<string>(keywords.Count);
        foreach (var keyword in keywords)
        {
            parts.Add(EmitterHelpers.ToCSharpStringLiteral(keyword));
        }

        return string.Join(", ", parts);
    }

    /// <summary>
    /// Returns either <c>var varName = expr</c> (when <paramref name="declare"/> is <see langword="true"/>)
    /// or <c>varName = expr</c> (when <see langword="false"/>).
    /// The <paramref name="csType"/> is only used for the declaration form.
    /// </summary>
    private static string DeclareOrAssign(string varName, string expr, bool declare, string csType)
    {
        if (declare)
        {
            return "var " + varName + " = " + expr;
        }

        return varName + " = " + expr;
    }

    private static void EmitParseResultAssignment(StringBuilder builder, string indent, string varName, string expr, bool declare, string csType)
    {
        var resultName = varName + "Result";
        builder.AppendLine(indent + "var " + resultName + " = " + expr + ";");
        builder.AppendLine(indent + "if (!" + resultName + ".IsSuccess)");
        builder.AppendLine(indent + "{");
        builder.AppendLine(indent + "    return ParseResult<OperationBodySyntax>.Failure(" + resultName + ".Diagnostic!);");
        builder.AppendLine(indent + "}");
        builder.AppendLine(indent + DeclareOrAssign(varName, resultName + ".Value", declare, csType) + ";");
    }

}
