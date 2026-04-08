namespace MLIR.Text;

using System.Collections.Generic;
using MLIR.Dialects;
using MLIR.Semantics.Types.Primitives;
using MLIR.Syntax;
using MLIR.Syntax.Attributes.Collections;
using MLIR.Syntax.Types.Collections;
using MLIR.Syntax.Types.Primitives;

public sealed partial class Parser
{
    /// <summary>
    /// Parses an array attribute value of the form <c>[ elem, elem, ... ]</c>.
    /// Each element is parsed as a generic attribute value stopping before <c>,</c> and <c>]</c>.
    /// </summary>
    private ParseResult<ArrayAttributeValueSyntax> TryParseArrayAttributeValueSyntaxResult()
    {
        return TryParseRequiredCommaSeparatedDelimitedList(
            TokenKind.LBracket,
            TokenKind.RBracket,
            () => TryParseAttributeValueSyntaxResult(false, (AttributeConstraintDefinition?)null, TokenKind.Comma, TokenKind.RBracket),
            "Expected '[' to start the array attribute.",
            "Expected ']' to close the array attribute.")
            .Map(static list => new ArrayAttributeValueSyntax(list.OpenToken!.Value, list.Items, list.SeparatorTokens, list.CloseToken!.Value));
    }

    /// <summary>
    /// Creates a minimal <see cref="AttributeConstraintDefinition"/> for a built-in attribute type
    /// identified by <paramref name="name"/> only (no assembly format). Used to pass type context to
    /// assembly format handlers for built-in types such as <c>DenseArrayAttr</c> and <c>ElementsAttr</c>.
    /// </summary>
    private static AttributeConstraintDefinition BuiltinAttributeConstraintDefinition(string name)
    {
        return new AttributeConstraintDefinition(name);
    }

    /// <summary>
    /// Parses a shaped-type body string of the form <c>dim×dim×...×elementType</c> or
    /// <c>*×elementType</c> (unranked), splitting it into dimension tokens and the element type text.
    /// </summary>
    /// <remarks>
    /// The body text arrives pre-extracted from a raw token scan so that the shaped-type parsers
    /// (<see cref="TryParseTensorTypeSyntaxResult"/>, <see cref="TryParseVectorTypeSyntaxResult"/>,
    /// <see cref="TryParseMemRefTypeSyntaxResult"/>) can re-use a single parsing kernel.
    /// Dimension separators are literal <c>x</c> characters, and dimensions are either <c>?</c>
    /// (dynamic) or a sequence of decimal digits (static).
    /// </remarks>
    /// <param name="text">The raw shaped-type body text, not including the surrounding angle brackets.</param>
    /// <param name="allowUnranked">When <see langword="true"/>, a leading <c>*x</c> is accepted as an unranked marker.</param>
    /// <param name="minimumDimensionCount">Minimum number of explicit dimensions required for a successful parse.</param>
    /// <param name="dimensions">Receives the parsed dimension nodes.</param>
    /// <param name="xTokens">Receives the <c>x</c> separator tokens between dimensions.</param>
    /// <param name="unrankedToken">Receives the <c>*</c> token when an unranked type was matched.</param>
    /// <param name="elementTypeText">Receives the element type text following the last dimension.</param>
    /// <returns>
    /// <see langword="true"/> when the body was successfully split; <see langword="false"/> when the
    /// text does not match the expected shape.
    /// </returns>
    private static bool TryParseShapedTypeBody(
        string text,
        bool allowUnranked,
        int minimumDimensionCount,
        out List<ShapedTypeDimensionSyntax> dimensions,
        out List<Token> xTokens,
        out Token? unrankedToken,
        out string elementTypeText)
    {
        dimensions = [];
        xTokens = [];
        unrankedToken = null;
        elementTypeText = string.Empty;

        if (allowUnranked && text.StartsWith("*x", System.StringComparison.Ordinal))
        {
            unrankedToken = TokenFactory.Star();
            xTokens.Add(TokenFactory.Identifier("x"));
            elementTypeText = text.Substring(2);
            return elementTypeText.Length > 0;
        }

        var index = 0;
        while (index < text.Length)
        {
            if (text[index] == '?')
            {
                dimensions.Add(new DynamicShapedTypeDimensionSyntax(TokenFactory.Question()));
                index++;
            }
            else if (char.IsDigit(text[index]))
            {
                var start = index;
                while (index < text.Length && char.IsDigit(text[index]))
                {
                    index++;
                }

                var digits = text.Substring(start, index - start);
                dimensions.Add(new StaticShapedTypeDimensionSyntax(TokenFactory.Integer(digits), long.Parse(digits)));
            }
            else
            {
                break;
            }

            if (index >= text.Length || text[index] != 'x')
            {
                return false;
            }

            xTokens.Add(TokenFactory.Identifier("x"));
            index++;
        }

        if (dimensions.Count < minimumDimensionCount)
        {
            return false;
        }

        elementTypeText = text.Substring(index);
        return elementTypeText.Length > 0;
    }

