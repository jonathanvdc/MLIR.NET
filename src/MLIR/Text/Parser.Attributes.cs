namespace MLIR.Text;

using System.Globalization;
using MLIR.Dialects;
using MLIR.Dialects.Attributes.Collections;
using MLIR.Dialects.Attributes.Primitives;
using MLIR.Numerics;
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
        var allowTypedSuffix = ShouldAllowTypedAttributeSuffix(mode, stopBefore);

        var parsers = GetAttributeValueParserSequence(expectedDefinition);
        for (var i = 0; i < parsers.Length; i++)
        {
            var mark = Mark();
            var result = parsers[i](mode, expectedDefinition, allowTypedSuffix, stopBefore);
            if (result.IsNoMatch)
            {
                Reset(mark);
            }
            else
            {
                return WrapTypedAttributeValueSyntax(result, allowTypedSuffix, mode, stopBefore);
            }
        }

        var rawResult = TryParseRawAttributeValue(mode, allowTypedSuffix, stopBefore);
        return WrapTypedAttributeValueSyntax(rawResult, allowTypedSuffix, mode, stopBefore);
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

    private static bool ShouldAllowTypedAttributeSuffix(AttributeValueParsingMode mode, TokenKind[] stopBefore)
    {
        return mode == AttributeValueParsingMode.Normal
            && !ContainsTokenKind(stopBefore, TokenKind.Colon);
    }

    private AttributeValueParser[] GetAttributeValueParserSequence(AttributeConstraintDefinition? expectedDefinition)
    {
        return expectedDefinition != null
            ? [TryParseExpectedAttributeValue, .. DefaultAttributeValueParsers]
            : DefaultAttributeValueParsers;
    }

    private AttributeValueParser[] DefaultAttributeValueParsers => [
        TryParseSelfIdentifyingAttributeValue,
        TryParseBuiltinStructuredAttributeValue,
        TryParseStringAttributeValue,
        TryParseNumericAttributeValue,
        TryParseBooleanAttributeValue
    ];

    private ParseResult<AttributeValueSyntax> TryParseExpectedAttributeValue(
        AttributeValueParsingMode mode,
        AttributeConstraintDefinition? expectedDefinition,
        bool allowTypedSuffix,
        TokenKind[] stopBefore)
    {
        _ = mode;
        _ = allowTypedSuffix;
        _ = stopBefore;
        return TryParseCustomAttribute(expectedDefinition);
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

    private ParseResult<AttributeValueSyntax> TryParseBuiltinStructuredAttributeValue(
        AttributeValueParsingMode mode,
        AttributeConstraintDefinition? expectedDefinition,
        bool allowTypedSuffix,
        TokenKind[] stopBefore)
    {
        _ = mode;
        _ = expectedDefinition;
        _ = allowTypedSuffix;
        _ = stopBefore;
        return TryParseBuiltinStructuredAttribute();
    }

    private ParseResult<AttributeValueSyntax> TryParseStringAttributeValue(
        AttributeValueParsingMode mode,
        AttributeConstraintDefinition? expectedDefinition,
        bool allowTypedSuffix,
        TokenKind[] stopBefore)
    {
        _ = mode;
        _ = expectedDefinition;
        _ = allowTypedSuffix;
        _ = stopBefore;
        return StringLiteralAttributeAssemblyFormat.TryParse(new AttributeParsingContext(this, dialectRegistry, null));
    }

    private ParseResult<AttributeValueSyntax> TryParseBooleanAttributeValue(
        AttributeValueParsingMode mode,
        AttributeConstraintDefinition? expectedDefinition,
        bool allowTypedSuffix,
        TokenKind[] stopBefore)
    {
        _ = mode;
        _ = expectedDefinition;
        _ = allowTypedSuffix;
        _ = stopBefore;
        return BooleanLiteralAttributeAssemblyFormat.TryParse(new AttributeParsingContext(this, dialectRegistry, null));
    }

    private ParseResult<AttributeValueSyntax> TryParseNumericAttributeValue(
        AttributeValueParsingMode mode,
        AttributeConstraintDefinition? expectedDefinition,
        bool allowTypedSuffix,
        TokenKind[] stopBefore)
    {
        _ = expectedDefinition;
        return TryParseNumericAttribute(mode, allowTypedSuffix, stopBefore);
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

    /// <summary>
    /// Tries to parse a built-in structured attribute value: an array literal (<c>[...]</c>),
    /// an attribute dictionary (<c>{...}</c>), a dense array, or an elements attribute.
    /// Returns <see cref="ParseOutcome.NoMatch"/> when none of those forms is present.
    /// </summary>
    private ParseResult<AttributeValueSyntax> TryParseBuiltinStructuredAttribute()
    {
        if (Is(TokenKind.LBracket))
        {
            return TryParseArrayAttributeValue().Map<AttributeValueSyntax>(static syntax => syntax);
        }

        if (Is(TokenKind.LBrace))
        {
            return TryParseAttrDict().Map<AttributeValueSyntax>(static syntax => new DictionaryAttributeValueSyntax(syntax));
        }

        var denseArrayResult = TryParseAttributeAssemblyFormat(BuiltinAttributeConstraintDefinition("DenseArrayAttr"), DenseArrayAttributeAssemblyFormat);
        if (!denseArrayResult.IsNoMatch)
        {
            return denseArrayResult;
        }

        return TryParseAttributeAssemblyFormat(BuiltinAttributeConstraintDefinition("ElementsAttr"), ElementsAttributeAssemblyFormat);
    }

    private ParseResult<AttributeValueSyntax> TryParseNumericAttribute(
        AttributeValueParsingMode mode,
        bool allowTypedSuffix,
        TokenKind[] stopBefore)
    {
        var checkpoint = Mark();

        var floatingPointResult = FloatingPointAssemblyFormatHelper.TryParseDecimalLiteral(new AttributeParsingContext(this, dialectRegistry, null));
        if (floatingPointResult.IsSuccess)
        {
            if (IsValidAttributeValueTermination(mode, allowTypedSuffix, stopBefore))
            {
                return floatingPointResult;
            }

            Reset(checkpoint);
            return ParseResult<AttributeValueSyntax>.NoMatch();
        }

        if (!floatingPointResult.IsNoMatch)
        {
            return floatingPointResult;
        }

        Reset(checkpoint);

        if (!IntegerLiteralAttributeAssemblyFormat.TryParseSignedIntegerLiteral(new AttributeParsingContext(this, dialectRegistry, null), out var signToken, out var integerToken, out var value))
        {
            Reset(checkpoint);
            return ParseResult<AttributeValueSyntax>.NoMatch();
        }

        var integerSyntax = ParseResult<AttributeValueSyntax>.Success(
            new IntegerAttributeValueSyntax(
                signToken,
                integerToken,
                ApInt.Parse(64, value.ToString(CultureInfo.InvariantCulture), isSigned: true)));
        if (IsValidAttributeValueTermination(mode, allowTypedSuffix, stopBefore))
        {
            return integerSyntax;
        }

        Reset(checkpoint);
        return ParseResult<AttributeValueSyntax>.NoMatch();
    }

    private bool IsValidAttributeValueTermination(
        AttributeValueParsingMode mode,
        bool allowTypedSuffix,
        TokenKind[] stopBefore)
    {
        if (Is(TokenKind.EndOfFile))
        {
            return true;
        }

        for (var i = 0; i < stopBefore.Length; i++)
        {
            if (Current.TokenKind == stopBefore[i])
            {
                return true;
            }
        }

        if (allowTypedSuffix && Is(TokenKind.Colon))
        {
            return true;
        }

        return mode == AttributeValueParsingMode.StopAtOperationBoundary
            && IsOperationBoundary(Current, false);
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
    /// <remarks>
    /// When the dialect's custom format is responsible only for the body of the attribute (i.e. the
    /// tokens that appear after <c>#dialect.attr</c>), the method consumes the <c>#</c> and the name
    /// token before delegating to the format so that the format sees only what it needs to parse.
    /// The result is wrapped in a <see cref="Syntax.DialectPrefixedAttributeValueSyntax"/> so that the
    /// full <c>#name body</c> form is re-emitted correctly on the print path.
    /// If the format does not match, the tokens are put back via checkpoint reset.
    /// </remarks>
    private ParseResult<AttributeValueSyntax> TryParseSelfIdentifyingAttribute()
    {
        if (dialectRegistry == null)
        {
            return ParseResult<AttributeValueSyntax>.NoMatch();
        }

        var canonicalName = TryPeekAttributeDefinitionName();
        if (canonicalName == null || !dialectRegistry.TryGetAttribute(canonicalName, out var definition))
        {
            return ParseResult<AttributeValueSyntax>.NoMatch();
        }

        if (definition.AssemblyFormat == null)
        {
            // No custom format: fall through to raw syntax (the attribute will be bound later).
            return ParseResult<AttributeValueSyntax>.NoMatch();
        }

        if (!(definition.AssemblyFormat is IBodyOnlyAttributeAssemblyFormat))
        {
            // Legacy format that consumes '#name' itself: delegate without stripping the prefix.
            return TryParseCustomAttribute(definition);
        }

        // Body-only format (generated from AttrDef): consume '#' and name, then delegate.
        // The format sees only the body (e.g. `<"NULL">`).
        // Both consumed tokens are passed to TryParse via the context's Prefix property so
        // that the generated syntax class can store and replay the original source tokens.
        var outerCheckpoint = Mark();
        var hashToken = ConsumeToken();   // '#'
        var nameToken = ConsumeToken();   // 'dialect.attr' (lexed as a single identifier with the dot)
        var prefix = new Syntax.DialectAttributePrefix(hashToken, nameToken);

        var result = definition.AssemblyFormat.TryParse(new AttributeParsingContext(this, dialectRegistry, definition, prefix));
        if (result.IsSuccess)
        {
            // The generated syntax class is itself a DialectPrefixedAttributeValueSyntax and
            // already stores the prefix; no additional wrapping is needed.
            return result;
        }

        // Format returned NoMatch or Error — restore position to before '#name'.
        Reset(outerCheckpoint);
        return result.IsError ? result : ParseResult<AttributeValueSyntax>.NoMatch();
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

    /// <summary>
    /// Parses an array attribute value of the form <c>[ elem, elem, ... ]</c>.
    /// Each element is parsed as a generic attribute value stopping before <c>,</c> and <c>]</c>.
    /// </summary>
    private ParseResult<ArrayAttributeValueSyntax> TryParseArrayAttributeValue()
    {
        return TryParseRequiredCommaSeparatedDelimitedList(
            TokenKind.LBracket,
            TokenKind.RBracket,
            () => TryParseAttributeValue(AttributeValueParsingMode.Normal, (AttributeConstraintDefinition?)null, TokenKind.Comma, TokenKind.RBracket),
            "Expected '[' to start the array attribute.",
            "Expected ']' to close the array attribute.")
            .Map(static list => new ArrayAttributeValueSyntax(list.OpenToken!.Value, list.Items, list.SeparatorTokens, list.CloseToken!.Value));
    }

}
