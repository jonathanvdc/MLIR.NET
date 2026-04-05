namespace MLIR.Text;

using MLIR.Dialects;
using MLIR.Dialects.Attributes.Collections;
using MLIR.Dialects.Attributes.Primitives;
using MLIR.Syntax;
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

    /// <summary>
    /// Parses an attribute value, trying production rules in the following priority order:
    /// <list type="number">
    ///   <item><description>Caller-supplied <paramref name="expectedDefinition"/> (if not <see langword="null"/>).</description></item>
    ///   <item><description>Self-identifying attributes of the form <c>#name</c> looked up in the dialect registry.</description></item>
    ///   <item><description>Built-in structured attributes: arrays (<c>[...]</c>), dictionaries (<c>{...}</c>),
    ///     dense arrays, and elements attributes.</description></item>
    ///   <item><description>Primitive numeric literals (floating-point and integer forms).</description></item>
    ///   <item><description>Raw token scan as a fallback <c>RawAttributeValueSyntax</c>.</description></item>
    /// </list>
    /// </summary>
    /// <param name="stopAtOperationBoundary">
    /// When <see langword="true"/>, the raw fallback scan stops at a newline operation boundary in addition to
    /// the explicit <paramref name="stopBefore"/> delimiters. This should be <see langword="true"/> when parsing
    /// inside a custom operation assembly format.
    /// </param>
    /// <param name="expectedDefinition">
    /// Optional hint from the caller indicating the expected attribute type. When supplied, the corresponding
    /// assembly format is tried first before the generic dispatch.
    /// </param>
    /// <param name="stopBefore">
    /// Token kinds that terminate the raw fallback scan at depth zero.
    /// </param>
    private ParseResult<AttributeValueSyntax> TryParseAttributeValueSyntaxResult(bool stopAtOperationBoundary, AttributeConstraintDefinition? expectedDefinition, params TokenKind[] stopBefore)
    {
        if (expectedDefinition != null)
        {
            var expectedResult = TryParseCustomAttributeSyntaxResult(expectedDefinition);
            if (!expectedResult.IsNoMatch)
            {
                return expectedResult;
            }
        }

        var selfIdentifyingResult = TryParseSelfIdentifyingAttributeSyntaxResult();
        if (!selfIdentifyingResult.IsNoMatch)
        {
            return selfIdentifyingResult;
        }

        var builtinStructuredResult = TryParseBuiltinStructuredAttributeSyntaxResult();
        if (!builtinStructuredResult.IsNoMatch)
        {
            return builtinStructuredResult;
        }

        var numericLiteralResult = TryParseNumericAttributeSyntaxResult(stopAtOperationBoundary, stopBefore);
        if (!numericLiteralResult.IsNoMatch)
        {
            return numericLiteralResult;
        }

        var rawResult = stopAtOperationBoundary
            ? TryParseRawUntilDelimiterOrBoundaryResult(stopBefore)
            : TryParseRawUntilDelimiterResult(stopBefore);
        return rawResult.IsSuccess
            ? ParseResult<AttributeValueSyntax>.Success(new RawAttributeValueSyntax(rawResult.Value))
            : ParseResult<AttributeValueSyntax>.Failure(rawResult.Diagnostic!);
    }

    /// <summary>
    /// Overload of <see cref="TryParseAttributeValueSyntaxResult(bool, AttributeConstraintDefinition?, TokenKind[])"/>
    /// that resolves the expected definition by name from the dialect registry.
    /// When <paramref name="expectedDefinitionName"/> is <see langword="null"/> or empty, or when no matching
    /// definition is found in the registry, the method falls back to unguided dispatch.
    /// </summary>
    private ParseResult<AttributeValueSyntax> TryParseAttributeValueSyntaxResult(bool stopAtOperationBoundary, string? expectedDefinitionName, params TokenKind[] stopBefore)
    {
        AttributeConstraintDefinition? expectedDefinition = null;
        if (!string.IsNullOrEmpty(expectedDefinitionName) && dialectRegistry != null)
        {
            dialectRegistry.TryResolveAttributeConstraint(expectedDefinitionName!, out expectedDefinition);
        }

        return TryParseAttributeValueSyntaxResult(stopAtOperationBoundary, expectedDefinition, stopBefore);
    }

    /// <summary>
    /// Tries to parse a built-in structured attribute value: an array literal (<c>[...]</c>),
    /// an attribute dictionary (<c>{...}</c>), a dense array, or an elements attribute.
    /// Returns <see cref="ParseOutcome.NoMatch"/> when none of those forms is present.
    /// </summary>
    private ParseResult<AttributeValueSyntax> TryParseBuiltinStructuredAttributeSyntaxResult()
    {
        if (Is(TokenKind.LBracket))
        {
            return TryParseArrayAttributeValueSyntaxResult().Map<AttributeValueSyntax>(static syntax => syntax);
        }

        if (Is(TokenKind.LBrace))
        {
            return TryParseAttrDictResult().Map<AttributeValueSyntax>(static syntax => new DictionaryAttributeValueSyntax(syntax));
        }

        var denseArrayResult = TryParseAttributeAssemblyFormatResult(BuiltinAttributeConstraintDefinition("DenseArrayAttr"), DenseArrayAttributeAssemblyFormat);
        if (!denseArrayResult.IsNoMatch)
        {
            return denseArrayResult;
        }

        return TryParseAttributeAssemblyFormatResult(BuiltinAttributeConstraintDefinition("ElementsAttr"), ElementsAttributeAssemblyFormat);
    }

    /// <summary>
    /// Tries to parse a primitive numeric attribute literal, preferring floating-point forms over integers.
    /// The method backtracks cleanly so partially-consumed non-numeric text can still fall through to raw syntax.
    /// </summary>
    private ParseResult<AttributeValueSyntax> TryParseNumericAttributeSyntaxResult(bool stopAtOperationBoundary, TokenKind[] stopBefore)
    {
        var checkpoint = Mark();

        var floatingPointResult = FloatingPointAssemblyFormatHelper.TryParseDecimalLiteral(new AttributeParsingContext(this, dialectRegistry, null));
        if (floatingPointResult.IsSuccess)
        {
            if (IsValidAttributeValueTermination(stopAtOperationBoundary, stopBefore))
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

        if (!IntegerLiteralAttributeAssemblyFormat.TryParseSignedIntegerLiteral(new AttributeParsingContext(this, dialectRegistry, null), out var rawText, out var value))
        {
            Reset(checkpoint);
            return ParseResult<AttributeValueSyntax>.NoMatch();
        }

        var integerSyntax = ParseResult<AttributeValueSyntax>.Success(
            new IntegerAttributeValueSyntax(IntegerLiteralAttributeAssemblyFormat.CreateSingleToken(rawText), value));
        if (IsValidAttributeValueTermination(stopAtOperationBoundary, stopBefore))
        {
            return integerSyntax;
        }

        Reset(checkpoint);
        return ParseResult<AttributeValueSyntax>.NoMatch();
    }

    /// <summary>
    /// Returns <see langword="true"/> when the current parser position is a valid termination point for
    /// a completed attribute value in the current parsing mode.
    /// </summary>
    private bool IsValidAttributeValueTermination(bool stopAtOperationBoundary, TokenKind[] stopBefore)
    {
        if (Is(TokenKind.EndOfFile))
        {
            return true;
        }

        for (var i = 0; i < stopBefore.Length; i++)
        {
            if (Current.Kind == stopBefore[i])
            {
                return true;
            }
        }

        return stopAtOperationBoundary && IsOperationBoundary(Current, false);
    }

    /// <summary>
    /// Invokes a specific <see cref="IAttributeAssemblyFormat"/> handler against the current parser position.
    /// Saves a checkpoint before calling and restores it when the handler returns <c>NoMatch</c>.
    /// Propagates <c>Success</c> and <c>Error</c> unchanged.
    /// </summary>
    private ParseResult<AttributeValueSyntax> TryParseAttributeAssemblyFormatResult(
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
    internal ParseResult<AttributeValueSyntax> TryParseAttributeValueSyntaxInternal(params TokenKind[] delimiters)
    {
        return TryParseAttributeValueSyntaxResult(false, (AttributeDefinition?)null, delimiters);
    }

    /// <summary>
    /// Bridges name-guided attribute value parsing for use by <see cref="DialectParsingContext"/>.
    /// </summary>
    internal ParseResult<AttributeValueSyntax> TryParseAttributeValueSyntaxInternal(string? expectedDefinitionName, params TokenKind[] delimiters)
    {
        return TryParseAttributeValueSyntaxResult(false, expectedDefinitionName, delimiters);
    }

    /// <summary>
    /// Bridges definition-guided attribute value parsing for use by <see cref="DialectParsingContext"/>.
    /// </summary>
    internal ParseResult<AttributeValueSyntax> TryParseAttributeValueSyntaxInternal(AttributeConstraintDefinition expectedDefinition, params TokenKind[] delimiters)
    {
        return TryParseAttributeValueSyntaxResult(false, expectedDefinition, delimiters);
    }

    /// <summary>
    /// Bridges unguided attribute value parsing with operation-boundary stopping for use by
    /// <see cref="OperationParsingContext"/>. The boundary stop is required inside custom operation
    /// assembly formats so that the parser does not consume tokens that belong to the next operation.
    /// </summary>
    internal ParseResult<AttributeValueSyntax> TryParseAttributeValueSyntaxOrBoundaryInternal(params TokenKind[] delimiters)
    {
        return TryParseAttributeValueSyntaxResult(true, (AttributeDefinition?)null, delimiters);
    }

    /// <summary>
    /// Bridges name-guided attribute value parsing with operation-boundary stopping for use by
    /// <see cref="OperationParsingContext"/>.
    /// </summary>
    internal ParseResult<AttributeValueSyntax> TryParseAttributeValueSyntaxOrBoundaryInternal(string? expectedDefinitionName, params TokenKind[] delimiters)
    {
        return TryParseAttributeValueSyntaxResult(true, expectedDefinitionName, delimiters);
    }

    /// <summary>
    /// Bridges definition-guided attribute value parsing with operation-boundary stopping for use by
    /// <see cref="OperationParsingContext"/>.
    /// </summary>
    internal ParseResult<AttributeValueSyntax> TryParseAttributeValueSyntaxOrBoundaryInternal(AttributeConstraintDefinition expectedDefinition, params TokenKind[] delimiters)
    {
        return TryParseAttributeValueSyntaxResult(true, expectedDefinition, delimiters);
    }

    /// <summary>
    /// Parses a named attribute entry of the form <c>name = value</c> or <c>name : value</c>.
    /// The name may be a bare identifier or a quoted string literal.
    /// </summary>
    private ParseResult<NamedAttributeSyntax> TryParseAttributeResult()
    {
        SyntaxToken nameToken;
        if (Is(TokenKind.Identifier) || Is(TokenKind.StringLiteral))
        {
            nameToken = ToSyntaxToken(ConsumeToken());
        }
        else
        {
            return ParseResult<NamedAttributeSyntax>.Failure(CreateDiagnostic("Expected an attribute name."));
        }

        SyntaxToken separatorToken;
        if (TryMatch(TokenKind.Equal, out var equalsToken))
        {
            separatorToken = ToSyntaxToken(equalsToken);
        }
        else if (TryMatch(TokenKind.Colon, out var colonToken))
        {
            separatorToken = ToSyntaxToken(colonToken);
        }
        else
        {
            return ParseResult<NamedAttributeSyntax>.Failure(CreateDiagnostic("Expected '=' or ':' after attribute name."));
        }

        return TryParseAttributeValueSyntaxResult(false, (AttributeConstraintDefinition?)null, TokenKind.Comma, TokenKind.RBrace)
            .Map(valueSyntax => new NamedAttributeSyntax(nameToken, separatorToken, valueSyntax));
    }

    /// <summary>
    /// Parses a standalone attribute value that must consume the entire input string.
    /// Returns a failure when any tokens remain after the attribute value.
    /// Used by the public <see cref="ParseAttributeValue"/> and <c>TryParseAttributeValue</c> entry points.
    /// </summary>
    private ParseResult<AttributeValueSyntax> TryParseStandaloneAttributeValueResult(AttributeConstraintDefinition? expectedDefinition)
    {
        var parsed = TryParseAttributeValueSyntaxResult(false, expectedDefinition);
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
    private ParseResult<AttributeValueSyntax> TryParseCustomAttributeSyntaxResult(AttributeConstraintDefinition? definition)
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
    private ParseResult<AttributeValueSyntax> TryParseSelfIdentifyingAttributeSyntaxResult()
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

        return TryParseCustomAttributeSyntaxResult(definition);
    }

    /// <summary>
    /// Parses an optional attribute dictionary of the form <c>{ name = value, ... }</c>.
    /// Returns an empty list when no <c>{</c> is present.
    /// </summary>
    private ParseResult<DelimitedSyntaxList<NamedAttributeSyntax>> TryParseAttrDictResult()
    {
        if (!Is(TokenKind.LBrace))
        {
            return ParseResult<DelimitedSyntaxList<NamedAttributeSyntax>>.Success(EmptyDelimitedSyntaxList<NamedAttributeSyntax>());
        }

        return TryParseRequiredCommaSeparatedDelimitedList(
            TokenKind.LBrace,
            TokenKind.RBrace,
            TryParseAttributeResult,
            "Expected '{' to start the attribute dictionary.",
            "Expected '}' to close the attribute dictionary.");
    }
}
