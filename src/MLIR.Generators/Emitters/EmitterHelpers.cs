namespace MLIR.Generators.Emitters;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using MLIR.ODS.Model;
using MLIR.ODS.Model.AssemblyFormat;
using MLIR.Text;

internal static class EmitterHelpers
{
    private static readonly HashSet<string> CSharpKeywords = new(StringComparer.Ordinal)
    {
        "abstract",
        "as",
        "base",
        "bool",
        "break",
        "byte",
        "case",
        "catch",
        "char",
        "checked",
        "class",
        "const",
        "continue",
        "decimal",
        "default",
        "delegate",
        "do",
        "double",
        "else",
        "enum",
        "event",
        "explicit",
        "extern",
        "false",
        "finally",
        "fixed",
        "float",
        "for",
        "foreach",
        "goto",
        "if",
        "implicit",
        "in",
        "int",
        "interface",
        "internal",
        "is",
        "lock",
        "long",
        "namespace",
        "new",
        "null",
        "object",
        "operator",
        "out",
        "override",
        "params",
        "private",
        "protected",
        "public",
        "readonly",
        "ref",
        "return",
        "sbyte",
        "sealed",
        "short",
        "sizeof",
        "stackalloc",
        "static",
        "string",
        "struct",
        "switch",
        "this",
        "throw",
        "true",
        "try",
        "typeof",
        "uint",
        "ulong",
        "unchecked",
        "unsafe",
        "ushort",
        "using",
        "var",
        "virtual",
        "void",
        "volatile",
        "while",
    };

    public static string ToCSharpStringLiteral(string value)
    {
        return "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
    }

    public static void AppendXmlDocComment(StringBuilder builder, string? summary, string? description)
    {
        if (!string.IsNullOrWhiteSpace(summary))
        {
            builder.AppendLine("/// <summary>" + EscapeXmlText(summary!.Trim()) + "</summary>");
        }

        if (!string.IsNullOrWhiteSpace(description))
        {
            builder.AppendLine("/// <remarks>");
            var trimmedDescription = description!.Trim();
            foreach (var rawLine in trimmedDescription.Split('\n'))
            {
                builder.AppendLine("/// " + EscapeXmlText(rawLine.TrimEnd('\r')));
            }

            builder.AppendLine("/// </remarks>");
        }
    }

    public static void AppendIndentedCode(StringBuilder builder, string code)
    {
        if (string.IsNullOrEmpty(code))
        {
            return;
        }

        var lines = code.Split('\n');
        foreach (var line in lines)
        {
            builder.Append("        ");
            builder.AppendLine(line);
        }
    }

    public static string EscapeXmlText(string text)
    {
        return text
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;");
    }

    public static string LowerFirst(string name)
    {
        if (name.Length == 0)
        {
            return name;
        }

        return SanitizeIdentifier(char.ToLowerInvariant(name[0]) + name.Substring(1));
    }

    public static string SanitizeIdentifier(string candidate)
    {
        if (candidate.Length == 0)
        {
            return "_";
        }

        var builder = new StringBuilder(candidate.Length + 1);
        for (var i = 0; i < candidate.Length; i++)
        {
            var ch = candidate[i];
            var isValid = i == 0
                ? char.IsLetter(ch) || ch == '_'
                : char.IsLetterOrDigit(ch) || ch == '_';
            builder.Append(isValid ? ch : '_');
        }

        if (builder.Length == 0 || (!char.IsLetter(builder[0]) && builder[0] != '_'))
        {
            builder.Insert(0, '_');
        }

        if (CSharpKeywords.Contains(builder.ToString()))
        {
            builder.Append('_');
        }

        return builder.ToString();
    }

    public static string MakeUnique(string baseName, HashSet<string> used)
    {
        if (used.Add(baseName))
        {
            return baseName;
        }

        for (var i = 2; ; i++)
        {
            var candidate = baseName + i.ToString(CultureInfo.InvariantCulture);
            if (used.Add(candidate))
            {
                return candidate;
            }
        }
    }