    /// <summary>
    /// Parses a comma-separated, delimited type list enclosed by <paramref name="openKind"/> and <paramref name="closeKind"/>.
    /// </summary>
    private ParseResult<DelimitedSyntaxList<TypeSyntax>> TryParseTypeListResult(TokenKind openKind, TokenKind closeKind, bool stopAtOperationBoundary)
    {
        return TryParseRequiredCommaSeparatedDelimitedList(
            openKind,
            closeKind,
            () => TryParseTypeSyntaxCoreResult([TokenKind.Comma, closeKind], stopAtOperationBoundary),
            $"Expected '{TokenText(openKind)}' to start the type list.",
            $"Expected '{TokenText(closeKind)}' to close the type list.");
    }

    /// <summary>
    /// Attempts to parse an integer type name of the form <c>i&lt;n&gt;</c>, <c>si&lt;n&gt;</c>, or <c>ui&lt;n&gt;</c>.
    /// Returns <see langword="false"/> when the text does not match any integer type prefix.
    /// </summary>
    private static bool TryParseBuiltinIntegerName(string text, out IntegerTypeSignedness signedness, out int width)
    {
        signedness = IntegerTypeSignedness.Signless;
        width = 0;

        string digits;
        if (text.Length > 1 && text[0] == 'i')
        {
            digits = text.Substring(1);
        }
        else if (text.Length > 2 && text[0] == 's' && text[1] == 'i')
        {
            signedness = IntegerTypeSignedness.Signed;
            digits = text.Substring(2);
        }
        else if (text.Length > 2 && text[0] == 'u' && text[1] == 'i')
        {
            signedness = IntegerTypeSignedness.Unsigned;
            digits = text.Substring(2);
        }
        else
        {
            return false;
        }

        return int.TryParse(digits, out width);
    }

    /// <summary>
    /// Returns <see langword="true"/> when <paramref name="text"/> is one of the MLIR built-in
    /// floating-point type names: <c>bf16</c>, <c>f16</c>, <c>f32</c>, <c>f64</c>, <c>f80</c>, <c>f128</c>, or <c>tf32</c>.
    /// </summary>
    private static bool IsBuiltinFloatName(string text)
    {
        return text is "bf16" or "f16" or "f32" or "f64" or "f80" or "f128" or "tf32";
    }

    /// <summary>Returns <see langword="true"/> when the current token is an identifier matching <paramref name="text"/>.</summary>
    private bool IsKeyword(string text)
    {
        return Is(TokenKind.Identifier) && Current.Text == text;
    }

    /// <summary>
    /// Attempts to parse a type using a dialect-registered custom assembly format.
    /// Peeks at the current position to determine the type name, looks it up in the dialect registry,
    /// and invokes the registered format handler. Resets the position on <c>NoMatch</c>.
    /// Returns <see cref="ParseOutcome.NoMatch"/> when no registry is available or the name is not registered.
    /// </summary>
    private ParseResult<TypeSyntax> TryParseCustomTypeSyntaxResult()
    {
        if (dialectRegistry == null)
        {
            return ParseResult<TypeSyntax>.NoMatch();
        }

        var canonicalName = TryPeekTypeDefinitionName();
        if (canonicalName == null || !dialectRegistry.TryGetType(canonicalName, out var definition) || definition.AssemblyFormat == null)
        {
            return ParseResult<TypeSyntax>.NoMatch();
        }

        var checkpoint = Mark();
        var result = definition.AssemblyFormat.TryParse(new TypeParsingContext(this));
        if (result.IsSuccess)
        {
            return result;
        }

        Reset(checkpoint);
        return result.IsError ? result : ParseResult<TypeSyntax>.NoMatch();
    }

