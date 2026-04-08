namespace MLIR.Dialects.Attributes.Collections;

using System.Collections.Generic;
using MLIR.Dialects;
using MLIR.Semantics;
using MLIR.Semantics.Attributes.Collections;
using MLIR.Syntax;
using MLIR.Syntax.Attributes.Collections;
using MLIR.Text;
using MLIR.Transforms;

/// <summary>
/// Parses dictionary attribute literals such as <c>{value = 1}</c>.
/// </summary>
public sealed class DictionaryAttributeAssemblyFormat : IAttributeAssemblyFormat
{
    /// <inheritdoc/>
    public ParseResult<AttributeValueSyntax> TryParse(AttributeParsingContext context)
    {
        if (!context.Is(TokenKind.LBrace))
        {
            return ParseResult<AttributeValueSyntax>.NoMatch();
        }

        return context.TryParseAttributeDictionarySyntax()
            .Map<AttributeValueSyntax>(static dictionarySyntax => new DictionaryAttributeValueSyntax(dictionarySyntax));
    }

    /// <inheritdoc/>
    public AttributeValue Bind(AttributeValueSyntax syntax, AttributeConstraintDefinition definition, Binder binder)
    {
        var normalizedSyntax = NormalizeSyntax(syntax, definition, binder);
        return definition.Factory(new AttributeValueConstructionContext(normalizedSyntax, definition.Name, definition, normalizedSyntax.Location));
    }

    /// <inheritdoc/>
    public AttributeValueSyntax BuildCustomAssemblySyntax(AttributeValue attribute, ConcreteSyntaxBuilderContext context)
    {
        if (attribute is not DictionaryAttributeValue dictionaryAttribute)
        {
            return attribute.Syntax ?? throw new System.InvalidOperationException("Dictionary attributes require syntax to rebuild their assembly form.");
        }

        var items = new List<NamedAttributeSyntax>(dictionaryAttribute.Attributes.Count);
        var separators = new List<SyntaxToken>(dictionaryAttribute.Attributes.Count > 0 ? dictionaryAttribute.Attributes.Count - 1 : 0);
        for (var i = 0; i < dictionaryAttribute.Attributes.Count; i++)
        {
            items.Add(context.BuildNamedAttributeSyntax(dictionaryAttribute.Attributes[i]));
            if (i > 0)
            {
                separators.Add(SyntaxTokenFactory.Comma());
            }
        }

        return new DictionaryAttributeValueSyntax(
            new DelimitedSyntaxList<NamedAttributeSyntax>(SyntaxTokenFactory.LBrace(), items, separators, SyntaxTokenFactory.RBrace()));
    }

    private static DictionaryAttributeValueSyntax NormalizeSyntax(AttributeValueSyntax syntax, AttributeConstraintDefinition definition, Binder binder)
    {
        if (syntax is DictionaryAttributeValueSyntax dictionarySyntax)
        {
            return dictionarySyntax;
        }

        throw new InvalidOperationException("Unexpected syntax for dictionary attribute. Expected a dictionary attribute literal such as '{value = 1}'.");
    }
}
