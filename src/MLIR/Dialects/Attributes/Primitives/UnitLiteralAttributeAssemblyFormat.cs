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
        if (!context.TryMatch(TokenKind.Identifier, out var token) || token.Text != "unit")
        {
            return ParseResult<AttributeValueSyntax>.NoMatch();
        }

        return ParseResult<AttributeValueSyntax>.Success(new UnitAttributeValueSyntax(token));
    }

    /// <inheritdoc/>
    public AttributeValue Bind(AttributeValueSyntax syntax, AttributeConstraintDefinition definition, Binder binder)
    {
        var normalizedSyntax = syntax as UnitAttributeValueSyntax ?? new UnitAttributeValueSyntax(TokenFactory.Identifier("unit"));
        return new UnitAttr(normalizedSyntax);
    }

    /// <inheritdoc/>
    public AttributeValueSyntax BuildCustomAssemblySyntax(AttributeValue attribute, ConcreteSyntaxBuilderContext context)
    {
        return attribute.Syntax ?? new UnitAttributeValueSyntax(TokenFactory.Identifier("unit"));
    }
}
