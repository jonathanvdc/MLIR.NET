namespace MLIR.Dialects.Attributes.Primitives;

using System.Text;
using MLIR.Dialects;
using MLIR.Semantics;
using MLIR.Semantics.Attributes.Primitives;
using MLIR.Syntax;
using MLIR.Syntax.Attributes.Primitives;
using MLIR.Text;
using MLIR.Transforms;

/// <summary>
/// Parses primitive string attribute literals.
/// </summary>
public sealed class StringLiteralAttributeAssemblyFormat : IAttributeAssemblyFormat
{
    /// <inheritdoc/>
    public ParseResult<AttributeValueSyntax> TryParse(AttributeParsingContext context)
    {
        if (!context.TryMatch(TokenKind.StringLiteral, out var token))
        {
            return ParseResult<AttributeValueSyntax>.NoMatch();
        }

        return ParseResult<AttributeValueSyntax>.Success(new StringAttributeValueSyntax(token, Unescape(token.Text)));
    }

    /// <inheritdoc/>
    public AttributeValue Bind(AttributeValueSyntax syntax, AttributeConstraintDefinition definition, Binder binder)
    {
        if (syntax is not StringAttributeValueSyntax stringSyntax)
        {
            throw new InvalidOperationException("Expected a string literal syntax for a primitive string attribute.");
        }

        return definition.Factory(new AttributeValueConstructionContext(stringSyntax, definition.Name, definition, stringSyntax.Location));
    }

    /// <inheritdoc/>
    public AttributeValueSyntax BuildCustomAssemblySyntax(AttributeValue attribute, ConcreteSyntaxBuilderContext context)
    {
        if (attribute is StringAttributeValue stringAttribute)
        {
            var quoted = Quote(stringAttribute.Value);
            return new StringAttributeValueSyntax(TokenFactory.StringLiteral(quoted), stringAttribute.Value);
        }

        return attribute.Syntax ?? throw new System.InvalidOperationException("Primitive string attributes require syntax to rebuild their assembly form.");
    }

    internal static string Unescape(string text)
    {
        if (text.Length >= 2 && text[0] == '"' && text[text.Length - 1] == '"')
        {
            text = text.Substring(1, text.Length - 2);
        }

        var builder = new StringBuilder(text.Length);
        var escaped = false;
        for (var i = 0; i < text.Length; i++)
        {
            var ch = text[i];
            if (!escaped)
            {
                if (ch == '\\')
                {
                    escaped = true;
                }
                else
                {
                    builder.Append(ch);
                }

                continue;
            }

            builder.Append(ch switch
            {
                '\\' => '\\',
                '"' => '"',
                'n' => '\n',
                'r' => '\r',
                't' => '\t',
                _ => ch,
            });
            escaped = false;
        }

        return builder.ToString();
    }

    /// <summary>
    /// Returns the supplied string wrapped in double-quote characters with internal special
    /// characters (<c>\</c>, <c>"</c>, newlines, carriage returns, and tabs) escaped using
    /// the MLIR string-literal escape conventions.
    /// </summary>
    /// <remarks>
    /// The inverse operation is <see cref="Unescape"/>.  This method is public so that
    /// generated attribute assembly-format printers can produce correct quoted string literals
    /// for <c>StringRefParameter</c>-backed attribute parameters.
    /// </remarks>
    /// <param name="value">The raw (unescaped) string to quote.</param>
    /// <returns>A quoted MLIR string literal, including the surrounding double-quote characters.</returns>
    public static string Quote(string value)
    {
        return "\"" + value
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r")
            .Replace("\t", "\\t") + "\"";
    }
}
