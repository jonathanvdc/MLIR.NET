namespace MLIR.Text;

using MLIR.Dialects;
using MLIR.Dialects.Attributes.Collections;
using MLIR.Dialects.Attributes.Primitives;
using MLIR.Dialects.Builtin;
using MLIR.Syntax;
using MLIR.Syntax.Attributes;

public sealed partial class Parser
{
    /// <summary>
    /// Ordered fallback parsers for attribute values with no expected assembly format.
    /// Self-identifying attributes are handled separately through the dialect registry.
    /// </summary>
    private static readonly IAttributeAssemblyFormat[] DefaultAttributeAssemblyFormats = [
        new ArrayAttributeAssemblyFormat(),
        new DictionaryAttributeAssemblyFormat(),
        new DenseIntegerArrayAttributeAssemblyFormat(),
        new ElementsAttributeAssemblyFormat(),
        new BuiltinSymbolRefAttributeAssemblyFormat(),
        new BuiltinOpaqueAttributeAssemblyFormat(),
        new StringLiteralAttributeAssemblyFormat(),
        new FloatingPointLiteralAttributeAssemblyFormat(),
        new IntegerLiteralAttributeAssemblyFormat(),
        new BooleanLiteralAttributeAssemblyFormat(),
        new UnitLiteralAttributeAssemblyFormat(parseSelfIdentifyingSyntax: false)
    ];

    private enum AttributeValueParsingMode
    {
        Normal,
        StopAtOperationBoundary,
    }

    private ParseResult<AttributeValueSyntax> TryParseAttributeValue(
        AttributeValueParsingMode mode,
        AttributeConstraintDefinition? expectedDefinition,
        params TokenKind[] stopBefore)
    {
        var allowTypedSuffix = ShouldAllowTypedAttributeSuffix(stopBefore);
        if (expectedDefinition != null)
        {
            var expectedResult = TryParseExpectedAttributeValue(expectedDefinition);
            if (!expectedResult.IsNoMatch)
            {
                return WrapTypedAttributeValueSyntax(expectedResult, false);
            }
        }

        var result = TryParseDefaultAttributeValue(mode, allowTypedSuffix, stopBefore);
        return WrapTypedAttributeValueSyntax(result, allowTypedSuffix);
    }

    private static bool ShouldAllowTypedAttributeSuffix(TokenKind[] stopBefore)
    {
        return !ContainsTokenKind(stopBefore, TokenKind.Colon);
    }

    private ParseResult<AttributeValueSyntax> TryParseExpectedAttributeValue(AttributeConstraintDefinition expectedDefinition)
    {
        if (expectedDefinition.AssemblyFormat == null)
        {
            return ParseResult<AttributeValueSyntax>.NoMatch();
        }

        var result = TryParseCustomAttribute(expectedDefinition);
        if (result.IsNoMatch)
        {
            return ParseResult<AttributeValueSyntax>.Failure(
                CreateDiagnostic($"Expected attribute value for '{expectedDefinition.Name}'."));
        }

        return result;
    }

    private ParseResult<AttributeValueSyntax> TryParseDefaultAttributeValue(
        AttributeValueParsingMode mode,
        bool allowTypedSuffix,
        TokenKind[] stopBefore)
    {
        var selfIdentifyingResult = TryParseSelfIdentifyingAttribute();
        if (!selfIdentifyingResult.IsNoMatch)
        {
            return selfIdentifyingResult;
        }

        foreach (var assemblyFormat in DefaultAttributeAssemblyFormats)
        {
            var result = TryParseAttributeAssemblyFormat(assemblyFormat);
            if (!result.IsNoMatch)
            {
                return result;
            }
        }

        return CreateUnrecognizedAttributeValueFailure(mode, allowTypedSuffix, stopBefore);
    }