    public static bool ContainsName(IReadOnlyList<string> names, string name)
    {
        foreach (var n in names)
        {
            if (string.Equals(n, name, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    public static bool ContainsName<T>(IReadOnlyList<T> items, string name, Func<T, string> selector)
    {
        foreach (var item in items)
        {
            if (string.Equals(selector(item), name, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    public static string? TryGetAttributeConstraint(OperationModel operation, string attributeName)
    {
        foreach (var attribute in operation.Attributes)
        {
            if (string.Equals(attribute.Name, attributeName, StringComparison.Ordinal))
            {
                return attribute.ConstraintRecordName;
            }
        }

        return null;
    }

    public static BodyComponentKind GetComponentKindForVariable(OperationModel operation, string variableName)
    {
        if (ContainsName(operation.Regions, variableName, static region => region.Name))
        {
            return BodyComponentKind.Regions;
        }

        if (ContainsName(operation.Results, variableName, static result => result.Name))
        {
            return BodyComponentKind.Result;
        }

        if (ContainsName(operation.Operands, variableName, static operand => operand.Name))
        {
            return BodyComponentKind.Operand;
        }

        return BodyComponentKind.Unknown;
    }

    public static string GetDirectiveOperandName(DirectiveOperand operand)
    {
        return operand switch
        {
            VariableOperand variable => variable.Name,
            _ => operand?.GetType().Name ?? "Operand"
        };
    }

    /// <summary>
    /// Appends body-syntax fields produced by <paramref name="element"/> into
    /// <paramref name="metadata"/>.
    /// </summary>
    /// <param name="usedNames">Tracks already-used field names to ensure uniqueness.</param>
    /// <param name="element">The assembly-format element to generate fields for.</param>
    /// <param name="operation">The operation model providing attribute/operand/result name lists.</param>
    /// <param name="metadata">Accumulates the generated fields and component descriptors.</param>
    /// <param name="nullable">
    /// When <see langword="true"/> every field is generated with a nullable C# type
    /// (e.g. <c>SyntaxToken?</c> instead of <c>SyntaxToken</c>).  This is used for
    /// elements that live inside optional groups.
    /// </param>
    public static void AppendBodySyntaxFields(HashSet<string> usedNames, Element element, OperationModel operation, OperationBodySyntaxMetadata metadata, bool nullable = false)
    {
        switch (element)
        {
            case LiteralChunk literal:
                foreach (var lit in literal.Value)
                {
                    switch (lit)
                    {
                        case PunctuationLiteral punc:
                            AppendPunctuationField(usedNames, punc.TokenKind, metadata, nullable);
                            break;

                        case KeywordLiteral kw:
                            AppendKeywordField(usedNames, kw.Spelling, metadata, nullable);
                            break;

                        // WhitespaceLiteral, NewlineLiteral, EmptyLiteral → no field; spacing is in stored trivia
                    }
                }

                break;

            case VariableChunk variable:
                AppendVariableField(usedNames, variable.Name, operation, metadata, nullable);
                break;

            case AttrDictDirectiveChunk _:
            {
                var name = MakeUnique("AttrDict", usedNames);
                var field = new BodySyntaxField(name, "DelimitedSyntaxList<NamedAttributeSyntax>",
                    "writer.WriteDelimitedList(" + name + ", \" \");");
                metadata.AddField(field);
                metadata.AddComponentField(new BodyComponentField(BodyComponentKind.AttrDict, "AttrDict", field.Name));
                break;
            }

            case AttrDictWithKeywordDirectiveChunk _:
            {
                var name = MakeUnique("AttrDictWithKeyword", usedNames);
                var field = new BodySyntaxField(name, "DelimitedSyntaxList<NamedAttributeSyntax>",
                    "writer.WriteDelimitedList(" + name + ", \" \");");
                metadata.AddField(field);
                metadata.AddComponentField(new BodyComponentField(BodyComponentKind.AttrDictWithKeyword, "AttrDictWithKeyword", field.Name));
                break;
            }

            case PropDictDirectiveChunk _:
            {
                var name = MakeUnique("PropDict", usedNames);
                var field = new BodySyntaxField(name, "DelimitedSyntaxList<NamedAttributeSyntax>",
                    "writer.WriteDelimitedList(" + name + ", \" \");");
                metadata.AddField(field);
                metadata.AddComponentField(new BodyComponentField(BodyComponentKind.PropDict, "PropDict", field.Name));
                break;
            }

            case RegionsDirectiveChunk _:
            {
                var name = MakeUnique("Regions", usedNames);
                var field = new BodySyntaxField(name, "IReadOnlyList<RegionSyntax>",
                    "foreach (var region in " + name + ")\n" +
                    "{\n" +
                    "    writer.WriteRegion(region);\n" +
                    "}");
                metadata.AddField(field);
                metadata.AddComponentField(new BodyComponentField(BodyComponentKind.Regions, "Regions", field.Name));
                break;
            }

            case TypeDirectiveChunk typeDir:
                AppendTypeField(usedNames, GetTypeBaseName(typeDir.Operand), GetDirectiveOperandName(typeDir.Operand), operation, metadata, nullable);
                break;

            case SuccessorsDirectiveChunk _:
            {
                var name = MakeUnique("Successors", usedNames);
                var field = new BodySyntaxField(name, "DelimitedSyntaxList<SyntaxToken>",
                    "writer.WriteDelimitedList(" + name + ", \" \");");
                metadata.AddField(field);
                metadata.AddComponentField(new BodyComponentField(BodyComponentKind.Successors, "Successors", field.Name));
                break;
            }

            case OperandsDirectiveChunk _:
            {
                var name = MakeUnique("Operands", usedNames);
                var field = new BodySyntaxField(name, "DelimitedSyntaxList<SyntaxToken>",
                    "writer.WriteDelimitedList(" + name + ", \" \");");
                metadata.AddField(field);
                metadata.AddComponentField(new BodyComponentField(BodyComponentKind.Operands, "Operands", field.Name));
                break;
            }

            case QualifiedDirectiveChunk qualified:
                // qualified(...) does not change parsing behaviour, so represent the inner
                // type as a plain TypeSyntax field just like TypeDirectiveChunk does.
                AppendTypeField(usedNames, GetQualifiedTypeBaseName(qualified.Operand), GetDirectiveOperandName(qualified.Operand), operation, metadata, nullable);
                break;

            case ResultsDirectiveChunk _:
                AppendTypeField(usedNames, "ResultsType", "Results", operation, metadata, nullable);
                break;

            case FunctionalTypeDirectiveChunk _:
                AppendTypeField(usedNames, "FunctionalType", "Type", operation, metadata, nullable);
                break;

            case OptionalGroup optionalGroup:
            {
                // Each element inside an optional group contributes a nullable field.
                foreach (var inner in optionalGroup.ThenElements)
                {
                    AppendBodySyntaxFields(usedNames, inner, operation, metadata, nullable: true);
                }

                if (optionalGroup.ElseElements != null)
                {
                    foreach (var inner in optionalGroup.ElseElements)
                    {
                        AppendBodySyntaxFields(usedNames, inner, operation, metadata, nullable: true);
                    }
                }

                break;
            }

            case OilistDirectiveChunk oilist:
            {
                // Each clause contributes a nullable keyword field followed by nullable
                // fields for the elements the clause contains.
                foreach (var clause in oilist.Clauses)
                {
                    AppendKeywordField(usedNames, clause.Keyword, metadata, nullable: true, isOilistKeyword: true);

                    foreach (var oiElem in clause.Elements)
                    {
                        AppendOilistElementFields(usedNames, oiElem, operation, metadata);
                    }
                }

                break;
            }

            // CustomDirectiveChunk, RefDirectiveChunk → not stored in this CST class
        }
    }

    /// <summary>
    /// Generates a nullable body-syntax field for a single element within an oilist clause,
    /// delegating to the same leaf helpers used by <see cref="AppendBodySyntaxFields"/>.
    /// </summary>
    private static void AppendOilistElementFields(HashSet<string> usedNames, OilistElement element, OperationModel operation, OperationBodySyntaxMetadata metadata)
    {
        switch (element)
        {
            case OilistVariableElement variable:
                AppendVariableField(usedNames, variable.Name, operation, metadata, nullable: true);
                break;

            case OilistTypeDirectiveElement typeDir:
                AppendTypeField(usedNames, GetTypeBaseName(typeDir.Operand), GetDirectiveOperandName(typeDir.Operand), operation, metadata, nullable: true);
                break;

            case OilistLiteralElement literal:
            {
                var name = MakeUnique(DialectGeneratorNaming.ToPascalCase(literal.Value) + "Token", usedNames);
                var field = new BodySyntaxField(name, "SyntaxToken?",
                    "if (" + name + ".HasValue) writer.WriteToken(" + name + ".Value, string.Empty);");
                metadata.AddField(field);
                metadata.AddComponentField(new BodyComponentField(BodyComponentKind.Literal, "OilistLiteral:" + literal.Value, field.Name));
                break;
            }
        }
    }

    // -----------------------------------------------------------------------
    // Leaf field-creation helpers shared across element types
    // -----------------------------------------------------------------------

    private static void AppendPunctuationField(HashSet<string> usedNames, TokenKind tokenKind, OperationBodySyntaxMetadata metadata, bool nullable)
    {
        var name = MakeUnique(GetPunctuationFieldName(tokenKind), usedNames);
        var (csType, writeStmt) = nullable
            ? ("SyntaxToken?", "if (" + name + ".HasValue) writer.WriteToken(" + name + ".Value, string.Empty);")
            : ("SyntaxToken", "writer.WriteToken(" + name + ", string.Empty);");
        var field = new BodySyntaxField(name, csType, writeStmt);
        metadata.AddField(field);
        metadata.AddComponentField(new BodyComponentField(BodyComponentKind.Literal, "Punctuation:" + tokenKind, field.Name));
    }

    private static void AppendKeywordField(HashSet<string> usedNames, string spelling, OperationBodySyntaxMetadata metadata, bool nullable, bool isOilistKeyword = false)
    {
        var name = MakeUnique(DialectGeneratorNaming.ToPascalCase(spelling) + "Keyword", usedNames);
        // Oilist keywords default to "\n    " so each synthesized clause starts on its own line.
        // ParseAttributeValueSyntax stops at newlines (operation boundaries), so without a newline
        // the first clause's raw value would greedily consume the next clause's keyword.
        var leadingTrivia = isOilistKeyword ? "\\n    " : " ";
        var (csType, writeStmt) = nullable
            ? ("SyntaxToken?", "if (" + name + ".HasValue) writer.WriteToken(" + name + ".Value, \"" + leadingTrivia + "\");")
            : ("SyntaxToken", "writer.WriteToken(" + name + ", \"" + leadingTrivia + "\");");
        var field = new BodySyntaxField(name, csType, writeStmt);
        metadata.AddField(field);
        metadata.AddComponentField(new BodyComponentField(BodyComponentKind.Literal, "Keyword:" + spelling, field.Name));
    }

    private static void AppendVariableField(HashSet<string> usedNames, string variableName, OperationModel operation, OperationBodySyntaxMetadata metadata, bool nullable)
    {
        var name = MakeUnique(DialectGeneratorNaming.ToPascalCase(variableName), usedNames);
        if (ContainsName(operation.Attributes, variableName, static attribute => attribute.Name))
        {
            var (csType, writeStmt) = nullable
                ? ("AttributeValueSyntax?", "if (" + name + " != null) { writer.SuggestTrivia(\" \"); " + name + ".WriteTo(writer); }")
                : ("AttributeValueSyntax", "writer.SuggestTrivia(\" \"); " + name + ".WriteTo(writer);");
            var field = new BodySyntaxField(name, csType, writeStmt);
            metadata.AddField(field);
            metadata.AddComponentField(new BodyComponentField(BodyComponentKind.Attribute, variableName, field.Name));
        }
        else if (ContainsName(operation.Regions, variableName, static region => region.Name))
        {
            var isVariadic = IsVariadicRegion(operation, variableName);
            string csType;
            string writeStmt;
            if (isVariadic)
            {
                csType = "global::System.Collections.Generic.IReadOnlyList<RegionSyntax>";
                writeStmt = "foreach (var region in " + name + ") { writer.WriteRegion(region); }";
            }
            else
            {
                csType = nullable ? "RegionSyntax?" : "RegionSyntax";
                writeStmt = nullable
                    ? "if (" + name + ".HasValue) writer.WriteRegion(" + name + ".Value);"
                    : "writer.WriteRegion(" + name + ");";
            }

            var field = new BodySyntaxField(name, csType, writeStmt);
            metadata.AddField(field);
            metadata.AddComponentField(new BodyComponentField(
                GetComponentKindForVariable(operation, variableName),
                variableName,
                field.Name));
        }
        else if (IsVariadicOperand(operation, variableName))
        {
            // Variadic operands use a list of SSA tokens.  The write statement iterates over
            // the list and inserts commas between items.
            const string csType = "global::System.Collections.Generic.IReadOnlyList<SyntaxToken>";
            var writeStmt =
                "for (var _i = 0; _i < " + name + ".Count; _i++) { " +
                "if (_i > 0) writer.WriteToken(new SyntaxToken(\",\"), \"\"); " +
                "writer.WriteToken(" + name + "[_i], _i > 0 ? \" \" : \"\"); }";
            var field = new BodySyntaxField(name, csType, writeStmt);
            metadata.AddField(field);
            metadata.AddComponentField(new BodyComponentField(
                GetComponentKindForVariable(operation, variableName),
                variableName,
                field.Name));
        }
        else
        {
            var (csType, writeStmt) = nullable
                ? ("SyntaxToken?", "if (" + name + ".HasValue) writer.WriteToken(" + name + ".Value, \" \");")
                : ("SyntaxToken", "writer.WriteToken(" + name + ", \" \");");
            var field = new BodySyntaxField(name, csType, writeStmt);
            metadata.AddField(field);
            metadata.AddComponentField(new BodyComponentField(
                GetComponentKindForVariable(operation, variableName),
                variableName,
                field.Name));
        }
    }

    /// <summary>Returns true when <paramref name="variableName"/> names a variadic operand of <paramref name="operation"/>.</summary>
    private static bool IsVariadicOperand(OperationModel operation, string variableName)
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

    private static bool IsVariadicRegion(OperationModel operation, string variableName)
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

    private static void AppendTypeField(HashSet<string> usedNames, string baseName, string operandName, OperationModel operation, OperationBodySyntaxMetadata metadata, bool nullable)
    {
        var name = MakeUnique(baseName, usedNames);
        string csType;
        string writeStmt;
        if (IsVariadicOperand(operation, operandName))
        {
            csType = "IReadOnlyList<TypeSyntax>";
            writeStmt =
                "for (var i = 0; i < " + name + ".Count; i++)\n" +
                "{\n" +
                "    if (i > 0)\n" +
                "    {\n" +
                "        writer.WriteToken(new SyntaxToken(\",\"), string.Empty);\n" +
                "    }\n" +
                "\n" +
                "    writer.SuggestTrivia(\" \");\n" +
                "    " + name + "[i].WriteTo(writer);\n" +
                "}";
        }
        else
        {
            (csType, writeStmt) = nullable
                ? ("TypeSyntax?", "if (" + name + " != null) { writer.SuggestTrivia(\" \"); " + name + ".WriteTo(writer); }")
                : ("TypeSyntax", "writer.SuggestTrivia(\" \"); " + name + ".WriteTo(writer);");
        }

        var field = new BodySyntaxField(name, csType, writeStmt);
        metadata.AddField(field);
        metadata.AddComponentField(new BodyComponentField(BodyComponentKind.Type, operandName, field.Name));
    }

    private static string GetTypeBaseName(DirectiveOperand operand)
    {
        return operand is VariableOperand varOp
            ? DialectGeneratorNaming.ToPascalCase(varOp.Name) + "Type"
            : "Type";
    }

    private static string GetQualifiedTypeBaseName(DirectiveOperand operand)
    {
        // qualified(type($var)) → the inner type operand gives the best base name.
        if (operand is TypeDirectiveOperand tdo && tdo.Operand is VariableOperand tVar)
        {
            return DialectGeneratorNaming.ToPascalCase(tVar.Name) + "Type";
        }

        return GetTypeBaseName(operand);
    }

    private static string GetPunctuationFieldName(TokenKind tokenKind)
    {
        return tokenKind switch
        {
            TokenKind.Comma => "CommaToken",
            TokenKind.LParen => "LParenToken",
            TokenKind.RParen => "RParenToken",
            TokenKind.LBracket => "LBracketToken",
            TokenKind.RBracket => "RBracketToken",
            TokenKind.LBrace => "LBraceToken",
            TokenKind.RBrace => "RBraceToken",
            TokenKind.Arrow => "ArrowToken",
            TokenKind.Colon => "ColonToken",
            TokenKind.Equal => "EqualToken",
            TokenKind.LessThan => "LessThanToken",
            TokenKind.GreaterThan => "GreaterThanToken",
            TokenKind.Question => "QuestionToken",
            TokenKind.Star => "StarToken",
            TokenKind.Plus => "PlusToken",
            TokenKind.Minus => "MinusToken",
            TokenKind.Dot => "DotToken",
            TokenKind.At => "AtToken",
            TokenKind.Hash => "HashToken",
            _ => "Token",
        };
    }
}