    /// <summary>
    /// Re-parses a standalone type text string (typically the element-type fragment extracted from a shaped type)
    /// by recursively invoking the public <c>TryParseType</c> entry point with the current dialect registry.
    /// </summary>
    private ParseResult<TypeSyntax> TryParseNestedStandaloneTypeText(string text)
    {
        return TryParseType(text, dialectRegistry, out var type, out var diagnostic)
            ? ParseResult<TypeSyntax>.Success(type!)
            : ParseResult<TypeSyntax>.Failure(diagnostic!);
    }

    /// <summary>
    /// Tries to parse a built-in MLIR type (function type, tuple, tensor, vector, memref, or primitive)
    /// using backtracking. Each alternative is tried in order; the position is reset between alternatives
    /// when an earlier one returns <c>NoMatch</c>.
    /// </summary>
    private ParseResult<TypeSyntax> TryParseBuiltinTypeSyntaxResult(TokenKind[] stopBefore, bool stopAtOperationBoundary)
    {
        var checkpoint = Mark();

        var functionResult = TryParseFunctionTypeSyntaxResult(stopBefore, stopAtOperationBoundary);
        if (!functionResult.IsNoMatch)
        {
            return functionResult;
        }

        Reset(checkpoint);
        var tupleResult = TryParseTupleTypeSyntaxResult();
        if (!tupleResult.IsNoMatch)
        {
            return tupleResult;
        }

        Reset(checkpoint);
        var tensorResult = TryParseTensorTypeSyntaxResult();
        if (!tensorResult.IsNoMatch)
        {
            return tensorResult;
        }

        Reset(checkpoint);
        var vectorResult = TryParseVectorTypeSyntaxResult();
        if (!vectorResult.IsNoMatch)
        {
            return vectorResult;
        }

        Reset(checkpoint);
        var memRefResult = TryParseMemRefTypeSyntaxResult();
        if (!memRefResult.IsNoMatch)
        {
            return memRefResult;
        }

        Reset(checkpoint);
        return TryParseBuiltinPrimitiveTypeSyntaxResult();
    }

    /// <summary>
    /// Attempts to parse a built-in primitive type: an integer type (<c>i32</c>, <c>si8</c>, <c>ui16</c>),
    /// a floating-point type (<c>f32</c>, etc.), <c>index</c>, or <c>none</c>.
    /// Consumes the identifier and returns <c>NoMatch</c> by un-consuming it when the text is not recognized.
    /// </summary>
    private ParseResult<TypeSyntax> TryParseBuiltinPrimitiveTypeSyntaxResult()
    {
        if (!Is(TokenKind.Identifier))
        {
            return ParseResult<TypeSyntax>.NoMatch();
        }

        var token = ConsumeToken();
        if (TryParseBuiltinIntegerName(token.Text, out var signedness, out var width))
        {
            return ParseResult<TypeSyntax>.Success(new BuiltinIntegerTypeSyntax(token, signedness, width));
        }

        if (IsBuiltinFloatName(token.Text))
        {
            return ParseResult<TypeSyntax>.Success(new BuiltinFloatTypeSyntax(token));
        }

        if (token.Text == "index")
        {
            return ParseResult<TypeSyntax>.Success(new BuiltinIndexTypeSyntax(token));
        }

        if (token.Text == "none")
        {
            return ParseResult<TypeSyntax>.Success(new BuiltinNoneTypeSyntax(token));
        }

        position--;
        return ParseResult<TypeSyntax>.NoMatch();
    }

