namespace MLIR.Generators.Emitters;

using System.Collections.Generic;
using System.Text;
using MLIR.ODS.Model;
using MLIR.ODS.Model.AssemblyFormat;
using MLIR.Text;

/// <summary>
/// Generates the <c>TryParse</c> method body for a declarative assembly format.
/// </summary>
/// <remarks>
/// Each supported assembly format element is translated into a call on
/// <see cref="MLIR.Text.OperationParsingContext"/>.  After all elements have
/// been parsed the generated code constructs the typed <c>OperationBodySyntax</c>
/// subclass and returns <see langword="true"/>.
///
/// Formats that contain directives that are not yet supported produce a fallback
/// implementation that immediately returns <see langword="false"/> so that the
/// parser falls back to the generic format.
/// </remarks>
internal sealed class TryParseEmitter
{
    private readonly OperationModel operation;
    private readonly OperationBodySyntaxMetadata metadata;
    private readonly string className;
    private int fieldIndex;

    private TryParseEmitter(OperationModel operation, OperationBodySyntaxMetadata metadata)
    {
        this.operation = operation;
        this.metadata = metadata;
        className = DialectGeneratorNaming.GetOperationClassName(operation);
        fieldIndex = 0;
    }

    /// <summary>
    /// Emits the full <c>TryParse</c> method, including signature and closing brace, into
    /// <paramref name="builder"/>.
    /// </summary>
    public static void Emit(StringBuilder builder, OperationModel operation, OperationBodySyntaxMetadata metadata)
    {
        var emitter = new TryParseEmitter(operation, metadata);
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
        builder.AppendLine("    public bool TryParse(SyntaxToken nameToken, IReadOnlyList<SyntaxToken> resultTokens, IReadOnlyList<SyntaxToken> resultCommaTokens, SyntaxToken? equalsToken, OperationParsingContext context, out OperationBodySyntax? body)");
        builder.AppendLine("    {");

        var format = operation.AssemblyFormat!;

        if (!CanHandleFormat(format, operation))
        {
            // Unsupported directives – fall back to generic parsing.
            builder.AppendLine("        body = null;");
            builder.AppendLine("        return false;");
            builder.AppendLine("    }");
            return;
        }

        fieldIndex = 0;

        var elements = format.Elements;
        for (var i = 0; i < elements.Count; i++)
        {
            EmitElement(builder, elements[i], i, elements, indent: "        ", declare: true);
        }

        EmitBodyConstruction(builder);
        builder.AppendLine("        return true;");
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
                    var field = NextField();
                    var varName = EmitterHelpers.LowerFirst(field.Name);
                    var expr = "context.Expect(TokenKind." + punc.TokenKind +
                               ", \"Expected '" + EscapeForStringLiteral(GetPunctuationDisplay(punc.TokenKind)) + "'.\")";
                    builder.AppendLine(indent + DeclareOrAssign(varName, expr, declare, field.CsType) + ";");
                    break;
                }

                case KeywordLiteral kw:
                {
                    var field = NextField();
                    var varName = EmitterHelpers.LowerFirst(field.Name);
                    var expr = "context.ExpectKeyword(" +
                               EmitterHelpers.ToCSharpStringLiteral(kw.Spelling) +
                               ", \"Expected '" + EscapeForStringLiteral(kw.Spelling) + "'.\")";
                    builder.AppendLine(indent + DeclareOrAssign(varName, expr, declare, field.CsType) + ";");
                    break;
                }

