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

        return char.ToLowerInvariant(name[0]) + name.Substring(1);
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

    public static BodyComponentKind GetComponentKindForVariable(OperationModel operation, string variableName)
    {
        if (ContainsName(operation.Results, variableName))
        {
            return BodyComponentKind.Result;
        }

        if (ContainsName(operation.Operands, variableName))
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

    public static void AppendBodySyntaxFields(HashSet<string> usedNames, Element element, OperationModel operation, OperationBodySyntaxMetadata metadata)
    {
        switch (element)
        {
            case LiteralChunk literal:
                foreach (var lit in literal.Value)
                {
                    switch (lit)
                    {
                        case PunctuationLiteral punc:
                        {
                            var name = MakeUnique(GetPunctuationFieldName(punc.TokenKind), usedNames);
                            var field = new BodySyntaxField(name, "SyntaxToken",
                                "writer.WriteToken(" + name + ", string.Empty);");
                            metadata.AddField(field);
                            metadata.AddComponentField(new BodyComponentField(BodyComponentKind.Literal, "Punctuation:" + punc.TokenKind, field.Name));
                            break;
                        }

                        case KeywordLiteral kw:
                        {
                            var name = MakeUnique(DialectGeneratorNaming.ToPascalCase(kw.Spelling) + "Keyword", usedNames);
                            var field = new BodySyntaxField(name, "SyntaxToken",
                                "writer.WriteToken(" + name + ", \" \");");
                            metadata.AddField(field);
                            metadata.AddComponentField(new BodyComponentField(BodyComponentKind.Literal, "Keyword:" + kw.Spelling, field.Name));
                            break;
                        }

                        // WhitespaceLiteral, NewlineLiteral, EmptyLiteral → no field; spacing is in stored trivia
                    }
                }

                break;

            case VariableChunk variable:
            {
                var pascalName = DialectGeneratorNaming.ToPascalCase(variable.Name);
                if (ContainsName(operation.Attributes, variable.Name))
                {
                    var name = MakeUnique(pascalName, usedNames);
                    var field = new BodySyntaxField(name, "AttributeValueSyntax",
                        name + ".WriteTo(writer, \" \");");
                    metadata.AddField(field);
                    metadata.AddComponentField(new BodyComponentField(BodyComponentKind.Attribute, variable.Name, field.Name));
                }
                else
                {
                    var name = MakeUnique(pascalName, usedNames);
                    var field = new BodySyntaxField(name, "SyntaxToken",
                        "writer.WriteToken(" + name + ", \" \");");
                    metadata.AddField(field);
                    metadata.AddComponentField(new BodyComponentField(
                        GetComponentKindForVariable(operation, variable.Name),
                        variable.Name,
                        field.Name));
                }

                break;
            }

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
                    "    writeRegion(writer, region, indentLevel);\n" +
                    "}");
                metadata.AddField(field);
                metadata.AddComponentField(new BodyComponentField(BodyComponentKind.Regions, "Regions", field.Name));
                break;
            }

            case TypeDirectiveChunk typeDir:
            {
                var baseName = typeDir.Operand is VariableOperand varOp
                    ? DialectGeneratorNaming.ToPascalCase(varOp.Name) + "Type"
                    : "Type";
                var name = MakeUnique(baseName, usedNames);
                var field = new BodySyntaxField(name, "TypeSyntax",
                    name + ".WriteTo(writer, \" \");");
                metadata.AddField(field);
                metadata.AddComponentField(new BodyComponentField(BodyComponentKind.Type, GetDirectiveOperandName(typeDir.Operand), field.Name));
                break;
            }

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
            {
                // qualified(...) does not change parsing behaviour, so represent the inner
                // type as a plain TypeSyntax field just like TypeDirectiveChunk does.
                var baseName = qualified.Operand is TypeDirectiveOperand tdo && tdo.Operand is VariableOperand tVar
                    ? DialectGeneratorNaming.ToPascalCase(tVar.Name) + "Type"
                    : qualified.Operand is VariableOperand vOp
                        ? DialectGeneratorNaming.ToPascalCase(vOp.Name) + "Type"
                        : "Type";
                var name = MakeUnique(baseName, usedNames);
                var field = new BodySyntaxField(name, "TypeSyntax",
                    name + ".WriteTo(writer, \" \");");
                metadata.AddField(field);
                metadata.AddComponentField(new BodyComponentField(BodyComponentKind.Type, GetDirectiveOperandName(qualified.Operand), field.Name));
                break;
            }

            case ResultsDirectiveChunk _:
            {
                var name = MakeUnique("ResultsType", usedNames);
                var field = new BodySyntaxField(name, "TypeSyntax",
                    name + ".WriteTo(writer, \" \");");
                metadata.AddField(field);
                metadata.AddComponentField(new BodyComponentField(BodyComponentKind.Type, "Results", field.Name));
                break;
            }

            case OptionalGroup optionalGroup:
            {
                // Each element inside an optional group contributes a nullable field.
                foreach (var inner in optionalGroup.ThenElements)
                {
                    AppendBodySyntaxFieldsNullable(usedNames, inner, operation, metadata);
                }

                if (optionalGroup.ElseElements != null)
                {
                    foreach (var inner in optionalGroup.ElseElements)
                    {
                        AppendBodySyntaxFieldsNullable(usedNames, inner, operation, metadata);
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
                    var kwName = MakeUnique(DialectGeneratorNaming.ToPascalCase(clause.Keyword) + "Keyword", usedNames);
                    var kwField = new BodySyntaxField(
                        kwName, "SyntaxToken?",
                        "if (" + kwName + ".HasValue) writer.WriteToken(" + kwName + ".Value, \" \");");
                    metadata.AddField(kwField);
                    metadata.AddComponentField(new BodyComponentField(BodyComponentKind.Literal, "Keyword:" + clause.Keyword, kwField.Name));

                    foreach (var oiElem in clause.Elements)
                    {
                        AppendBodySyntaxFieldsForOilistElement(usedNames, oiElem, operation, metadata);
                    }
                }

                break;
            }

            // CustomDirectiveChunk, FunctionalTypeDirectiveChunk, RefDirectiveChunk → not stored in this CST class
        }
    }

    /// <summary>
    /// Generates nullable body-syntax fields for an element that appears inside an optional group.
    /// </summary>
    public static void AppendBodySyntaxFieldsNullable(HashSet<string> usedNames, Element element, OperationModel operation, OperationBodySyntaxMetadata metadata)
    {
        switch (element)
        {
            case LiteralChunk literal:
                foreach (var lit in literal.Value)
                {
                    switch (lit)
                    {
                        case PunctuationLiteral punc:
                        {
                            var name = MakeUnique(GetPunctuationFieldName(punc.TokenKind), usedNames);
                            var field = new BodySyntaxField(name, "SyntaxToken?",
                                "if (" + name + ".HasValue) writer.WriteToken(" + name + ".Value, string.Empty);");
                            metadata.AddField(field);
                            metadata.AddComponentField(new BodyComponentField(BodyComponentKind.Literal, "Punctuation:" + punc.TokenKind, field.Name));
                            break;
                        }

                        case KeywordLiteral kw:
                        {
                            var name = MakeUnique(DialectGeneratorNaming.ToPascalCase(kw.Spelling) + "Keyword", usedNames);
                            var field = new BodySyntaxField(name, "SyntaxToken?",
                                "if (" + name + ".HasValue) writer.WriteToken(" + name + ".Value, \" \");");
                            metadata.AddField(field);
                            metadata.AddComponentField(new BodyComponentField(BodyComponentKind.Literal, "Keyword:" + kw.Spelling, field.Name));
                            break;
                        }
                    }
                }

                break;

            case VariableChunk variable:
            {
                var pascalName = DialectGeneratorNaming.ToPascalCase(variable.Name);
                if (ContainsName(operation.Attributes, variable.Name))
                {
                    var name = MakeUnique(pascalName, usedNames);
                    var field = new BodySyntaxField(name, "AttributeValueSyntax?",
                        name + "?.WriteTo(writer, \" \");");
                    metadata.AddField(field);
                    metadata.AddComponentField(new BodyComponentField(BodyComponentKind.Attribute, variable.Name, field.Name));
                }
                else
                {
                    var name = MakeUnique(pascalName, usedNames);
                    var field = new BodySyntaxField(name, "SyntaxToken?",
                        "if (" + name + ".HasValue) writer.WriteToken(" + name + ".Value, \" \");");
                    metadata.AddField(field);
                    metadata.AddComponentField(new BodyComponentField(
                        GetComponentKindForVariable(operation, variable.Name),
                        variable.Name,
                        field.Name));
                }

                break;
            }

            case TypeDirectiveChunk typeDir:
            {
                var baseName = typeDir.Operand is VariableOperand varOp
                    ? DialectGeneratorNaming.ToPascalCase(varOp.Name) + "Type"
                    : "Type";
                var name = MakeUnique(baseName, usedNames);
                var field = new BodySyntaxField(name, "TypeSyntax?",
                    name + "?.WriteTo(writer, \" \");");
                metadata.AddField(field);
                metadata.AddComponentField(new BodyComponentField(BodyComponentKind.Type, GetDirectiveOperandName(typeDir.Operand), field.Name));
                break;
            }

            // Other element types inside optional groups are uncommon; if encountered they are skipped here.
        }
    }

    /// <summary>
    /// Generates nullable body-syntax fields for a single element within an oilist clause.
    /// </summary>
    private static void AppendBodySyntaxFieldsForOilistElement(HashSet<string> usedNames, OilistElement element, OperationModel operation, OperationBodySyntaxMetadata metadata)
    {
        switch (element)
        {
            case OilistVariableElement variable:
            {
                var pascalName = DialectGeneratorNaming.ToPascalCase(variable.Name);
                if (ContainsName(operation.Attributes, variable.Name))
                {
                    var name = MakeUnique(pascalName, usedNames);
                    var field = new BodySyntaxField(name, "AttributeValueSyntax?",
                        name + "?.WriteTo(writer, \" \");");
                    metadata.AddField(field);
                    metadata.AddComponentField(new BodyComponentField(BodyComponentKind.Attribute, variable.Name, field.Name));
                }
                else
                {
                    var name = MakeUnique(pascalName, usedNames);
                    var field = new BodySyntaxField(name, "SyntaxToken?",
                        "if (" + name + ".HasValue) writer.WriteToken(" + name + ".Value, \" \");");
                    metadata.AddField(field);
                    metadata.AddComponentField(new BodyComponentField(
                        GetComponentKindForVariable(operation, variable.Name),
                        variable.Name,
                        field.Name));
                }

                break;
            }

            case OilistTypeDirectiveElement typeDir:
            {
                var baseName = typeDir.Operand is VariableOperand varOp
                    ? DialectGeneratorNaming.ToPascalCase(varOp.Name) + "Type"
                    : "Type";
                var name = MakeUnique(baseName, usedNames);
                var field = new BodySyntaxField(name, "TypeSyntax?",
                    name + "?.WriteTo(writer, \" \");");
                metadata.AddField(field);
                metadata.AddComponentField(new BodyComponentField(BodyComponentKind.Type, GetDirectiveOperandName(typeDir.Operand), field.Name));
                break;
            }

            case OilistLiteralElement literal:
            {
                var spellingPascal = DialectGeneratorNaming.ToPascalCase(literal.Value);
                var name = MakeUnique(spellingPascal + "Token", usedNames);
                var field = new BodySyntaxField(name, "SyntaxToken?",
                    "if (" + name + ".HasValue) writer.WriteToken(" + name + ".Value, string.Empty);");
                metadata.AddField(field);
                metadata.AddComponentField(new BodyComponentField(BodyComponentKind.Literal, "OilistLiteral:" + literal.Value, field.Name));
                break;
            }
        }
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
