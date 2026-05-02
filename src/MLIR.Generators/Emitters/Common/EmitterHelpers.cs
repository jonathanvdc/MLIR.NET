namespace MLIR.Generators.Emitters.Common;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using MLIR.Generators.Emitters.Operation;
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

    public static string ToCSharpCharLiteral(char value)
    {
        return "'" + (value switch
        {
            '\\' => "\\\\",
            '\'' => "\\'",
            '\n' => "\\n",
            '\r' => "\\r",
            '\t' => "\\t",
            _ => value.ToString(),
        }) + "'";
    }

    public static string EscapeForStringLiteral(string text, bool escapeSingleQuote = false)
    {
        var escaped = text.Replace("\\", "\\\\").Replace("\"", "\\\"");
        if (escapeSingleQuote)
        {
            escaped = escaped.Replace("'", "\\'");
        }

        return escaped;
    }

    public static string CapitalizeFirst(string s)
    {
        return s.Length == 0 ? s : char.ToUpperInvariant(s[0]) + s.Substring(1);
    }

    public static string GetPunctuationText(TokenKind kind)
    {
        return kind switch
        {
            TokenKind.Comma => ",",
            TokenKind.LParen => "(",
            TokenKind.RParen => ")",
            TokenKind.LBracket => "[",
            TokenKind.RBracket => "]",
            TokenKind.LBrace => "{",
            TokenKind.RBrace => "}",
            TokenKind.LessThan => "<",
            TokenKind.GreaterThan => ">",
            TokenKind.Question => "?",
            TokenKind.Star => "*",
            TokenKind.Plus => "+",
            TokenKind.Minus => "-",
            TokenKind.Dot => ".",
            TokenKind.Colon => ":",
            TokenKind.Equal => "=",
            TokenKind.At => "@",
            TokenKind.Hash => "#",
            TokenKind.Arrow => "->",
            _ => kind.ToString(),
        };
    }

    /// <summary>
    /// Returns the C# expression used to synthesize a token of the supplied kind.
    /// Fixed-text punctuation maps to the dedicated <c>TokenFactory</c> methods, while
    /// variable-text tokens use the corresponding factory method that accepts text.
    /// </summary>
    public static string GetSyntaxTokenFactoryExpression(TokenKind kind, string? textLiteral = null)
    {
        return kind switch
        {
            TokenKind.Comma => "TokenFactory.Comma()",
            TokenKind.LParen => "TokenFactory.LParen()",
            TokenKind.RParen => "TokenFactory.RParen()",
            TokenKind.LBracket => "TokenFactory.LBracket()",
            TokenKind.RBracket => "TokenFactory.RBracket()",
            TokenKind.LBrace => "TokenFactory.LBrace()",
            TokenKind.RBrace => "TokenFactory.RBrace()",
            TokenKind.LessThan => "TokenFactory.LessThan()",
            TokenKind.GreaterThan => "TokenFactory.GreaterThan()",
            TokenKind.Question => "TokenFactory.Question()",
            TokenKind.Star => "TokenFactory.Star()",
            TokenKind.Plus => "TokenFactory.Plus()",
            TokenKind.Minus => "TokenFactory.Minus()",
            TokenKind.Dot => "TokenFactory.Dot()",
            TokenKind.Colon => "TokenFactory.Colon()",
            TokenKind.Equal => "TokenFactory.Equal()",
            TokenKind.At => "TokenFactory.At()",
            TokenKind.Hash => "TokenFactory.Hash()",
            TokenKind.Arrow => "TokenFactory.Arrow()",
            TokenKind.Identifier => "TokenFactory.Identifier(" + ToCSharpStringLiteral(textLiteral ?? string.Empty) + ")",
            TokenKind.Integer => "TokenFactory.Integer(" + ToCSharpStringLiteral(textLiteral ?? string.Empty) + ")",
            TokenKind.StringLiteral => "TokenFactory.StringLiteral(" + ToCSharpStringLiteral(textLiteral ?? string.Empty) + ")",
            TokenKind.SymbolName => "TokenFactory.SymbolName(" + ToCSharpStringLiteral(textLiteral ?? string.Empty) + ")",
            TokenKind.SsaName => "TokenFactory.SsaName(" + ToCSharpStringLiteral(textLiteral ?? string.Empty) + ")",
            TokenKind.BlockLabel => "TokenFactory.BlockLabel(" + ToCSharpStringLiteral(textLiteral ?? string.Empty) + ")",
            TokenKind.EndOfFile => "TokenFactory.EndOfFile()",
            _ => throw new NotSupportedException("Unsupported token kind: " + kind),
        };
    }

    public static BodySyntaxField NextBodySyntaxField(IReadOnlyList<BodySyntaxField> fields, ref int fieldIndex)
    {
        return fields[fieldIndex++];
    }

    public static string GetBodySyntaxFieldLocalName(BodySyntaxField field)
    {
        return LowerFirst(field.Name);
    }

    public static void AppendSeparated(StringBuilder builder, int count, Action<int> emitItem, string separator = ", ")
    {
        for (var i = 0; i < count; i++)
        {
            if (i > 0)
            {
                builder.Append(separator);
            }

            emitItem(i);
        }
    }

    /// <summary>
    /// Emits a constructor call for a definition object such as <c>TypeDefinition</c> or
    /// <c>AttributeDefinition</c>.
    /// </summary>
    /// <remarks>
    /// The helper keeps the common shape in one place: a definition name and an optional assembly
    /// format object.
    /// </remarks>
    public static void AppendDefinitionConstructor(
        StringBuilder builder,
        string definitionTypeName,
        string name,
        string? assemblyFormatExpression = null)
    {
        builder.Append("        new " + definitionTypeName + "(" + ToCSharpStringLiteral(name));
        if (assemblyFormatExpression != null)
        {
            builder.Append(", " + assemblyFormatExpression);
        }

        builder.AppendLine(");");
    }

    /// <summary>
    /// Appends XML doc comment lines for the supplied ODS <paramref name="summary"/> and
    /// <paramref name="description"/> fields to <paramref name="builder"/>.
    /// </summary>
    /// <remarks>
    /// The summary is placed in a single-line <c>&lt;summary&gt;</c> tag.
    /// The description is interpreted as Markdown and converted to structured XML doc
    /// comment content inside a <c>&lt;remarks&gt;</c> block.  Paragraphs, fenced code
    /// blocks, ATX headings, inline code, and inline links are all translated to their
    /// XML doc equivalents.
    /// </remarks>
    public static void AppendXmlDocComment(StringBuilder builder, string? summary, string? description, string indent = "")
    {
        if (!string.IsNullOrWhiteSpace(summary))
        {
            builder.AppendLine(indent + "/// <summary>" + EscapeXmlText(summary!.Trim()) + "</summary>");
        }

        if (!string.IsNullOrWhiteSpace(description))
        {
            builder.AppendLine(indent + "/// <remarks>");
            // Pass the raw description to ConvertToRemarksLines, which handles dedenting
            // and blank-line normalization internally.  Calling Trim() here would strip
            // the leading whitespace only from the first line, which breaks the dedent
            // calculation when ODS multi-line strings indent every line consistently.
            var remarksLines = MarkdownXmlDocConverter.ConvertToRemarksLines(description!);
            foreach (var line in remarksLines)
            {
                // Emit a bare `///` for blank lines so that empty XML doc lines do not
                // include a trailing space (consistent with standard C# doc comment style).
                builder.AppendLine(string.IsNullOrEmpty(line) ? indent + "///" : indent + "/// " + line);
            }

            builder.AppendLine(indent + "/// </remarks>");
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
    /// Returns the C# statement(s) that merge the source location contribution of a
    /// generated body-syntax field into a local variable named <c>result</c>.
    /// </summary>
    /// <param name="field">The body syntax field whose location is to be merged.</param>
    /// <returns>One or more C# statements, each terminated with a semicolon.</returns>
    public static string GetLocationMergeCode(BodySyntaxField field)
    {
        var name = field.Name;
        var type = field.CsType;

        // Nullable Token
        if (string.Equals(type, "Token?", StringComparison.Ordinal))
        {
            return "if (" + name + ".HasValue) result = SourceLocation.Merge(result, " + name + ".Value.Location);";
        }

        // Non-nullable Token
        if (string.Equals(type, "Token", StringComparison.Ordinal))
        {
            return "result = SourceLocation.Merge(result, " + name + ".Location);";
        }

        // DelimitedSyntaxList<T> – merge the open and close delimiter tokens
        if (type.StartsWith("DelimitedSyntaxList<", StringComparison.Ordinal))
        {
            return
                "if (" + name + ".OpenToken.HasValue) result = SourceLocation.Merge(result, " + name + ".OpenToken.Value.Location);\n" +
                "if (" + name + ".CloseToken.HasValue) result = SourceLocation.Merge(result, " + name + ".CloseToken.Value.Location);";
        }

        // IReadOnlyList<T> (regions, tokens, types, etc.) – merge every element
        if (type.Contains("IReadOnlyList<"))
        {
            return "foreach (var _loc_item in " + name + ") result = SourceLocation.Merge(result, _loc_item.Location);";
        }

        // Nullable reference types (TypeSyntax?, RegionSyntax?, AttributeValueSyntax?, …)
        if (type.EndsWith("?", StringComparison.Ordinal))
        {
            return "if (" + name + " != null) result = SourceLocation.Merge(result, " + name + ".Location);";
        }

        // Non-nullable reference types with a Location property (TypeSyntax, RegionSyntax, …)
        return "result = SourceLocation.Merge(result, " + name + ".Location);";
    }
}
