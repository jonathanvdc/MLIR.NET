namespace MLIR.Generators;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using MLIR.ODS.Model;
using MLIR.ODS.Model.AssemblyFormat;
using MLIR.Text;

internal static partial class DialectSourceEmitter
{
    private static void AppendBodySyntaxFields(HashSet<string> usedNames, Element element, OperationModel operation, OperationBodySyntaxMetadata metadata)
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
                    // Operand, result variable, or unknown → SyntaxToken
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

            // OptionalGroup, OilistDirectiveChunk, CustomDirectiveChunk, FunctionalTypeDirectiveChunk,
            // QualifiedDirectiveChunk, RefDirectiveChunk, ResultsDirectiveChunk → not stored in this CST class
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

    private static string MakeUnique(string baseName, HashSet<string> used)
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

    private static string LowerFirst(string name)
    {
        if (name.Length == 0)
        {
            return name;
        }

        return char.ToLowerInvariant(name[0]) + name.Substring(1);
    }

    private static bool ContainsName(IReadOnlyList<string> names, string name)
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

    private static BodyComponentKind GetComponentKindForVariable(OperationModel operation, string variableName)
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

    private static string GetDirectiveOperandName(DirectiveOperand operand)
    {
        return operand switch
        {
            VariableOperand variable => variable.Name,
            _ => operand?.GetType().Name ?? "Operand"
        };
    }

    private static void AppendIndentedCode(StringBuilder builder, string code)
    {
        if (code.Length == 0)
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

    private static string EscapeXmlText(string text)
    {
        return text
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;");
    }

    private sealed class BodySyntaxField
    {
        public BodySyntaxField(string name, string csType, string writeToCode)
        {
            Name = name;
            CsType = csType;
            WriteToCode = writeToCode;
        }

        public string Name { get; }
        public string CsType { get; }

        /// <summary>
        /// C# code (indented for the WriteTo body, ending with a newline) that writes this field.
        /// </summary>
        public string WriteToCode { get; }
    }

    private sealed class BodyComponentField
    {
        public BodyComponentField(BodyComponentKind kind, string componentName, string fieldName)
        {
            Kind = kind;
            ComponentName = componentName;
            FieldName = fieldName;
        }

        public BodyComponentKind Kind { get; }

        public string ComponentName { get; }

        public string FieldName { get; }
    }

    private enum BodyComponentKind
    {
        Literal,
        Attribute,
        Operand,
        Result,
        AttrDict,
        AttrDictWithKeyword,
        PropDict,
        Regions,
        Type,
        Successors,
        Operands,
        Unknown
    }

    private sealed class OperationBodySyntaxMetadata
    {
        private readonly List<BodySyntaxField> fields = new();
        private readonly List<BodyComponentField> componentFields = new();

        public OperationBodySyntaxMetadata(string operationClassName)
        {
            OperationClassName = operationClassName;
        }

        public string OperationClassName { get; }

        public string BodyClassName => OperationClassName + "BodySyntax";

        public IReadOnlyList<BodySyntaxField> Fields => fields;

        public IReadOnlyList<BodyComponentField> ComponentFields => componentFields;

        public void AddField(BodySyntaxField field) => fields.Add(field);

        public void AddComponentField(BodyComponentField componentField) => componentFields.Add(componentField);
    }
}
