namespace MLIR.Dialects.Attributes.Collections;

using System.Collections.Generic;
using MLIR;
using MLIR.Dialects;
using MLIR.Dialects.Builtin;
using MLIR.Semantics;
using MLIR.Syntax;
using MLIR.Syntax.Attributes;
using MLIR.Syntax.Attributes.Collections;
using MLIR.Text;
using MLIR.Transforms;

/// <summary>
/// Base class for parsing dense array attribute literals such as <c>array&lt;i32: 1, 2&gt;</c>.
/// Subclasses specialise element parsing and synthesis for a concrete element type.
/// </summary>
public abstract class DenseArrayAttributeAssemblyFormat<TElement> : IAttributeAssemblyFormat
{
    private readonly AttributeConstraintDefinition? definition;

    /// <summary>
    /// Initializes a new dense-array assembly format, optionally bound to a concrete attribute definition.
    /// </summary>
    protected DenseArrayAttributeAssemblyFormat(AttributeConstraintDefinition? definition = null)
    {
        this.definition = definition;
    }

    /// <inheritdoc/>
    public ParseResult<AttributeValueSyntax> TryParse(ParsingContext context)
    {
        if (!context.TryMatch(TokenKind.Identifier, out var keywordToken) || keywordToken.Text != "array")
        {
            return ParseResult<AttributeValueSyntax>.NoMatch();
        }

        var lessThanTokenResult = context.Expect(TokenKind.LessThan, "Expected '<' after 'array'.");
        if (!lessThanTokenResult.IsSuccess)
        {
            return ParseResult<AttributeValueSyntax>.Failure(lessThanTokenResult.Diagnostic!);
        }

        var elementTypeSyntaxResult = context.TryParseTypeSyntax();
        if (!elementTypeSyntaxResult.IsSuccess)
        {
            return ParseResult<AttributeValueSyntax>.Failure(elementTypeSyntaxResult.Diagnostic!);
        }

        var colonTokenResult = context.Expect(TokenKind.Colon, "Expected ':' after the dense array element type.");
        if (!colonTokenResult.IsSuccess)
        {
            return ParseResult<AttributeValueSyntax>.Failure(colonTokenResult.Diagnostic!);
        }

        var items = new List<AttributeValueSyntax>();
        var separators = new List<Token>();
        if (!context.Is(TokenKind.GreaterThan))
        {
            var firstItemResult = context.TryParseAttributeValueSyntax(TokenKind.Comma, TokenKind.GreaterThan);
            if (!firstItemResult.IsSuccess)
            {
                return ParseResult<AttributeValueSyntax>.Failure(firstItemResult.Diagnostic!);
            }

            items.Add(firstItemResult.Value);
            while (context.TryMatch(TokenKind.Comma, out var commaToken))
            {
                separators.Add(commaToken);
                var itemResult = context.TryParseAttributeValueSyntax(TokenKind.Comma, TokenKind.GreaterThan);
                if (!itemResult.IsSuccess)
                {
                    return ParseResult<AttributeValueSyntax>.Failure(itemResult.Diagnostic!);
                }

                items.Add(itemResult.Value);
            }
        }

        var greaterThanTokenResult = context.Expect(TokenKind.GreaterThan, "Expected '>' to close the dense array attribute.");
        if (!greaterThanTokenResult.IsSuccess)
        {
            return ParseResult<AttributeValueSyntax>.Failure(greaterThanTokenResult.Diagnostic!);
        }

        return ParseResult<AttributeValueSyntax>.Success(new DenseArrayAttributeValueSyntax(
            keywordToken,
            lessThanTokenResult.Value,
            elementTypeSyntaxResult.Value,
            colonTokenResult.Value,
            new SeparatedSyntaxList<AttributeValueSyntax>(items, separators),
            greaterThanTokenResult.Value));
    }

    /// <inheritdoc/>
    public AttributeValue Bind(AttributeValueSyntax syntax, Binder binder)
    {
        var resultSyntax = syntax;
        var normalizedSyntax = NormalizeSyntax(syntax, binder);
        var constraintName = definition?.Name ?? InferConstraintName(normalizedSyntax.ElementTypeSyntax);
        var items = new List<TElement>(normalizedSyntax.Items.Count);
        for (var i = 0; i < normalizedSyntax.Items.Count; i++)
        {
            items.Add(ElementFromSyntax(normalizedSyntax.Items[i]));
        }

        return CreateDenseArrayAttribute(resultSyntax, constraintName, items);
    }