    private ParseResult<AttributeValueSyntax> CreateUnrecognizedAttributeValueFailure(
        AttributeValueParsingMode mode,
        bool allowTypedSuffix,
        TokenKind[] stopBefore)
    {
        var rawStopBefore = allowTypedSuffix ? [.. stopBefore, TokenKind.Colon] : stopBefore;
        var rawResult = mode == AttributeValueParsingMode.StopAtOperationBoundary
            ? TryParseRawUntilDelimiterOrBoundaryResult(rawStopBefore)
            : TryParseRawUntilDelimiterResult(rawStopBefore);
        if (!rawResult.IsSuccess)
        {
            return ParseResult<AttributeValueSyntax>.Failure(rawResult.Diagnostic!);
        }

        return ParseResult<AttributeValueSyntax>.Failure(
            CreateDiagnostic("Expected an attribute value; unrecognized raw syntax '" + rawResult.Value.Text + "'."));
    }

    private ParseResult<AttributeValueSyntax> WrapTypedAttributeValueSyntax(
        ParseResult<AttributeValueSyntax> result,
        bool allowTypedSuffix)
    {
        if (!allowTypedSuffix || !result.IsSuccess || result.Value is TypedAttributeValueSyntax)
        {
            return result;
        }

        if (!TryMatch(TokenKind.Colon, out var colonToken))
        {
            return result;
        }

        var typeResult = TryParseTypeSyntax();
        if (!typeResult.IsSuccess)
        {
            return ParseResult<AttributeValueSyntax>.Failure(typeResult.Diagnostic!);
        }

        return ParseResult<AttributeValueSyntax>.Success(new TypedAttributeValueSyntax(result.Value, colonToken, typeResult.Value));
    }

