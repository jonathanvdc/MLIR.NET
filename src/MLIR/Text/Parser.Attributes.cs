namespace MLIR.Text;

using MLIR.Dialects;
using MLIR.Dialects.Attributes.Collections;
using MLIR.Dialects.Attributes.Primitives;
using MLIR.Syntax;
using MLIR.Syntax.Attributes;
using MLIR.Syntax.Attributes.Primitives;
using MLIR.Syntax.Attributes.Collections;

public sealed partial class Parser
{
    /// <summary>Cached singleton format handler for boolean literal attributes. Stateless and safe to share across parse operations.</summary>
    private static readonly BooleanLiteralAttributeAssemblyFormat BooleanLiteralAttributeAssemblyFormat = new();
    /// <summary>Cached singleton format handler for integer literal attributes. Stateless and safe to share across parse operations.</summary>
    private static readonly IntegerLiteralAttributeAssemblyFormat IntegerLiteralAttributeAssemblyFormat = new();
    /// <summary>Cached singleton format handler for floating-point literal attributes. Stateless and safe to share across parse operations.</summary>
    private static readonly FloatingPointLiteralAttributeAssemblyFormat FloatingPointLiteralAttributeAssemblyFormat = new();
    /// <summary>Cached singleton format handler for string literal attributes. Stateless and safe to share across parse operations.</summary>
    private static readonly StringLiteralAttributeAssemblyFormat StringLiteralAttributeAssemblyFormat = new();
    /// <summary>Cached singleton format handler for dense integer array attributes. Stateless and safe to share across parse operations.</summary>
    private static readonly DenseIntegerArrayAttributeAssemblyFormat DenseArrayAttributeAssemblyFormat = new();
    /// <summary>Cached singleton format handler for elements attributes. Stateless and safe to share across parse operations.</summary>
    private static readonly ElementsAttributeAssemblyFormat ElementsAttributeAssemblyFormat = new();
    /// <summary>Cached singleton format handler for dictionary attributes. Stateless and safe to share across parse operations.</summary>
    private static readonly DictionaryAttributeAssemblyFormat DictionaryAttributeAssemblyFormat = new();
    /// <summary>Cached singleton format handler for array attributes. Stateless and safe to share across parse operations.</summary>
    private static readonly TypedArrayAttributeAssemblyFormat ArrayAttributeAssemblyFormat = new();
    /// <summary>Cached singleton format handler for bare unit literals in default parsing. Stateless and safe to share across parse operations.</summary>
    private static readonly UnitLiteralAttributeAssemblyFormat BareUnitLiteralAttributeAssemblyFormat = new(parseSelfIdentifyingSyntax: false);

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
                return WrapTypedAttributeValueSyntax(expectedResult, false, mode, stopBefore);
            }
        }

        var result = TryParseDefaultAttributeValue(mode, expectedDefinition, allowTypedSuffix, stopBefore);
        return WrapTypedAttributeValueSyntax(result, allowTypedSuffix, mode, stopBefore);
    }

    private ParseResult<AttributeValueSyntax> TryParseAttributeValue(
        AttributeValueParsingMode mode,
        string? expectedDefinitionName,
        params TokenKind[] stopBefore)
    {
        AttributeConstraintDefinition? expectedDefinition = null;
        if (!string.IsNullOrEmpty(expectedDefinitionName) && dialectRegistry != null)
        {
            dialectRegistry.TryResolveAttributeConstraint(expectedDefinitionName!, out expectedDefinition);
        }

        return TryParseAttributeValue(mode, expectedDefinition, stopBefore);
    }

    private delegate ParseResult<AttributeValueSyntax> AttributeValueParser(
        AttributeValueParsingMode mode,
        AttributeConstraintDefinition? expectedDefinition,
        bool allowTypedSuffix,
        TokenKind[] stopBefore);

    private static bool ShouldAllowTypedAttributeSuffix(TokenKind[] stopBefore)
    {
        return !ContainsTokenKind(stopBefore, TokenKind.Colon);
    }


    private AttributeValueParser[] DefaultAttributeValueParsers => [
        TryParseSelfIdentifyingAttributeValue,
        TryParseArrayAttributeValue,
        TryParseDictionaryAttributeValue,
        TryParseDenseArrayAttributeValue,
        TryParseElementsAttributeValue,
        TryParseStringAttributeValue,
        TryParseFloatingPointAttributeValue,
        TryParseIntegerAttributeValue,
        TryParseBooleanAttributeValue,
        TryParseUnitAttributeValue
    ];

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
        AttributeConstraintDefinition? expectedDefinition,
        bool allowTypedSuffix,
        TokenKind[] stopBefore)
    {
        var parsers = DefaultAttributeValueParsers;
        for (var i = 0; i < parsers.Length; i++)
        {
            var result = parsers[i](mode, expectedDefinition, allowTypedSuffix, stopBefore);
            if (!result.IsNoMatch)
            {
                return result;
            }
        }

        return TryParseRawAttributeValue(mode, allowTypedSuffix, stopBefore);
    }

    private ParseResult<AttributeValueSyntax> TryParseSelfIdentifyingAttributeValue(
        AttributeValueParsingMode mode,
        AttributeConstraintDefinition? expectedDefinition,
        bool allowTypedSuffix,
        TokenKind[] stopBefore)
    {
        _ = mode;
        _ = expectedDefinition;
        _ = allowTypedSuffix;
        _ = stopBefore;
        return TryParseSelfIdentifyingAttribute();
    }

    private ParseResult<AttributeValueSyntax> TryParseArrayAttributeValue(
        AttributeValueParsingMode mode,
        AttributeConstraintDefinition? expectedDefinition,
        bool allowTypedSuffix,
        TokenKind[] stopBefore)
    {
        _ = mode;
        _ = expectedDefinition;
        _ = allowTypedSuffix;
        _ = stopBefore;
        return TryParseAttributeAssemblyFormat(
            BuiltinAttributeConstraintDefinition("ArrayAttr"),
            ArrayAttributeAssemblyFormat);
    }

    private ParseResult<AttributeValueSyntax> TryParseDictionaryAttributeValue(
        AttributeValueParsingMode mode,
        AttributeConstraintDefinition? expectedDefinition,
        bool allowTypedSuffix,
        TokenKind[] stopBefore)
    {
        _ = mode;
        _ = expectedDefinition;
        _ = allowTypedSuffix;
        _ = stopBefore;
        return TryParseAttributeAssemblyFormat(
            BuiltinAttributeConstraintDefinition("DictionaryAttr"),
            DictionaryAttributeAssemblyFormat);
    }

    private ParseResult<AttributeValueSyntax> TryParseStringAttributeValue(
        AttributeValueParsingMode mode,
        AttributeConstraintDefinition? expectedDefinition,
        bool allowTypedSuffix,
        TokenKind[] stopBefore)
    {
        _ = mode;
        _ = allowTypedSuffix;
        _ = stopBefore;
        return TryParseAttributeAssemblyFormat(expectedDefinition, StringLiteralAttributeAssemblyFormat);
    }

    private ParseResult<AttributeValueSyntax> TryParseDenseArrayAttributeValue(
        AttributeValueParsingMode mode,
        AttributeConstraintDefinition? expectedDefinition,
        bool allowTypedSuffix,
        TokenKind[] stopBefore)
    {
        _ = mode;
        _ = expectedDefinition;
        _ = allowTypedSuffix;
        _ = stopBefore;
        return TryParseAttributeAssemblyFormat(
            BuiltinAttributeConstraintDefinition("DenseArrayAttr"),
            DenseArrayAttributeAssemblyFormat);
    }

    private ParseResult<AttributeValueSyntax> TryParseElementsAttributeValue(
        AttributeValueParsingMode mode,
        AttributeConstraintDefinition? expectedDefinition,
        bool allowTypedSuffix,
        TokenKind[] stopBefore)
    {
        _ = mode;
        _ = expectedDefinition;
        _ = allowTypedSuffix;
        _ = stopBefore;
        return TryParseAttributeAssemblyFormat(
            BuiltinAttributeConstraintDefinition("ElementsAttr"),
            ElementsAttributeAssemblyFormat);
    }

    private ParseResult<AttributeValueSyntax> TryParseBooleanAttributeValue(
        AttributeValueParsingMode mode,
        AttributeConstraintDefinition? expectedDefinition,
        bool allowTypedSuffix,
        TokenKind[] stopBefore)
    {
        _ = mode;
        _ = allowTypedSuffix;
        _ = stopBefore;
        return TryParseAttributeAssemblyFormat(expectedDefinition, BooleanLiteralAttributeAssemblyFormat);
    }

    private ParseResult<AttributeValueSyntax> TryParseUnitAttributeValue(
        AttributeValueParsingMode mode,
        AttributeConstraintDefinition? expectedDefinition,
        bool allowTypedSuffix,
        TokenKind[] stopBefore)
    {
        _ = mode;
        _ = allowTypedSuffix;
        _ = stopBefore;
        return TryParseAttributeAssemblyFormat(expectedDefinition, BareUnitLiteralAttributeAssemblyFormat);
    }

    private ParseResult<AttributeValueSyntax> TryParseIntegerAttributeValue(
        AttributeValueParsingMode mode,
        AttributeConstraintDefinition? expectedDefinition,
        bool allowTypedSuffix,
        TokenKind[] stopBefore)
    {
        _ = mode;
        _ = allowTypedSuffix;
        _ = stopBefore;
        return TryParseAttributeAssemblyFormat(expectedDefinition, IntegerLiteralAttributeAssemblyFormat);
    }

    private ParseResult<AttributeValueSyntax> TryParseFloatingPointAttributeValue(
        AttributeValueParsingMode mode,
        AttributeConstraintDefinition? expectedDefinition,
        bool allowTypedSuffix,
        TokenKind[] stopBefore)
    {
        _ = mode;
        _ = allowTypedSuffix;
        _ = stopBefore;
        return TryParseAttributeAssemblyFormat(expectedDefinition, FloatingPointLiteralAttributeAssemblyFormat);
    }

    private ParseResult<AttributeValueSyntax> TryParseRawAttributeValue(
        AttributeValueParsingMode mode,
        bool allowTypedSuffix,
        TokenKind[] stopBefore)
    {
        var rawStopBefore = allowTypedSuffix ? [.. stopBefore, TokenKind.Colon] : stopBefore;
        var rawResult = mode == AttributeValueParsingMode.StopAtOperationBoundary
            ? TryParseRawUntilDelimiterOrBoundaryResult(rawStopBefore)
            : TryParseRawUntilDelimiterResult(rawStopBefore);
        return rawResult.IsSuccess
            ? ParseResult<AttributeValueSyntax>.Success(new RawAttributeValueSyntax(rawResult.Value))
            : ParseResult<AttributeValueSyntax>.Failure(rawResult.Diagnostic!);
    }

    private ParseResult<AttributeValueSyntax> WrapTypedAttributeValueSyntax(
        ParseResult<AttributeValueSyntax> result,
        bool allowTypedSuffix,
        AttributeValueParsingMode mode,
        TokenKind[] stopBefore)
    {
        if (!allowTypedSuffix || !result.IsSuccess || result.Value is TypedAttributeValueSyntax)
        {
            return result;
        }

        if (!TryMatch(TokenKind.Colon, out var colonToken))
        {
            return result;
        }

        var typeResult = TryParseTypeSyntaxCoreResult(stopBefore, mode == AttributeValueParsingMode.StopAtOperationBoundary);
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
    private ParseResult<AttributeValueSyntax> TryParseAttributeAssemblyFormat(
        AttributeConstraintDefinition? definition,
        IAttributeAssemblyFormat assemblyFormat)
    {
        var checkpoint = Mark();
        var result = assemblyFormat.TryParse(new AttributeParsingContext(this, dialectRegistry, definition));
        if (result.IsSuccess)
        {
            return result;
        }

        Reset(checkpoint);
        return result.IsError ? result : ParseResult<AttributeValueSyntax>.NoMatch();
    }

    /// <summary>
    /// Bridges unguided attribute value parsing (no stop-at-boundary, no expected definition) for use by
    /// <see cref="DialectParsingContext"/>.
    /// </summary>
    internal ParseResult<AttributeValueSyntax> TryParseAttributeValueInternal(params TokenKind[] delimiters)
    {
        return TryParseAttributeValue(AttributeValueParsingMode.Normal, (AttributeConstraintDefinition?)null, delimiters);
    }

    /// <summary>
    /// Bridges name-guided attribute value parsing for use by <see cref="DialectParsingContext"/>.
    /// </summary>
    internal ParseResult<AttributeValueSyntax> TryParseAttributeValueInternal(string? expectedDefinitionName, params TokenKind[] delimiters)
    {
        return TryParseAttributeValue(AttributeValueParsingMode.Normal, expectedDefinitionName, delimiters);
    }

    /// <summary>
    /// Bridges definition-guided attribute value parsing for use by <see cref="DialectParsingContext"/>.
    /// </summary>
    internal ParseResult<AttributeValueSyntax> TryParseAttributeValueInternal(AttributeConstraintDefinition expectedDefinition, params TokenKind[] delimiters)
    {
        return TryParseAttributeValue(AttributeValueParsingMode.Normal, expectedDefinition, delimiters);
    }

    /// <summary>
    /// Bridges unguided attribute value parsing with operation-boundary stopping for use by
    /// <see cref="OperationParsingContext"/>. The boundary stop is required inside custom operation
    /// assembly formats so that the parser does not consume tokens that belong to the next operation.
    /// </summary>
    internal ParseResult<AttributeValueSyntax> TryParseAttributeValueOrBoundaryInternal(params TokenKind[] delimiters)
    {
        return TryParseAttributeValue(AttributeValueParsingMode.StopAtOperationBoundary, (AttributeConstraintDefinition?)null, delimiters);
    }

    /// <summary>
    /// Bridges name-guided attribute value parsing with operation-boundary stopping for use by
    /// <see cref="OperationParsingContext"/>.
    /// </summary>
    internal ParseResult<AttributeValueSyntax> TryParseAttributeValueOrBoundaryInternal(string? expectedDefinitionName, params TokenKind[] delimiters)
    {
        return TryParseAttributeValue(AttributeValueParsingMode.StopAtOperationBoundary, expectedDefinitionName, delimiters);
    }

    /// <summary>
    /// Bridges definition-guided attribute value parsing with operation-boundary stopping for use by
    /// <see cref="OperationParsingContext"/>.
    /// </summary>
    internal ParseResult<AttributeValueSyntax> TryParseAttributeValueOrBoundaryInternal(AttributeConstraintDefinition expectedDefinition, params TokenKind[] delimiters)
    {
        return TryParseAttributeValue(AttributeValueParsingMode.StopAtOperationBoundary, expectedDefinition, delimiters);
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
        var result = definition.AssemblyFormat.TryParse(new AttributeParsingContext(this, dialectRegistry, definition));
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