    /// <summary>
    /// Attempts to parse a function type: <c>(types) -> type</c> or <c>(types) -> (types)</c>.
    /// Returns <c>NoMatch</c> when there is no opening <c>(</c>, or when a <c>(types)</c> is present but not
    /// followed by <c>-&gt;</c> (which could be a parenthesized expression rather than a function type).
    /// </summary>
    private ParseResult<TypeSyntax> TryParseFunctionTypeSyntaxResult(TokenKind[] stopBefore, bool stopAtOperationBoundary)
    {
        if (!Is(TokenKind.LParen))
        {
            return ParseResult<TypeSyntax>.NoMatch();
        }

        var checkpoint = Mark();
        var inputsResult = TryParseTypeListResult(TokenKind.LParen, TokenKind.RParen, stopAtOperationBoundary: false);
        if (!inputsResult.IsSuccess)
        {
            return ParseResult<TypeSyntax>.Failure(inputsResult.Diagnostic!);
        }

        if (!TryMatch(TokenKind.Arrow, out var arrowToken))
        {
            Reset(checkpoint);
            return ParseResult<TypeSyntax>.NoMatch();
        }

        TypeSyntax? resultType = null;
        DelimitedSyntaxList<TypeSyntax> resultTypes;
        if (Is(TokenKind.LParen))
        {
            var resultTypesResult = TryParseTypeListResult(TokenKind.LParen, TokenKind.RParen, stopAtOperationBoundary);
            if (!resultTypesResult.IsSuccess)
            {
                return ParseResult<TypeSyntax>.Failure(resultTypesResult.Diagnostic!);
            }

            resultTypes = resultTypesResult.Value;
        }
        else
        {
            resultTypes = new DelimitedSyntaxList<TypeSyntax>(null, [], [], null);
            var resultTypeResult = TryParseTypeSyntaxCoreResult(stopBefore, stopAtOperationBoundary);
            if (!resultTypeResult.IsSuccess)
            {
                return ParseResult<TypeSyntax>.Failure(resultTypeResult.Diagnostic!);
            }

            resultType = resultTypeResult.Value;
        }

        return ParseResult<TypeSyntax>.Success(new FunctionTypeSyntax(inputsResult.Value, arrowToken, resultType, resultTypes));
    }

    /// <summary>
    /// Attempts to parse a tuple type: <c>tuple&lt;type, ...&gt;</c>.
    /// Returns <c>NoMatch</c> when the current token is not the <c>tuple</c> keyword.
    /// </summary>
    private ParseResult<TypeSyntax> TryParseTupleTypeSyntaxResult()
    {
        if (!IsKeyword("tuple"))
        {
            return ParseResult<TypeSyntax>.NoMatch();
        }

        var keywordResult = ExpectKeywordResult("tuple", "Expected 'tuple'.");
        if (!keywordResult.IsSuccess)
        {
            return ParseResult<TypeSyntax>.Failure(keywordResult.Diagnostic!);
        }

        var elementsResult = TryParseRequiredCommaSeparatedDelimitedList(
            TokenKind.LessThan,
            TokenKind.GreaterThan,
            () => TryParseTypeSyntaxResult(TokenKind.Comma, TokenKind.GreaterThan),
            "Expected '<' after 'tuple'.",
            "Expected '>' to close the tuple type.");
        if (!elementsResult.IsSuccess)
        {
            return ParseResult<TypeSyntax>.Failure(elementsResult.Diagnostic!);
        }

        return ParseResult<TypeSyntax>.Success(new TupleTypeSyntax(keywordResult.Value, elementsResult.Value.OpenToken!.Value, elementsResult.Value.Items, elementsResult.Value.SeparatorTokens, elementsResult.Value.CloseToken!.Value));
    }

    /// <summary>
    /// Attempts to parse a tensor type: <c>tensor&lt;dims×elementType, ...&gt;</c>.
    /// Supports ranked and unranked (<c>*x</c>) forms.
    /// Trailing comma-separated parameters after the element type are captured as raw text.
    /// Returns <c>NoMatch</c> when the current token is not the <c>tensor</c> keyword.
    /// </summary>
    private ParseResult<TypeSyntax> TryParseTensorTypeSyntaxResult()
    {
        if (!IsKeyword("tensor"))
        {
            return ParseResult<TypeSyntax>.NoMatch();
        }

        var keywordResult = ExpectKeywordResult("tensor", "Expected 'tensor'.");
        if (!keywordResult.IsSuccess)
        {
            return ParseResult<TypeSyntax>.Failure(keywordResult.Diagnostic!);
        }

        var lessThanResult = ExpectTokenResult(TokenKind.LessThan, "Expected '<' after 'tensor'.");
        if (!lessThanResult.IsSuccess)
        {
            return ParseResult<TypeSyntax>.Failure(lessThanResult.Diagnostic!);
        }

        var prefixResult = TryParseRawUntilDelimiterResult(TokenKind.Comma, TokenKind.GreaterThan);
        if (!prefixResult.IsSuccess)
        {
            return ParseResult<TypeSyntax>.Failure(prefixResult.Diagnostic!);
        }

        if (!TryParseShapedTypeBody(prefixResult.Value.Text, allowUnranked: true, minimumDimensionCount: 0, out var dimensions, out var xTokens, out var unrankedToken, out var elementTypeText))
        {
            return ParseResult<TypeSyntax>.NoMatch();
        }

        var elementTypeResult = TryParseNestedStandaloneTypeText(elementTypeText);
        if (!elementTypeResult.IsSuccess)
        {
            return elementTypeResult;
        }

        var trailingCommaTokens = new List<Token>();
        var trailingParameters = new List<RawSyntaxText>();
        while (TryMatch(TokenKind.Comma, out var comma))
        {
            trailingCommaTokens.Add(comma);
            var trailingResult = TryParseRawUntilDelimiterResult(TokenKind.Comma, TokenKind.GreaterThan);
            if (!trailingResult.IsSuccess)
            {
                return ParseResult<TypeSyntax>.Failure(trailingResult.Diagnostic!);
            }

            trailingParameters.Add(trailingResult.Value);
        }

        var greaterThanResult = ExpectTokenResult(TokenKind.GreaterThan, "Expected '>' to close the tensor type.");
        if (!greaterThanResult.IsSuccess)
        {
            return ParseResult<TypeSyntax>.Failure(greaterThanResult.Diagnostic!);
        }

        return ParseResult<TypeSyntax>.Success(new TensorTypeSyntax(keywordResult.Value, lessThanResult.Value, dimensions, xTokens, unrankedToken, elementTypeResult.Value, trailingCommaTokens, trailingParameters, greaterThanResult.Value));
    }