    private static bool ContainsTokenKind(TokenKind[] kinds, TokenKind kind)
    {
        for (var i = 0; i < kinds.Length; i++)
        {
            if (kinds[i] == kind)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Invokes a specific <see cref="IAttributeAssemblyFormat"/> handler against the current parser position.
    /// Saves a checkpoint before calling and restores it when the handler returns <c>NoMatch</c>.
    /// Propagates <c>Success</c> and <c>Error</c> unchanged.
    /// </summary>
    private ParseResult<AttributeValueSyntax> TryParseAttributeAssemblyFormat(IAttributeAssemblyFormat assemblyFormat)
    {
        var checkpoint = Mark();
        var result = assemblyFormat.TryParse(new ParsingContext(this));
        if (result.IsSuccess)
        {
            return result;
        }

        Reset(checkpoint);
        return result.IsError ? result : ParseResult<AttributeValueSyntax>.NoMatch();
    }

    /// <summary>
    /// Bridges unguided attribute value parsing (no stop-at-boundary, no expected definition) for use by
    /// <see cref="ParsingContext"/>.
    /// </summary>
    internal ParseResult<AttributeValueSyntax> TryParseAttributeValueInternal(params TokenKind[] delimiters)
    {
        return TryParseAttributeValue(AttributeValueParsingMode.Normal, (AttributeConstraintDefinition?)null, delimiters);
    }

    /// <summary>
    /// Bridges definition-guided attribute value parsing for use by <see cref="ParsingContext"/>.
    /// </summary>
    internal ParseResult<AttributeValueSyntax> TryParseAttributeValueInternal(AttributeConstraintDefinition expectedDefinition, params TokenKind[] delimiters)
    {
        return TryParseAttributeValue(AttributeValueParsingMode.Normal, expectedDefinition, delimiters);
    }

    /// <summary>
    /// Parses a named attribute entry of the form <c>name = value</c> or <c>name : value</c>.
    /// The name may be a bare identifier or a quoted string literal.
    /// </summary>
    private ParseResult<NamedAttributeSyntax> TryParseAttribute()
    {
        Token nameToken;
        if (Is(TokenKind.Identifier) || Is(TokenKind.StringLiteral))
        {
            nameToken = ConsumeToken();
        }
        else
        {
            return ParseResult<NamedAttributeSyntax>.Failure(CreateDiagnostic("Expected an attribute name."));
        }

        Token separatorToken;
        if (TryMatch(TokenKind.Equal, out var equalsToken))
        {
            separatorToken = equalsToken;
        }
        else if (TryMatch(TokenKind.Colon, out var colonToken))
        {
            separatorToken = colonToken;
        }
        else
        {
            return ParseResult<NamedAttributeSyntax>.Failure(CreateDiagnostic("Expected '=' or ':' after attribute name."));
        }

        return TryParseAttributeValue(AttributeValueParsingMode.Normal, (AttributeConstraintDefinition?)null, TokenKind.Comma, TokenKind.RBrace)
            .Map(valueSyntax => new NamedAttributeSyntax(nameToken, separatorToken, valueSyntax));
    }

    /// <summary>
    /// Parses a standalone attribute value that must consume the entire input string.
    /// Returns a failure when any tokens remain after the attribute value.
    /// Used by the public <see cref="ParseAttributeValue"/> and <c>TryParseAttributeValue</c> entry points.
    /// </summary>
    private ParseResult<AttributeValueSyntax> TryParseStandaloneAttributeValue(AttributeConstraintDefinition? expectedDefinition)
    {
        var parsed = TryParseAttributeValue(AttributeValueParsingMode.Normal, expectedDefinition);
        if (!parsed.IsSuccess)
        {
            return parsed;
        }

        return !Is(TokenKind.EndOfFile)
            ? ParseResult<AttributeValueSyntax>.Failure(CreateDiagnostic("Expected the attribute value to consume the entire input."))
            : parsed;
    }

    /// <summary>
    /// Attempts to parse an attribute value using the assembly format registered on
    /// <paramref name="definition"/>. Returns <see cref="ParseOutcome.NoMatch"/> when the definition
    /// has no assembly format, and resets the position when the format handler returns <c>NoMatch</c>.
    /// </summary>
    private ParseResult<AttributeValueSyntax> TryParseCustomAttribute(AttributeConstraintDefinition? definition)
    {
        if (definition?.AssemblyFormat == null)
        {
            return ParseResult<AttributeValueSyntax>.NoMatch();
        }

        var checkpoint = Mark();
        var result = definition.AssemblyFormat.TryParse(new ParsingContext(this));
        if (result.IsSuccess)
        {
            return result;
        }

        Reset(checkpoint);
        return result.IsError ? result : ParseResult<AttributeValueSyntax>.NoMatch();
    }

    /// <summary>
    /// Attempts to parse a self-identifying attribute value by peeking at a leading <c>#name</c> prefix
    /// and looking the name up in the dialect registry. Returns <see cref="ParseOutcome.NoMatch"/> when no
    /// registry is available or the name is not registered.
    /// </summary>
    private ParseResult<AttributeValueSyntax> TryParseSelfIdentifyingAttribute()
    {
        var canonicalName = TryPeekAttributeDefinitionName();
        if (canonicalName == null)
        {
            return ParseResult<AttributeValueSyntax>.NoMatch();
        }

        if (dialectRegistry == null)
        {
            return ParseResult<AttributeValueSyntax>.NoMatch();
        }

        if (!dialectRegistry.TryGetAttribute(canonicalName, out var definition))
        {
            return ParseResult<AttributeValueSyntax>.NoMatch();
        }

        if (definition.AssemblyFormat == null)
        {
            // No custom format: fall through to raw syntax (the attribute will be bound later).
            return ParseResult<AttributeValueSyntax>.NoMatch();
        }

        return TryParseCustomAttribute(definition);
    }

    /// <summary>
    /// Parses an optional attribute dictionary of the form <c>{ name = value, ... }</c>.
    /// Returns an empty list when no <c>{</c> is present.
    /// </summary>
    private ParseResult<DelimitedSyntaxList<NamedAttributeSyntax>> TryParseAttrDict()
    {
        if (!Is(TokenKind.LBrace))
        {
            return ParseResult<DelimitedSyntaxList<NamedAttributeSyntax>>.Success(EmptyDelimitedSyntaxList<NamedAttributeSyntax>());
        }

        return TryParseRequiredCommaSeparatedDelimitedList(
            TokenKind.LBrace,
            TokenKind.RBrace,
            TryParseAttribute,
            "Expected '{' to start the attribute dictionary.",
            "Expected '}' to close the attribute dictionary.");
    }

}