    /// <inheritdoc/>
    public AttributeValueSyntax BuildCustomAssemblySyntax(AttributeValue attribute, ConcreteSyntaxBuilderContext context)
    {
        if (attribute is not DenseArrayAttr denseArray)
        {
            return attribute.Syntax ?? throw new System.InvalidOperationException("Dense array attributes require syntax to rebuild their assembly form.");
        }

        if (attribute.Syntax is DenseArrayAttributeValueSyntax denseArraySyntax)
        {
            return denseArraySyntax;
        }

        var constraintName = attribute.Definition?.Name ?? attribute.Name;
        var items = DecodeItems(constraintName, denseArray.RawData);
        var itemSyntax = new List<AttributeValueSyntax>(items.Count);
        for (var i = 0; i < items.Count; i++)
        {
            itemSyntax.Add(ElementToSyntax(items[i]));
        }

        var separators = new List<Token>(itemSyntax.Count > 0 ? itemSyntax.Count - 1 : 0);
        for (var i = 1; i < itemSyntax.Count; i++)
        {
            separators.Add(TokenFactory.Comma());
        }

        return new DenseArrayAttributeValueSyntax(
            TokenFactory.Identifier("array"),
            TokenFactory.LessThan(),
            GetElementTypeSyntax(attribute.Definition?.Name ?? attribute.Name),
            TokenFactory.Colon(),
            new SeparatedSyntaxList<AttributeValueSyntax>(itemSyntax, separators),
            TokenFactory.GreaterThan());
    }

    /// <summary>
    /// Converts a single element value to its attribute-value syntax representation.
    /// </summary>
    protected abstract AttributeValueSyntax ElementToSyntax(TElement element);

    /// <summary>
    /// Decodes a dense-element syntax node into a semantic element value.
    /// </summary>
    protected abstract TElement ElementFromSyntax(AttributeValueSyntax syntax);

    /// <summary>
    /// Encodes dense items to the byte payload used by <see cref="DenseArrayAttr"/>.
    /// </summary>
    protected abstract System.ReadOnlyMemory<byte> EncodeRawData(string? constraintName, IReadOnlyList<TElement> items);

    /// <summary>
    /// Decodes a raw dense byte payload into dense elements.
    /// </summary>
    protected abstract IReadOnlyList<TElement> DecodeItems(string? constraintName, System.ReadOnlyMemory<byte> rawData);

    /// <summary>
    /// Gets the semantic element type for the given dense-array constraint.
    /// </summary>
    protected abstract TypeReference GetElementType(string? constraintName);

    /// <summary>
    /// Returns the MLIR element-type syntax for the given constraint name (e.g. <c>i32</c>, <c>f32</c>).
    /// </summary>
    protected abstract TypeSyntax GetElementTypeSyntax(string? constraintName);

    private DenseArrayAttr CreateDenseArrayAttribute(
        AttributeValueSyntax syntax,
        string? constraintName,
        IReadOnlyList<TElement> items)
    {
        return new DenseArrayAttr(
            GetElementType(constraintName),
            items.Count,
            EncodeRawData(constraintName, items),
            syntax);
    }

    private static DenseArrayAttributeValueSyntax NormalizeSyntax(AttributeValueSyntax syntax, Binder binder)
    {
        if (syntax is TypedAttributeValueSyntax typedSyntax)
        {
            syntax = typedSyntax.AttributeSyntax;
        }

        if (syntax is DenseArrayAttributeValueSyntax denseArraySyntax)
        {
            return denseArraySyntax;
        }

        throw new System.InvalidOperationException("Unexpected syntax for dense array attribute. Expected a dense array attribute literal such as 'array<i32: 1, 2>'.");
    }

    private static string? InferConstraintName(TypeSyntax elementType)
    {
        return elementType.ToString() switch
        {
            "i1" => "DenseBoolArrayAttr",
            "i8" => "DenseI8ArrayAttr",
            "i16" => "DenseI16ArrayAttr",
            "i64" => "DenseI64ArrayAttr",
            "f32" => "DenseF32ArrayAttr",
            "f64" => "DenseF64ArrayAttr",
            _ => "DenseI32ArrayAttr"
        };
    }
}
