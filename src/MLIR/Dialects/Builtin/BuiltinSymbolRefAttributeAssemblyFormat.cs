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
        if (!context.TryMatch(TokenKind.At, out var rootAtToken))
        {
            return ParseResult<AttributeValueSyntax>.NoMatch();
        }

        var rootNameResult = TryParseSymbolNameToken(context);
        if (!rootNameResult.IsSuccess)
        {
            return ParseResult<AttributeValueSyntax>.Failure(rootNameResult.Diagnostic!);
        }

        var components = new List<SymbolRefAttributeComponentSyntax>
        {
            new(rootAtToken, rootNameResult.Value),
        };
        var separators = new List<SymbolRefAttributeSeparatorSyntax>();

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

            var nestedAtResult = context.Expect(TokenKind.At, "Expected '@' after '::' in nested symbol reference.");
            if (!nestedAtResult.IsSuccess)
            {
                return ParseResult<AttributeValueSyntax>.Failure(nestedAtResult.Diagnostic!);
            }

            var nestedNameResult = TryParseSymbolNameToken(context);
            if (!nestedNameResult.IsSuccess)
            {
                return ParseResult<AttributeValueSyntax>.Failure(nestedNameResult.Diagnostic!);
            }

            separators.Add(new SymbolRefAttributeSeparatorSyntax(firstColonResult.Value, secondColonResult.Value));
            components.Add(new SymbolRefAttributeComponentSyntax(nestedAtResult.Value, nestedNameResult.Value));
        }

        return ParseResult<AttributeValueSyntax>.Success(
            new SymbolRefAttributeValueSyntax(components, separators));
    }

    /// <inheritdoc/>
    public AttributeValue Bind(AttributeValueSyntax syntax, AttributeConstraintDefinition definition, Binder binder)
    {
        if (syntax is TypedAttributeValueSyntax typedSyntax)
        {
            syntax = typedSyntax.AttributeSyntax;
        }

        if (syntax is not SymbolRefAttributeValueSyntax symbolSyntax)
        {
            throw new InvalidOperationException("Symbol-reference attributes require symbol-reference syntax.");
        }

        var nestedReferences = new string[Math.Max(0, symbolSyntax.Count - 1)];
        for (var i = 1; i < symbolSyntax.Count; i++)
        {
            nestedReferences[i - 1] = DecodeSymbolName(symbolSyntax.Components[i].NameToken);
        }

        return new SymbolRefAttr(DecodeSymbolName(symbolSyntax.Components[0].NameToken), nestedReferences, symbolSyntax);
    }

    /// <inheritdoc/>
    public AttributeValueSyntax BuildCustomAssemblySyntax(AttributeValue attribute, ConcreteSyntaxBuilderContext context)
    {
        if (attribute is not SymbolRefAttr symbolRef)
        {
            return attribute.Syntax ?? throw new InvalidOperationException("Symbol-reference attributes require SymbolRefAttr storage or reusable syntax.");
        }

        var componentCount = 1 + symbolRef.NestedReferences.Count;
        var components = new List<SymbolRefAttributeComponentSyntax>(componentCount);
        var separators = new List<SymbolRefAttributeSeparatorSyntax>(Math.Max(0, componentCount - 1));

        AddComponent(TrimLeadingAt(symbolRef.RootReference), components);
        for (var i = 0; i < symbolRef.NestedReferences.Count; i++)
        {
            separators.Add(new SymbolRefAttributeSeparatorSyntax(TokenFactory.Colon(), TokenFactory.Colon()));
            AddComponent(TrimLeadingAt(symbolRef.NestedReferences[i]), components);
        }

        return new SymbolRefAttributeValueSyntax(components, separators);
    }

    private static ParseResult<Token> TryParseSymbolNameToken(AttributeParsingContext context)
    {
        if (context.TryMatch(TokenKind.Identifier, out var identifierToken))
        {
            return ParseResult<Token>.Success(identifierToken);
        }

        if (context.TryMatch(TokenKind.StringLiteral, out var stringToken))
        {
            return ParseResult<Token>.Success(stringToken);
        }

        return ParseResult<Token>.Failure(context.CreateDiagnostic("Expected a symbol name after '@'."));
    }

    private static bool IsNestedReferenceSeparator(AttributeParsingContext context)
    {
        return context.TryPeekToken(0, out var currentKind, out _) &&
            currentKind == TokenKind.Colon &&
            context.TryPeekToken(1, out var nextKind, out _) &&
            nextKind == TokenKind.Colon;
    }

    private static void AddComponent(string name, List<SymbolRefAttributeComponentSyntax> components)
    {
        components.Add(new SymbolRefAttributeComponentSyntax(TokenFactory.At(), CreateSymbolNameToken(name)));
    }

    private static Token CreateSymbolNameToken(string name)
    {
        return IsBareSymbolName(name)
            ? TokenFactory.Identifier(name)
            : TokenFactory.StringLiteral(StringLiteralAttributeAssemblyFormat.Quote(name));
    }

    private static string DecodeSymbolName(Token token)
    {
        return token.TokenKind == TokenKind.StringLiteral
            ? StringLiteralAttributeAssemblyFormat.Unescape(token.Text)
            : token.Text;
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