    /// <summary>
    /// Attempts to parse a vector type: <c>vector&lt;dims×elementType&gt;</c>.
    /// Requires at least one explicit dimension; unranked forms are not valid for vectors.
    /// Returns <c>NoMatch</c> when the current token is not the <c>vector</c> keyword or when
    /// the body does not parse as a valid shaped-type body.
    /// </summary>
    private ParseResult<TypeSyntax> TryParseVectorTypeSyntaxResult()
    {
        if (!IsKeyword("vector"))
        {
            return ParseResult<TypeSyntax>.NoMatch();
        }

        var checkpoint = Mark();
        var keywordResult = ExpectKeywordResult("vector", "Expected 'vector'.");
        if (!keywordResult.IsSuccess)
        {
            return ParseResult<TypeSyntax>.Failure(keywordResult.Diagnostic!);
        }

        var lessThanResult = ExpectTokenResult(TokenKind.LessThan, "Expected '<' after 'vector'.");
        if (!lessThanResult.IsSuccess)
        {
            return ParseResult<TypeSyntax>.Failure(lessThanResult.Diagnostic!);
        }

        var prefixResult = TryParseRawUntilDelimiterResult(TokenKind.GreaterThan);
        if (!prefixResult.IsSuccess)
        {
            return ParseResult<TypeSyntax>.Failure(prefixResult.Diagnostic!);
        }

        if (!TryParseShapedTypeBody(prefixResult.Value.Text, allowUnranked: false, minimumDimensionCount: 1, out var dimensions, out var xTokens, out _, out var elementTypeText))
        {
            Reset(checkpoint);
            return ParseResult<TypeSyntax>.NoMatch();
        }

        var elementTypeParse = TryParseNestedStandaloneTypeText(elementTypeText);
        if (!elementTypeParse.IsSuccess)
        {
            return elementTypeParse;
        }

        var greaterThanResult = ExpectTokenResult(TokenKind.GreaterThan, "Expected '>' to close the vector type.");
        if (!greaterThanResult.IsSuccess)
        {
            return ParseResult<TypeSyntax>.Failure(greaterThanResult.Diagnostic!);
        }

        return ParseResult<TypeSyntax>.Success(new VectorTypeSyntax(keywordResult.Value, lessThanResult.Value, dimensions, xTokens, elementTypeParse.Value, greaterThanResult.Value));
    }

