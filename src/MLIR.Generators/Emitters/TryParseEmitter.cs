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
            EmitElement(builder, elements[i], i, elements);
        }

        EmitBodyConstruction(builder);
        builder.AppendLine("        return true;");
        builder.AppendLine("    }");
    }

    // -----------------------------------------------------------------------
    // Element dispatch
    // -----------------------------------------------------------------------

    private void EmitElement(StringBuilder builder, Element element, int elementIndex, IReadOnlyList<Element> allElements)
    {
        switch (element)
        {
            case LiteralChunk literal:
                EmitLiteral(builder, literal);
                break;
            case VariableChunk variable:
                EmitVariable(builder, variable, elementIndex, allElements);
                break;
            case AttrDictDirectiveChunk _:
                EmitAttrDict(builder);
                break;
            case AttrDictWithKeywordDirectiveChunk _:
                EmitAttrDictWithKeyword(builder);
                break;
            case PropDictDirectiveChunk _:
                EmitPropDict(builder);
                break;
            case TypeDirectiveChunk typeDir:
                EmitType(builder, typeDir, elementIndex, allElements);
                break;
            case RegionsDirectiveChunk _:
                EmitRegions(builder);
                break;
            case SuccessorsDirectiveChunk _:
                EmitSuccessors(builder);
                break;
            case OperandsDirectiveChunk _:
                EmitOperands(builder);
                break;

            // OptionalGroup, OilistDirectiveChunk, CustomDirectiveChunk,
            // FunctionalTypeDirectiveChunk, QualifiedDirectiveChunk,
            // RefDirectiveChunk, ResultsDirectiveChunk → not yet supported;
            // CanHandleFormat already rejects formats containing these.
        }
    }

    // -----------------------------------------------------------------------
    // Per-element emission
    // -----------------------------------------------------------------------

    private void EmitLiteral(StringBuilder builder, LiteralChunk literal)
    {
        foreach (var lit in literal.Value)
        {
            switch (lit)
            {
                case PunctuationLiteral punc:
                {
                    var field = NextField();
                    var varName = EmitterHelpers.LowerFirst(field.Name);
                    builder.AppendLine(
                        "        var " + varName + " = context.Expect(TokenKind." + punc.TokenKind +
                        ", \"Expected '" + EscapeForStringLiteral(GetPunctuationDisplay(punc.TokenKind)) + "'.\");");
                    break;
                }

                case KeywordLiteral kw:
                {
                    var field = NextField();
                    var varName = EmitterHelpers.LowerFirst(field.Name);
                    builder.AppendLine(
                        "        var " + varName + " = context.ExpectKeyword(" +
                        EmitterHelpers.ToCSharpStringLiteral(kw.Spelling) +
                        ", \"Expected '" + EscapeForStringLiteral(kw.Spelling) + "'.\");");
                    break;
                }

                // WhitespaceLiteral, NewlineLiteral, EmptyLiteral: no field, no parse call
            }
        }
    }

    private void EmitVariable(StringBuilder builder, VariableChunk variable, int elementIndex, IReadOnlyList<Element> allElements)
    {
        var field = NextField();
        var varName = EmitterHelpers.LowerFirst(field.Name);

        if (EmitterHelpers.ContainsName(operation.Attributes, variable.Name))
        {
            // Attribute variable: parse raw attribute value, stopping at the next
            // meaningful delimiter so we don't over-consume into the next element.
            var delimiters = FindNextDelimitersForRawParsing(elementIndex, allElements);
            if (delimiters.Count > 0)
            {
                var delimList = BuildDelimiterList(delimiters);
                builder.AppendLine("        var " + varName + " = context.ParseAttributeValueSyntax(" + delimList + ");");
            }
            else
            {
                builder.AppendLine("        var " + varName + " = context.ParseAttributeValueSyntax();");
            }
        }
        else
        {
            // Operand or result variable: parse SSA value reference.
            builder.AppendLine("        var " + varName + " = context.ParseSsaToken();");
        }
    }

    private void EmitAttrDict(StringBuilder builder)
    {
        var field = NextField();
        var varName = EmitterHelpers.LowerFirst(field.Name);
        builder.AppendLine("        var " + varName + " = context.ParseAttrDict();");
    }

    private void EmitAttrDictWithKeyword(StringBuilder builder)
    {
        var field = NextField();
        var varName = EmitterHelpers.LowerFirst(field.Name);
        builder.AppendLine("        var " + varName + " = context.ParseAttrDictWithKeyword();");
    }

    private void EmitPropDict(StringBuilder builder)
    {
        var field = NextField();
        var varName = EmitterHelpers.LowerFirst(field.Name);
        builder.AppendLine("        var " + varName + " = context.ParseAttrDict();");
    }

    private void EmitType(StringBuilder builder, TypeDirectiveChunk typeDir, int elementIndex, IReadOnlyList<Element> allElements)
    {
        var field = NextField();
        var varName = EmitterHelpers.LowerFirst(field.Name);

        // Stop at the next punctuation / attr-dict boundary so we don't over-consume
        // when this type directive appears before other elements.
        var delimiters = FindNextDelimitersForRawParsing(elementIndex, allElements);
        if (delimiters.Count > 0)
        {
            var delimList = BuildDelimiterList(delimiters);
            builder.AppendLine("        var " + varName + " = new RawTypeSyntax(context.ParseRawUntilDelimiter(" + delimList + "));");
        }
        else
        {
            builder.AppendLine("        var " + varName + " = context.ParseTypeSyntax();");
        }
    }

    private void EmitRegions(StringBuilder builder)
    {
        var field = NextField();
        var varName = EmitterHelpers.LowerFirst(field.Name);
        builder.AppendLine("        var " + varName + " = context.ParseRegions();");
    }

    private void EmitSuccessors(StringBuilder builder)
    {
        var field = NextField();
        var varName = EmitterHelpers.LowerFirst(field.Name);
        builder.AppendLine("        var " + varName + " = context.ParseSuccessors();");
    }

    private void EmitOperands(StringBuilder builder)
    {
        var field = NextField();
        var varName = EmitterHelpers.LowerFirst(field.Name);
        builder.AppendLine("        var " + varName + " = context.ParseOperands();");
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
            RegionsDirectiveChunk _ => true,
            SuccessorsDirectiveChunk _ => true,
            OperandsDirectiveChunk _ => true,
            _ => false,
        };
    }

    /// <summary>
    /// Looks ahead in the element list from <paramref name="currentIndex"/> to find
    /// delimiter token kinds that should stop raw parsing for the element at
    /// <paramref name="currentIndex"/>.
    /// </summary>
    /// <remarks>
    /// Rules, in priority order:
    /// <list type="number">
    ///   <item>The first punctuation literal encountered → use its token kind.</item>
    ///   <item>An <c>attr-dict</c>, <c>attr-dict-with-keyword</c>, or <c>prop-dict</c>
    ///         directive → stop at <c>{</c> so the dict parser can decide whether
    ///         the brace is actually present.</item>
    /// </list>
    /// An empty list means "stop at operation boundary only".
    /// </remarks>
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

    private static string BuildDelimiterList(IReadOnlyList<TokenKind> delimiters)
    {
        var parts = new List<string>(delimiters.Count);
        foreach (var d in delimiters)
        {
            parts.Add("TokenKind." + d);
        }

        return string.Join(", ", parts);
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
