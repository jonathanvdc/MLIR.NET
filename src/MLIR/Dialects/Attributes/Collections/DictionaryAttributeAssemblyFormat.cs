namespace MLIR.Dialects.Attributes.Collections;

using System.Collections.Generic;
using MLIR.Dialects;
using MLIR.Semantics;
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
        return new DictionaryAttr(
            MLIR.Semantics.Attributes.Collections.StructuredAttributeSemanticDecoder.DecodeAttributes(normalizedSyntax.Attributes.Items),
            normalizedSyntax);
    }

    /// <inheritdoc/>
    public AttributeValueSyntax BuildCustomAssemblySyntax(AttributeValue attribute, ConcreteSyntaxBuilderContext context)
    {
        if (attribute is not DictionaryAttr dictionaryAttribute)
        {
            return attribute.Syntax ?? throw new System.InvalidOperationException("Dictionary attributes require syntax to rebuild their assembly form.");
        }

        return BuildSyntax(dictionaryAttribute.Value, context);
    }

    internal static DictionaryAttributeValueSyntax BuildSyntax(NamedAttributeCollection attributes, ConcreteSyntaxBuilderContext context)
    {
        var items = new List<NamedAttributeSyntax>(attributes.Count);
        var separators = new List<Token>(attributes.Count > 0 ? attributes.Count - 1 : 0);
        for (var i = 0; i < attributes.Count; i++)
        {
            items.Add(context.BuildNamedAttributeSyntax(attributes[i]));
            if (i > 0)
            {
                separators.Add(TokenFactory.Comma());
            }
        }

        return new DictionaryAttributeValueSyntax(
            new DelimitedSyntaxList<NamedAttributeSyntax>(TokenFactory.LBrace(), items, separators, TokenFactory.RBrace()));
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
