namespace MLIR.Dialects.Attributes.Primitives;

using MLIR;
using MLIR.Dialects;
using MLIR.Dialects.Builtin;
using MLIR.Semantics;
using MLIR.Syntax;
using MLIR.Syntax.Attributes;
using MLIR.Syntax.Attributes.Primitives;
using MLIR.Text;
using MLIR.Transforms;

/// <summary>
/// Parses primitive boolean attribute literals.
/// </summary>
public sealed class BooleanLiteralAttributeAssemblyFormat : IAttributeAssemblyFormat
{
    /// <inheritdoc/>
    public ParseResult<AttributeValueSyntax> TryParse(AttributeParsingContext context)
    {
        if (!context.TryMatch(TokenKind.Identifier, out var identifier))
        {
            return ParseResult<AttributeValueSyntax>.NoMatch();
        }

        if (identifier.Text == "true")
        {
            return ParseResult<AttributeValueSyntax>.Success(new BooleanAttributeValueSyntax(identifier, true));
        }

        if (identifier.Text == "false")
        {
            return ParseResult<AttributeValueSyntax>.Success(new BooleanAttributeValueSyntax(identifier, false));
        }

        return ParseResult<AttributeValueSyntax>.NoMatch();
    }

    /// <inheritdoc/>
    public AttributeValue Bind(AttributeValueSyntax syntax, AttributeConstraintDefinition definition, Binder binder)
    {
        if (syntax is TypedAttributeValueSyntax typedSyntax)
        {
            syntax = typedSyntax.AttributeSyntax;
        }

        if (syntax is not BooleanAttributeValueSyntax booleanSyntax)
        {
            throw new InvalidOperationException("Expected a boolean literal syntax for a primitive boolean attribute.");
        }

        return new IntegerAttr(TypeFactory.I1, MLIR.Numerics.ApInt.FromInt64(1, booleanSyntax.Value ? 1 : 0), booleanSyntax);
    }

    /// <inheritdoc/>
    public AttributeValueSyntax BuildCustomAssemblySyntax(AttributeValue attribute, ConcreteSyntaxBuilderContext context)
    {
        if (attribute is IntegerAttr integerAttr)
        {
            var value = integerAttr.Value.ToUInt64() != 0;
            return new BooleanAttributeValueSyntax(TokenFactory.Identifier(value ? "true" : "false"), value);
        }

        // Fallback: use existing syntax for attributes that aren't IntegerAttr
        // (e.g., a user-defined test attribute).
        if (attribute.Syntax is BooleanAttributeValueSyntax boolSyntax)
        {
            return new BooleanAttributeValueSyntax(TokenFactory.Identifier(boolSyntax.Value ? "true" : "false"), boolSyntax.Value);
        }

        return attribute.Syntax ?? throw new System.InvalidOperationException("Primitive boolean attributes require syntax to rebuild their assembly form.");
    }
}
