namespace MLIR.Dialects.Builtin;

using System;
using System.Collections.Generic;
using MLIR.Dialects.Attributes.Primitives;
using MLIR.Semantics;
using MLIR.Syntax;
using MLIR.Syntax.Attributes;
using MLIR.Text;
using MLIR.Transforms;

/// <summary>
/// Parses, binds, and rebuilds builtin symbol-reference attributes such as
/// <c>@callee</c> and <c>@module::@function</c>.
/// </summary>
public sealed class BuiltinSymbolRefAttributeAssemblyFormat : IAttributeAssemblyFormat
{
    /// <inheritdoc/>
    public ParseResult<AttributeValueSyntax> TryParse(AttributeParsingContext context)
    {
        if (!context.TryMatch(TokenKind.SymbolName, out var rootSymbolNameToken))
        {
            return ParseResult<AttributeValueSyntax>.NoMatch();
        }

        var nestedReferences = new List<SymbolRefNestedReferenceSyntax>();

        while (IsNestedReferenceSeparator(context))
        {
            var firstColonResult = context.Expect(TokenKind.Colon, "Expected ':' in symbol reference separator.");
            if (!firstColonResult.IsSuccess)
            {
                return ParseResult<AttributeValueSyntax>.Failure(firstColonResult.Diagnostic!);
            }

            var secondColonResult = context.Expect(TokenKind.Colon, "Expected '::' in nested symbol reference.");
            if (!secondColonResult.IsSuccess)
            {
                return ParseResult<AttributeValueSyntax>.Failure(secondColonResult.Diagnostic!);
            }

            var nestedNameResult = context.Expect(TokenKind.SymbolName, "Expected a symbol name after '::' in nested symbol reference.");
            if (!nestedNameResult.IsSuccess)
            {
                return ParseResult<AttributeValueSyntax>.Failure(nestedNameResult.Diagnostic!);
            }

            nestedReferences.Add(new SymbolRefNestedReferenceSyntax(
                firstColonResult.Value,
                secondColonResult.Value,
                nestedNameResult.Value));
        }

        return ParseResult<AttributeValueSyntax>.Success(
            new SymbolRefAttributeValueSyntax(rootSymbolNameToken, nestedReferences));
    }

    /// <inheritdoc/>
    public AttributeValue Bind(AttributeValueSyntax syntax, AttributeConstraintDefinition definition, Binder binder)
    {
        var resultSyntax = syntax;
        if (syntax is TypedAttributeValueSyntax typedSyntax)
        {
            syntax = typedSyntax.AttributeSyntax;
        }

        if (syntax is not SymbolRefAttributeValueSyntax symbolSyntax)
        {
            throw new InvalidOperationException("Symbol-reference attributes require symbol-reference syntax.");
        }

        var nestedReferences = new string[Math.Max(0, symbolSyntax.Count - 1)];
        for (var i = 0; i < symbolSyntax.NestedReferences.Count; i++)
        {
            nestedReferences[i] = DecodeSymbolName(symbolSyntax.NestedReferences[i].SymbolNameToken);
        }

        return new SymbolRefAttr(DecodeSymbolName(symbolSyntax.RootSymbolNameToken), nestedReferences, resultSyntax);
    }

    /// <inheritdoc/>
    public AttributeValueSyntax BuildCustomAssemblySyntax(AttributeValue attribute, ConcreteSyntaxBuilderContext context)
    {
        if (attribute is not SymbolRefAttr symbolRef)
        {
            return attribute.Syntax ?? throw new InvalidOperationException("Symbol-reference attributes require SymbolRefAttr storage or reusable syntax.");
        }

        var nestedReferences = new List<SymbolRefNestedReferenceSyntax>(symbolRef.NestedReferences.Count);
        for (var i = 0; i < symbolRef.NestedReferences.Count; i++)
        {
            nestedReferences.Add(new SymbolRefNestedReferenceSyntax(
                TokenFactory.Colon(),
                TokenFactory.Colon(),
                CreateSymbolNameToken(symbolRef.NestedReferences[i])));
        }

        return new SymbolRefAttributeValueSyntax(
            CreateSymbolNameToken(symbolRef.RootReference),
            nestedReferences);
    }

    private static bool IsNestedReferenceSeparator(AttributeParsingContext context)
    {
        return context.TryPeekToken(0, out var currentKind, out _) &&
            currentKind == TokenKind.Colon &&
            context.TryPeekToken(1, out var nextKind, out _) &&
            nextKind == TokenKind.Colon;
    }

    private static Token CreateSymbolNameToken(string name)
    {
        name = TrimLeadingAt(name);
        return IsBareSymbolName(name)
            ? TokenFactory.SymbolName("@" + name)
            : TokenFactory.SymbolName("@" + StringLiteralAttributeAssemblyFormat.Quote(name));
    }

    private static string DecodeSymbolName(Token token)
    {
        var text = TrimLeadingAt(token.Text);
        return text.Length >= 2 && text[0] == '"' && text[text.Length - 1] == '"'
            ? StringLiteralAttributeAssemblyFormat.Unescape(text)
            : text;
    }

    private static string TrimLeadingAt(string name)
    {
        return name.Length > 0 && name[0] == '@' ? name.Substring(1) : name;
    }

    private static bool IsBareSymbolName(string name)
    {
        if (name.Length == 0 || !IsIdentifierStart(name[0]))
        {
            return false;
        }

        for (var i = 1; i < name.Length; i++)
        {
            if (!IsIdentifierPart(name[i]))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsIdentifierStart(char ch)
    {
        return char.IsLetter(ch) || ch == '_' || ch == '$';
    }

    private static bool IsIdentifierPart(char ch)
    {
        return char.IsLetterOrDigit(ch) || ch == '_' || ch == '$' || ch == '.' || ch == '-';
    }
}
