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
public sealed class UnitLiteralAttributeAssemblyFormat : IBodylessSelfIdentifyingAttributeAssemblyFormat
{
    private const string BuiltinUnitAttributeName = "builtin.unit";

    /// <inheritdoc/>
    public string SelfIdentifyingAttributeName => BuiltinUnitAttributeName;

    /// <inheritdoc/>
    public ParseResult<AttributeValueSyntax> TryParse(AttributeParsingContext context)
    {
        if (context.TryMatch(TokenKind.Identifier, out var token) && token.Text == "unit")
        {
            return ParseResult<AttributeValueSyntax>.Success(new UnitAttributeValueSyntax(token));
        }

        return ParseResult<AttributeValueSyntax>.NoMatch();
    }

    /// <inheritdoc/>
    public bool CanParseSelfIdentifyingAttribute(string name)
    {
        return name == SelfIdentifyingAttributeName;
    }

    /// <inheritdoc/>
    public AttributeValueSyntax CreateSelfIdentifyingSyntax(DialectAttributePrefix prefix)
    {
        return new PrefixedUnitAttributeValueSyntax(prefix);
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
