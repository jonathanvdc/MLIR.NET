namespace MLIR.Dialects.Attributes;

using MLIR.Dialects;
using MLIR.Semantics;
using MLIR.Semantics.Attributes;
using MLIR.Syntax;
using MLIR.Syntax.Attributes;
using MLIR.Text;
using MLIR.Transforms;

/// <summary>
/// Parses unit attribute literals.
/// </summary>
public sealed class UnitAttributeAssemblyFormat : IAttributeAssemblyFormat
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
        var normalizedSyntax = syntax as UnitAttributeValueSyntax ?? new UnitAttributeValueSyntax(SyntaxTokenFactory.Identifier("unit"));
        return definition.Factory(new AttributeValueConstructionContext(normalizedSyntax, definition.Name, definition, normalizedSyntax.Location));
    }

    /// <inheritdoc/>
    public AttributeValueSyntax BuildCustomAssemblySyntax(AttributeValue attribute, ConcreteSyntaxBuilderContext context)
    {
        return attribute.Syntax ?? new UnitAttributeValueSyntax(SyntaxTokenFactory.Identifier("unit"));
    }
}