                // WhitespaceLiteral, NewlineLiteral, EmptyLiteral: no field, no parse call
            }
        }
    }

    private void EmitVariable(StringBuilder builder, VariableChunk variable, int elementIndex, IReadOnlyList<Element> allElements, string indent, bool declare)
    {
        var field = NextField();
        var varName = EmitterHelpers.LowerFirst(field.Name);

        if (EmitterHelpers.ContainsName(operation.Attributes, variable.Name))
        {
            var delimiters = FindNextDelimitersForRawParsing(elementIndex, allElements);
            var expr = delimiters.Count > 0
                ? "context.ParseAttributeValueSyntax(" + BuildDelimiterList(delimiters) + ")"
                : "context.ParseAttributeValueSyntax()";
            builder.AppendLine(indent + DeclareOrAssign(varName, expr, declare, field.CsType) + ";");
        }
        else
        {
            builder.AppendLine(indent + DeclareOrAssign(varName, "context.ParseSsaToken()", declare, field.CsType) + ";");
        }
    }

    private void EmitAttrDict(StringBuilder builder, string indent, bool declare)
    {
        var field = NextField();
        var varName = EmitterHelpers.LowerFirst(field.Name);
        builder.AppendLine(indent + DeclareOrAssign(varName, "context.ParseAttrDict()", declare, field.CsType) + ";");
    }

    private void EmitAttrDictWithKeyword(StringBuilder builder, string indent, bool declare)
    {
        var field = NextField();
        var varName = EmitterHelpers.LowerFirst(field.Name);
        builder.AppendLine(indent + DeclareOrAssign(varName, "context.ParseAttrDictWithKeyword()", declare, field.CsType) + ";");
    }

    private void EmitPropDict(StringBuilder builder, string indent, bool declare)
    {
        var field = NextField();
        var varName = EmitterHelpers.LowerFirst(field.Name);
        builder.AppendLine(indent + DeclareOrAssign(varName, "context.ParseAttrDict()", declare, field.CsType) + ";");
    }

    private void EmitType(StringBuilder builder, TypeDirectiveChunk typeDir, int elementIndex, IReadOnlyList<Element> allElements, string indent, bool declare)
    {
        var field = NextField();
        var varName = EmitterHelpers.LowerFirst(field.Name);
        var expr = BuildTypeParseExpr(elementIndex, allElements);
        builder.AppendLine(indent + DeclareOrAssign(varName, expr, declare, field.CsType) + ";");
    }

    private void EmitQualifiedType(StringBuilder builder, QualifiedDirectiveChunk qualified, int elementIndex, IReadOnlyList<Element> allElements, string indent, bool declare)
    {
        // qualified(...) does not change parsing behaviour; treat the same as a plain type.
        var field = NextField();
        var varName = EmitterHelpers.LowerFirst(field.Name);
        var expr = BuildTypeParseExpr(elementIndex, allElements);
        builder.AppendLine(indent + DeclareOrAssign(varName, expr, declare, field.CsType) + ";");
    }

    private void EmitResultsType(StringBuilder builder, int elementIndex, IReadOnlyList<Element> allElements, string indent, bool declare)
    {
        var field = NextField();
        var varName = EmitterHelpers.LowerFirst(field.Name);
        var expr = BuildTypeParseExpr(elementIndex, allElements);
        builder.AppendLine(indent + DeclareOrAssign(varName, expr, declare, field.CsType) + ";");
    }

    private void EmitRegions(StringBuilder builder, string indent, bool declare)
    {
        var field = NextField();
        var varName = EmitterHelpers.LowerFirst(field.Name);
        builder.AppendLine(indent + DeclareOrAssign(varName, "context.ParseRegions()", declare, field.CsType) + ";");
    }

    private void EmitSuccessors(StringBuilder builder, string indent, bool declare)
    {
        var field = NextField();
        var varName = EmitterHelpers.LowerFirst(field.Name);
        builder.AppendLine(indent + DeclareOrAssign(varName, "context.ParseSuccessors()", declare, field.CsType) + ";");
    }

    private void EmitOperands(StringBuilder builder, string indent, bool declare)
    {
        var field = NextField();
        var varName = EmitterHelpers.LowerFirst(field.Name);
        builder.AppendLine(indent + DeclareOrAssign(varName, "context.ParseOperands()", declare, field.CsType) + ";");
    }

    // -----------------------------------------------------------------------
    // Optional group – conditional parsing
    // -----------------------------------------------------------------------

    private void EmitOptionalGroup(StringBuilder builder, OptionalGroup group, int elementIndex, IReadOnlyList<Element> allElements, string indent)
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
            for (var i = 1; i < group.ThenElements.Count; i++)
            {
                EmitElement(builder, group.ThenElements[i], i, group.ThenElements, indent + "    ", declare: false);
            }
        }
        else
        {
            fieldIndex = groupStart;
            for (var i = 0; i < group.ThenElements.Count; i++)
            {
                EmitElement(builder, group.ThenElements[i], i, group.ThenElements, indent + "    ", declare: false);
            }
        }

        builder.AppendLine(indent + "}");

        if (group.ElseElements != null && group.ElseElements.Count > 0)
        {
            builder.AppendLine(indent + "else");
            builder.AppendLine(indent + "{");
            fieldIndex = groupStart + thenFieldCount;
            for (var i = 0; i < group.ElseElements.Count; i++)
            {
                EmitElement(builder, group.ElseElements[i], i, group.ElseElements, indent + "    ", declare: false);
            }

            builder.AppendLine(indent + "}");
        }

        fieldIndex = groupStart + thenFieldCount + elseFieldCount;
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
        else if (first is VariableChunk variable && !EmitterHelpers.ContainsName(operation.Attributes, variable.Name))
        {
            // Guard on SSA name presence (operand or result variable).
            return ("context.Is(TokenKind.SsaName)", null);
        }

        return (null, null);
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
            totalFields += 1 + CountFieldsForOilistElements(clause.Elements);
        }

        // Pre-declare all oilist fields as nullable locals.
        for (var i = 0; i < totalFields; i++)
        {
            var f = metadata.Fields[oilistStart + i];
            builder.AppendLine(indent + f.CsType + " " + EmitterHelpers.LowerFirst(f.Name) + " = default;");
        }

        builder.AppendLine(indent + "bool foundOilist;");
        builder.AppendLine(indent + "do");
        builder.AppendLine(indent + "{");
        builder.AppendLine(indent + "    foundOilist = false;");

        fieldIndex = oilistStart;
        var first = true;
        foreach (var clause in oilist.Clauses)
        {
            var clauseFieldCount = 1 + CountFieldsForOilistElements(clause.Elements);
            var kwField = metadata.Fields[fieldIndex];
            var kwVarName = EmitterHelpers.LowerFirst(kwField.Name);

            var ifKw = first ? "if" : "else if";
            builder.AppendLine(indent + "    " + ifKw + " (!" + kwVarName + ".HasValue && context.IsKeyword(" + EmitterHelpers.ToCSharpStringLiteral(clause.Keyword) + "))");
            builder.AppendLine(indent + "    {");
            builder.AppendLine(indent + "        " + kwVarName + " = context.ExpectKeyword(" +
                               EmitterHelpers.ToCSharpStringLiteral(clause.Keyword) +
                               ", \"Expected '" + EscapeForStringLiteral(clause.Keyword) + "'.\");");
            fieldIndex++; // advance past keyword field

            // Emit parsing for each oilist element in this clause.
            for (var i = 0; i < clause.Elements.Count; i++)
            {
                EmitOilistElement(builder, clause.Elements[i], i, clause.Elements, indent + "        ");
            }

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
                if (EmitterHelpers.ContainsName(operation.Attributes, variable.Name))
                {
                    builder.AppendLine(indent + varName + " = context.ParseAttributeValueSyntax();");
                }
                else
                {
                    builder.AppendLine(indent + varName + " = context.ParseSsaToken();");
                }

                break;
            }

            case OilistTypeDirectiveElement _:
            {
                var f = metadata.Fields[fieldIndex++];
                var varName = EmitterHelpers.LowerFirst(f.Name);
                builder.AppendLine(indent + varName + " = context.ParseTypeSyntax();");
                break;
            }

            case OilistLiteralElement literal:
            {
                var f = metadata.Fields[fieldIndex++];
                var varName = EmitterHelpers.LowerFirst(f.Name);
                builder.AppendLine(indent + varName + " = context.Expect(TokenKind.Identifier, \"Expected '" + EscapeForStringLiteral(literal.Value) + "'.\");");
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
            builder.AppendLine("        body = new " + bodyClassName + "();");
            return;
        }

        builder.Append("        body = new " + bodyClassName + "(");
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
    // Helpers
    // -----------------------------------------------------------------------

    private BodySyntaxField NextField()
    {
        return metadata.Fields[fieldIndex++];
    }

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
            RegionsDirectiveChunk _ => true,
            SuccessorsDirectiveChunk _ => true,
            OperandsDirectiveChunk _ => true,
            OptionalGroup _ => true,
            OilistDirectiveChunk _ => true,
            _ => false,
        };
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
                    if (lit is PunctuationLiteral || lit is KeywordLiteral) n++;
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
            case OilistDirectiveChunk oilist:
            {
                var total = 0;
                foreach (var clause in oilist.Clauses)
                {
                    total += 1 + CountFieldsForOilistElements(clause.Elements);
                }

                return total;
            }

            default: return 0;
        }
    }

    private static int CountFieldsForOilistElements(IReadOnlyList<OilistElement> elements)
    {
        var count = 0;
        foreach (var e in elements)
        {
            count += e is OilistVariableElement || e is OilistTypeDirectiveElement || e is OilistLiteralElement ? 1 : 0;
        }

        return count;
    }

    /// <summary>
    /// Looks ahead in the element list from <paramref name="currentIndex"/> to find
    /// delimiter token kinds that should stop raw parsing for the element at
    /// <paramref name="currentIndex"/>.
    /// </summary>
    private static IReadOnlyList<TokenKind> FindNextDelimitersForRawParsing(int currentIndex, IReadOnlyList<Element> elements)
    {
        for (var i = currentIndex + 1; i < elements.Count; i++)
        {
            var element = elements[i];
            if (element is LiteralChunk literalChunk)
            {
                foreach (var lit in literalChunk.Value)
                {
                    if (lit is PunctuationLiteral punc)
                    {
                        return new[] { punc.TokenKind };
                    }
                }
            }
            else if (element is AttrDictDirectiveChunk || element is AttrDictWithKeywordDirectiveChunk || element is PropDictDirectiveChunk)
            {
                return new[] { TokenKind.LBrace };
            }
        }

        return System.Array.Empty<TokenKind>();
    }

    private string BuildTypeParseExpr(int elementIndex, IReadOnlyList<Element> allElements)
    {
        var delimiters = FindNextDelimitersForRawParsing(elementIndex, allElements);
        if (delimiters.Count > 0)
        {
            return "new RawTypeSyntax(context.ParseRawUntilDelimiter(" + BuildDelimiterList(delimiters) + "))";
        }

        return "context.ParseTypeSyntax()";
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

    private static string GetPunctuationDisplay(TokenKind kind)
    {
        return kind switch
        {
            TokenKind.Comma => ",",
            TokenKind.Colon => ":",
            TokenKind.LParen => "(",
            TokenKind.RParen => ")",
            TokenKind.LBracket => "[",
            TokenKind.RBracket => "]",
            TokenKind.LBrace => "{",
            TokenKind.RBrace => "}",
            TokenKind.Arrow => "->",
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
            _ => kind.ToString(),
        };
    }

    private static string EscapeForStringLiteral(string text)
    {
        return text.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }
}

