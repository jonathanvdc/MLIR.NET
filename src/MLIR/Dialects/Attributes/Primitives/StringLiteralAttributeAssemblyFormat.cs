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
    public bool TryParse(AttributeParsingContext context, out AttributeValueSyntax? syntax)
    {
        syntax = null;
        if (!context.TryMatch(TokenKind.StringLiteral, out var token))
        {
            return false;
        }

        syntax = new StringAttributeValueSyntax(token, Unescape(token.Text));
        return true;
    }

    /// <inheritdoc/>
    public AttributeValue Bind(AttributeValueSyntax syntax, AttributeConstraintDefinition definition, Binder binder)
    {
        var normalizedSyntax = syntax as StringAttributeValueSyntax
            ?? new StringAttributeValueSyntax(new SyntaxToken(syntax.GetRawText().Text), Unescape(syntax.GetRawText().Text));
        return definition.Factory(new AttributeValueConstructionContext(normalizedSyntax, definition.Name, definition, normalizedSyntax.Location));
    }

    /// <inheritdoc/>
    public AttributeValueSyntax BuildCustomAssemblySyntax(AttributeValue attribute, ConcreteSyntaxBuilderContext context)
    {
        if (attribute is StringAttributeValue stringAttribute)
        {
            var quoted = Quote(stringAttribute.Value);
            return new StringAttributeValueSyntax(new SyntaxToken(quoted), stringAttribute.Value);
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

    internal static string Quote(string value)
    {
        return "\"" + value
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r")
            .Replace("\t", "\\t") + "\"";
    }
}
