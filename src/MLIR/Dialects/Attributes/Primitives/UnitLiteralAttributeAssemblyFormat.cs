namespace MLIR.Dialects.Attributes.Primitives;

using MLIR.Dialects;
using MLIR.Dialects.Builtin;
using MLIR.Semantics;
using MLIR.Syntax;
using MLIR.Syntax.Attributes;
using MLIR.Text;
using MLIR.Transforms;

/// <summary>
/// Parses unit attribute literals.
/// </summary>
public sealed class UnitLiteralAttributeAssemblyFormat : IAttributeAssemblyFormat
{
    /// <inheritdoc/>
    public ParseResult<AttributeValueSyntax> TryParse(AttributeParsingContext context)
    {
        if (context.TryMatch(TokenKind.Identifier, out var token))
        {
            if (token.Text == "unit")
            {
                return ParseResult<AttributeValueSyntax>.Success(new UnitAttributeValueSyntax(token));
            }
            else if (token.Text == "#builtin")
            {
                // Special case for the 'builtin' dialect namespace, which is reserved for built-in attributes and types.
                // This allows parsing of the '#builtin.unit' syntax.
                if (context.TryMatch(TokenKind.Dot, out _) && context.TryMatch(TokenKind.Identifier, out var attrName) && attrName.Text == "unit")
                {
                    return ParseResult<AttributeValueSyntax>.Success(new UnitAttributeValueSyntax(attrName));
                }
            }
        }

        return ParseResult<AttributeValueSyntax>.NoMatch();
    }

    /// <inheritdoc/>
    public AttributeValue Bind(AttributeValueSyntax syntax, AttributeConstraintDefinition definition, Binder binder)
    {
        return new UnitAttr(syntax);
    }

    /// <inheritdoc/>
    public AttributeValueSyntax BuildCustomAssemblySyntax(AttributeValue attribute, ConcreteSyntaxBuilderContext context)
    {
        return attribute.Syntax ?? new UnitAttributeValueSyntax(TokenFactory.Identifier("unit"));
    }
}
