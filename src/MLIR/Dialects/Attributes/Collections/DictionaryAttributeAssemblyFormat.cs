namespace MLIR.Dialects.Attributes.Collections;

using System.Collections.Generic;
using MLIR.Dialects;
using MLIR.Dialects.Builtin;
using MLIR.Semantics;
using MLIR.Syntax;
using MLIR.Syntax.Attributes;
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
    public AttributeValue Bind(AttributeValueSyntax syntax, Binder binder)
    {
        var resultSyntax = syntax;
        var normalizedSyntax = NormalizeSyntax(syntax, binder);
        return new DictionaryAttr(BindAttributesFromSyntax(normalizedSyntax.Attributes.Items, binder), resultSyntax);
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

    /// <summary>
    /// Binds dictionary item syntax through the normal attribute binder.
    /// </summary>
    /// <param name="attributes">The named attribute syntax items in the dictionary.</param>
    /// <param name="binder">The binder to use, or <see langword="null"/> to use a syntax-only binder.</param>
    /// <returns>The bound named attribute collection.</returns>
    public static NamedAttributeCollection BindAttributesFromSyntax(IReadOnlyList<NamedAttributeSyntax> attributes, Binder? binder = null)
    {
        binder ??= new Binder(null);
        return binder.BindNamedAttributes(attributes);
    }

    private static DictionaryAttributeValueSyntax NormalizeSyntax(AttributeValueSyntax syntax, Binder binder)
    {
        if (syntax is TypedAttributeValueSyntax typedSyntax)
        {
            syntax = typedSyntax.AttributeSyntax;
        }

        if (syntax is DictionaryAttributeValueSyntax dictionarySyntax)
        {
            return dictionarySyntax;
        }

        throw new InvalidOperationException("Unexpected syntax for dictionary attribute. Expected a dictionary attribute literal such as '{value = 1}'.");
    }
}