    /// <summary>
    /// Attempts to parse a memref type: <c>memref&lt;dims×elementType, ...&gt;</c>.
    /// Supports ranked and unranked (<c>*x</c>) forms.
    /// Trailing comma-separated parameters (affine maps, memory spaces) are captured as raw text.
    /// Returns <c>NoMatch</c> when the current token is not the <c>memref</c> keyword.
    /// </summary>
    private ParseResult<TypeSyntax> TryParseMemRefTypeSyntaxResult()
    {
        if (!IsKeyword("memref"))
        {
            return ParseResult<TypeSyntax>.NoMatch();
        }

        var keywordResult = ExpectKeywordResult("memref", "Expected 'memref'.");
        if (!keywordResult.IsSuccess)
        {
            return ParseResult<TypeSyntax>.Failure(keywordResult.Diagnostic!);
        }

        var lessThanResult = ExpectTokenResult(TokenKind.LessThan, "Expected '<' after 'memref'.");
        if (!lessThanResult.IsSuccess)
        {
            return ParseResult<TypeSyntax>.Failure(lessThanResult.Diagnostic!);
        }

        var prefixResult = TryParseRawUntilDelimiterResult(TokenKind.Comma, TokenKind.GreaterThan);
        if (!prefixResult.IsSuccess)
        {
            return ParseResult<TypeSyntax>.Failure(prefixResult.Diagnostic!);
        }

        if (!TryParseShapedTypeBody(prefixResult.Value.Text, allowUnranked: true, minimumDimensionCount: 0, out var dimensions, out var xTokens, out var unrankedToken, out var elementTypeText))
        {
            return ParseResult<TypeSyntax>.NoMatch();
        }

        var elementTypeParse = TryParseNestedStandaloneTypeText(elementTypeText);
        if (!elementTypeParse.IsSuccess)
        {
            return elementTypeParse;
        }

        var trailingCommaTokens = new List<Token>();
        var trailingParameters = new List<RawSyntaxText>();
        while (TryMatch(TokenKind.Comma, out var comma))
        {
            trailingCommaTokens.Add(comma);
            var trailingResult = TryParseRawUntilDelimiterResult(TokenKind.Comma, TokenKind.GreaterThan);
            if (!trailingResult.IsSuccess)
            {
                return ParseResult<TypeSyntax>.Failure(trailingResult.Diagnostic!);
            }

            trailingParameters.Add(trailingResult.Value);
        }

        var greaterThanResult = ExpectTokenResult(TokenKind.GreaterThan, "Expected '>' to close the memref type.");
        if (!greaterThanResult.IsSuccess)
        {
            return ParseResult<TypeSyntax>.Failure(greaterThanResult.Diagnostic!);
        }

        return ParseResult<TypeSyntax>.Success(new MemRefTypeSyntax(keywordResult.Value, lessThanResult.Value, dimensions, xTokens, unrankedToken, elementTypeParse.Value, trailingCommaTokens, trailingParameters, greaterThanResult.Value));
    }

    /// <summary>
    /// Parses a standalone type that must consume the entire input.
    /// Returns a failure when tokens remain after the type.
    /// Used by the public <see cref="ParseType"/> and <c>TryParseType</c> entry points.
    /// </summary>
    private ParseResult<TypeSyntax> TryParseStandaloneTypeResult()
    {
        var parsed = TryParseTypeSyntaxUntilOperationBoundaryResult();
        if (!parsed.IsSuccess)
        {
            return parsed;
        }

        return !Is(TokenKind.EndOfFile)
            ? ParseResult<TypeSyntax>.Failure(CreateDiagnostic("Expected the type to consume the entire input."))
            : parsed;
    }

    /// <summary>
    /// Parses a type, stopping before any of the supplied delimiter token kinds.
    /// Does not stop at operation boundaries.
    /// </summary>
    private ParseResult<TypeSyntax> TryParseTypeSyntaxResult(params TokenKind[] stopBefore)
    {
        return TryParseTypeSyntaxCoreResult(stopBefore, stopAtOperationBoundary: false);
    }

    /// <summary>
    /// Parses a type, stopping before any of the supplied delimiter token kinds or keyword spellings.
    /// Does not stop at operation boundaries.
    /// </summary>
    private ParseResult<TypeSyntax> TryParseTypeSyntaxResult(string[] stopBeforeKeywords, params TokenKind[] stopBefore)
    {
        return TryParseTypeSyntaxCoreResult(stopBefore, stopBeforeKeywords, stopAtOperationBoundary: false);
    }

    /// <summary>
    /// Parses a type, stopping at an operation boundary (newline in leading trivia) rather than an explicit delimiter.
    /// Used for operation type signatures where no closing delimiter token is present.
    /// </summary>
    private ParseResult<TypeSyntax> TryParseTypeSyntaxUntilOperationBoundaryResult()
    {
        return TryParseTypeSyntaxCoreResult([], stopAtOperationBoundary: true);
    }

