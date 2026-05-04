namespace MLIR.Dialects.Builtin;

using System;
using System.Linq;
using MLIR.Semantics;
using MLIR.Syntax;
using MLIR.Syntax.Attributes;
using MLIR.Text;
using MLIR.Transforms;

/// <summary>
/// Parses, binds, and rebuilds builtin opaque attributes of the form <c>#dialect&lt;data&gt;</c>.
/// </summary>
public sealed class BuiltinOpaqueAttributeAssemblyFormat : IAttributeAssemblyFormat
{
    /// <inheritdoc/>
    public ParseResult<AttributeValueSyntax> TryParse(ParsingContext context)
    {
        if (!context.TryMatch(TokenKind.Hash, out var hashToken))
        {
            return ParseResult<AttributeValueSyntax>.NoMatch();
        }

        if (!context.TryMatch(TokenKind.Identifier, out var dialectToken))
        {
            return ParseResult<AttributeValueSyntax>.NoMatch();
        }

        var lessThanResult = context.Expect(TokenKind.LessThan, "Expected '<' after opaque attribute dialect namespace.");
        if (!lessThanResult.IsSuccess)
        {
            return ParseResult<AttributeValueSyntax>.Failure(lessThanResult.Diagnostic!);
        }

        var payloadResult = context.TryParseRawUntilDelimiter(TokenKind.GreaterThan);
        if (!payloadResult.IsSuccess)
        {
            return ParseResult<AttributeValueSyntax>.Failure(payloadResult.Diagnostic!);
        }

        var greaterThanResult = context.Expect(TokenKind.GreaterThan, "Expected '>' to close opaque attribute data.");
        if (!greaterThanResult.IsSuccess)
        {
            return ParseResult<AttributeValueSyntax>.Failure(greaterThanResult.Diagnostic!);
        }

        return ParseResult<AttributeValueSyntax>.Success(new OpaqueAttributeValueSyntax(new RawSyntaxText(
        [
            hashToken,
            dialectToken,
            lessThanResult.Value,
            .. payloadResult.Value.Tokens,
            greaterThanResult.Value,
        ])));
    }

    /// <inheritdoc/>
    public AttributeValue Bind(AttributeValueSyntax syntax, Binder binder)
    {
        var resultSyntax = syntax;
        if (syntax is TypedAttributeValueSyntax typedSyntax)
        {
            syntax = typedSyntax.AttributeSyntax;
        }

        if (syntax is not OpaqueAttributeValueSyntax opaqueSyntax)
        {
            throw new InvalidOperationException("Opaque attributes require opaque attribute syntax.");
        }

        return Decode(opaqueSyntax, resultSyntax);
    }

    /// <inheritdoc/>
    public AttributeValueSyntax BuildCustomAssemblySyntax(AttributeValue attribute, ConcreteSyntaxBuilderContext context)
    {
        if (attribute.Syntax is OpaqueAttributeValueSyntax opaqueSyntax)
        {
            return opaqueSyntax;
        }

        if (attribute is not OpaqueAttr opaqueAttr)
        {
            return attribute.Syntax ?? throw new InvalidOperationException("Opaque attributes require OpaqueAttr storage or reusable syntax.");
        }

        return new OpaqueAttributeValueSyntax(new RawSyntaxText(
            [
                TokenFactory.Hash(),
                TokenFactory.Identifier(opaqueAttr.DialectNamespace),
                TokenFactory.LessThan(),
                TokenFactory.Identifier(opaqueAttr.AttrData),
                TokenFactory.GreaterThan(),
            ]));
    }

    /// <summary>
    /// Decodes opaque attribute syntax into generated builtin opaque attribute storage.
    /// </summary>
    public static OpaqueAttr Decode(OpaqueAttributeValueSyntax syntax)
    {
        return Decode(syntax, syntax);
    }

    private static OpaqueAttr Decode(OpaqueAttributeValueSyntax syntax, AttributeValueSyntax resultSyntax)
    {
        var tokens = syntax.RawText.Tokens;
        if (tokens.Count < 4 ||
            tokens[0].TokenKind != TokenKind.Hash ||
            tokens[1].TokenKind != TokenKind.Identifier ||
            tokens[2].TokenKind != TokenKind.LessThan ||
            tokens[tokens.Count - 1].TokenKind != TokenKind.GreaterThan)
        {
            throw new InvalidOperationException("Opaque attribute syntax must have the form '#dialect<data>'.");
        }

        var dialectNamespace = tokens[1].Text;
        var attrDataStart = tokens[2].TokenStart + tokens[2].TokenLength;
        var attrDataEndToken = tokens[tokens.Count - 1];
        var attrData = attrDataEndToken.HasSourceLocation
            ? attrDataEndToken.Document!.GetText(attrDataStart, attrDataEndToken.TokenStart - attrDataStart)
            : string.Concat(tokens.Skip(3).Take(tokens.Count - 4).Select(static token => token.FullText));

        return new OpaqueAttr(dialectNamespace, attrData, TypeFactory.None, resultSyntax);
    }
}
