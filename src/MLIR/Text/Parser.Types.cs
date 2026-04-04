namespace MLIR.Text;

using System.Collections.Generic;
using MLIR.Dialects;
using MLIR.Syntax;
using MLIR.Syntax.Attributes.Collections;
using MLIR.Syntax.Types.Collections;
using MLIR.Syntax.Types.Primitives;

public sealed partial class Parser
{
    private ParseResult<ArrayAttributeValueSyntax> TryParseArrayAttributeValueSyntaxResult()
    {
        return TryParseRequiredCommaSeparatedDelimitedList(
            TokenKind.LBracket,
            TokenKind.RBracket,
            () => TryParseAttributeValueSyntaxResult(false, (AttributeConstraintDefinition?)null, TokenKind.Comma, TokenKind.RBracket),
            "Expected '[' to start the array attribute.",
            "Expected ']' to close the array attribute.")
            .Map(list => new ArrayAttributeValueSyntax(list.OpenToken!.Value, list.Items, list.SeparatorTokens, list.CloseToken!.Value));
    }

    private static AttributeConstraintDefinition BuiltinAttributeConstraintDefinition(string name)
    {
        return new AttributeConstraintDefinition(name);
    }

    private static bool TryParseShapedTypeBody(
        string text,
        bool allowUnranked,
        int minimumDimensionCount,
        out List<ShapedTypeDimensionSyntax> dimensions,
        out List<SyntaxToken> xTokens,
        out SyntaxToken? unrankedToken,
        out string elementTypeText)
    {
        dimensions = [];
        xTokens = [];
        unrankedToken = null;
        elementTypeText = string.Empty;

        if (allowUnranked && text.StartsWith("*x", System.StringComparison.Ordinal))
        {
            unrankedToken = new SyntaxToken("*");
            xTokens.Add(new SyntaxToken("x"));
            elementTypeText = text.Substring(2);
            return elementTypeText.Length > 0;
        }

        var index = 0;
        while (index < text.Length)
        {
            if (text[index] == '?')
            {
                dimensions.Add(new DynamicShapedTypeDimensionSyntax(new SyntaxToken("?")));
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
                dimensions.Add(new StaticShapedTypeDimensionSyntax(new SyntaxToken(digits), long.Parse(digits)));
            }
            else
            {
                break;
            }

            if (index >= text.Length || text[index] != 'x')
            {
                return false;
            }

            xTokens.Add(new SyntaxToken("x"));
            index++;
        }

        if (dimensions.Count < minimumDimensionCount)
        {
            return false;
        }

        elementTypeText = text.Substring(index);
        return elementTypeText.Length > 0;
    }

    private ParseResult<DelimitedSyntaxList<TypeSyntax>> TryParseTypeListResult(TokenKind openKind, TokenKind closeKind, bool stopAtOperationBoundary)
    {
        return TryParseRequiredCommaSeparatedDelimitedList(
            openKind,
            closeKind,
            () => TryParseTypeSyntaxCoreResult([TokenKind.Comma, closeKind], stopAtOperationBoundary),
            $"Expected '{TokenText(openKind)}' to start the type list.",
            $"Expected '{TokenText(closeKind)}' to close the type list.");
    }

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

    private static bool IsBuiltinFloatName(string text)
    {
        return text is "bf16" or "f16" or "f32" or "f64" or "f80" or "f128" or "tf32";
    }

    private bool IsKeyword(string text)
    {
        return Is(TokenKind.Identifier) && Current.Text == text;
    }

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

    private ParseResult<TypeSyntax> TryParseNestedStandaloneTypeText(string text)
    {
        return TryParseType(text, dialectRegistry, out var type, out var diagnostic)
            ? ParseResult<TypeSyntax>.Success(type!)
            : ParseResult<TypeSyntax>.Failure(diagnostic!);
    }

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

    private ParseResult<TypeSyntax> TryParseBuiltinPrimitiveTypeSyntaxResult()
    {
        if (!Is(TokenKind.Identifier))
        {
            return ParseResult<TypeSyntax>.NoMatch();
        }

        var token = ToSyntaxToken(ConsumeToken());
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

        return ParseResult<TypeSyntax>.Success(new FunctionTypeSyntax(inputsResult.Value, ToSyntaxToken(arrowToken), resultType, resultTypes));
    }

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

        var trailingCommaTokens = new List<SyntaxToken>();
        var trailingParameters = new List<RawSyntaxText>();
        while (TryMatch(TokenKind.Comma, out var comma))
        {
            trailingCommaTokens.Add(ToSyntaxToken(comma));
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

        var trailingCommaTokens = new List<SyntaxToken>();
        var trailingParameters = new List<RawSyntaxText>();
        while (TryMatch(TokenKind.Comma, out var comma))
        {
            trailingCommaTokens.Add(ToSyntaxToken(comma));
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

    private ParseResult<TypeSyntax> TryParseTypeSyntaxResult(params TokenKind[] stopBefore)
    {
        return TryParseTypeSyntaxCoreResult(stopBefore, stopAtOperationBoundary: false);
    }

    private ParseResult<TypeSyntax> TryParseTypeSyntaxResult(string[] stopBeforeKeywords, params TokenKind[] stopBefore)
    {
        return TryParseTypeSyntaxCoreResult(stopBefore, stopBeforeKeywords, stopAtOperationBoundary: false);
    }

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

    private ParseResult<TypeSyntax> TryParseTypeSyntaxCoreResult(TokenKind[] stopBefore, bool stopAtOperationBoundary)
    {
        return TryParseTypeSyntaxCoreResult(stopBefore, [], stopAtOperationBoundary);
    }

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

    internal ParseResult<TypeSyntax> TryParseTypeSyntaxInternal(params TokenKind[] delimiters)
    {
        return TryParseTypeSyntaxResult(delimiters);
    }

    internal ParseResult<TypeSyntax> TryParseTypeSyntaxInternal(string[] stopBeforeKeywords, params TokenKind[] delimiters)
    {
        return TryParseTypeSyntaxResult(stopBeforeKeywords, delimiters);
    }

    internal ParseResult<TypeSyntax> TryParseTypeSyntaxUntilOperationBoundaryInternal()
    {
        return TryParseTypeSyntaxUntilOperationBoundaryResult();
    }

    internal IReadOnlyList<TypeSyntax> ParseTypeSyntaxListUntilOperationBoundaryInternal()
    {
        return ParseTypeSyntaxListUntilOperationBoundary();
    }
}