    /// <summary>
    /// Parses a comma-separated list of types until an operation boundary is reached.
    /// This is used by custom operation assembly formats such as <c>type($variadic)</c>,
    /// where the list is not enclosed in parentheses but still needs depth-aware parsing.
    /// </summary>
    private IReadOnlyList<TypeSyntax> ParseTypeSyntaxListUntilOperationBoundary()
    {
        var items = new List<TypeSyntax>();
        while (true)
        {
            var itemResult = TryParseTypeSyntaxCoreResult([TokenKind.Comma], stopAtOperationBoundary: true);
            if (!itemResult.IsSuccess)
            {
                throw new ParseException(itemResult.Diagnostic!);
            }

            items.Add(itemResult.Value);
            if (!TryMatch(TokenKind.Comma, out _))
            {
                break;
            }
        }

        return items;
    }

    /// <summary>
    /// Core type parsing dispatcher: tries built-in types, then registered dialect types, then raw fallback.
    /// </summary>
    private ParseResult<TypeSyntax> TryParseTypeSyntaxCoreResult(TokenKind[] stopBefore, bool stopAtOperationBoundary)
    {
        return TryParseTypeSyntaxCoreResult(stopBefore, [], stopAtOperationBoundary);
    }

    /// <summary>
    /// Core type parsing dispatcher with both delimiter and keyword stop conditions.
    /// <list type="number">
    ///   <item><description>Tries built-in type forms (function, tuple, tensor, vector, memref, primitive) via
    ///     <see cref="TryParseBuiltinTypeSyntaxResult"/>.</description></item>
    ///   <item><description>Tries registered dialect types via <see cref="TryParseCustomTypeSyntaxResult"/>.</description></item>
    ///   <item><description>Falls back to a raw token scan that produces a <c>RawTypeSyntax</c>.</description></item>
    /// </list>
    /// </summary>
    private ParseResult<TypeSyntax> TryParseTypeSyntaxCoreResult(TokenKind[] stopBefore, string[] stopBeforeKeywords, bool stopAtOperationBoundary)
    {
        var builtinTypeResult = TryParseBuiltinTypeSyntaxResult(stopBefore, stopAtOperationBoundary);
        if (!builtinTypeResult.IsNoMatch)
        {
            return builtinTypeResult;
        }

        var customTypeResult = TryParseCustomTypeSyntaxResult();
        if (!customTypeResult.IsNoMatch)
        {
            return customTypeResult;
        }

        var rawResult = stopAtOperationBoundary
            ? TryParseRawUntilDelimiterOrBoundaryResult(stopBefore)
            : TryParseRawUntilDelimiterOrKeywordResult(stopBefore, stopBeforeKeywords);
        return rawResult.Map<TypeSyntax>(static raw => new RawTypeSyntax(raw));
    }

    /// <summary>Bridges <see cref="TryParseTypeSyntaxResult(TokenKind[])"/> for use by <see cref="DialectParsingContext"/>.</summary>
    internal ParseResult<TypeSyntax> TryParseTypeSyntaxInternal(params TokenKind[] delimiters)
    {
        return TryParseTypeSyntaxResult(delimiters);
    }

    /// <summary>Bridges <see cref="TryParseTypeSyntaxResult(string[], TokenKind[])"/> for use by <see cref="DialectParsingContext"/>.</summary>
    internal ParseResult<TypeSyntax> TryParseTypeSyntaxInternal(string[] stopBeforeKeywords, params TokenKind[] delimiters)
    {
        return TryParseTypeSyntaxResult(stopBeforeKeywords, delimiters);
    }

    /// <summary>Bridges <see cref="TryParseTypeSyntaxUntilOperationBoundaryResult"/> for use by <see cref="DialectParsingContext"/>.</summary>
    internal ParseResult<TypeSyntax> TryParseTypeSyntaxUntilOperationBoundaryInternal()
    {
        return TryParseTypeSyntaxUntilOperationBoundaryResult();
    }

    /// <summary>Bridges <see cref="ParseTypeSyntaxListUntilOperationBoundary"/> for use by <see cref="OperationParsingContext"/>.</summary>
    internal IReadOnlyList<TypeSyntax> ParseTypeSyntaxListUntilOperationBoundaryInternal()
    {
        return ParseTypeSyntaxListUntilOperationBoundary();
    }
}
