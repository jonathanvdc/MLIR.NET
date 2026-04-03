namespace MLIR.Text;

using System.Collections.Generic;
using MLIR.Dialects;
using MLIR.Dialects.Attributes.Collections;
using MLIR.Dialects.Attributes.Primitives;
using MLIR.Syntax;
using MLIR.Syntax.Attributes.Collections;
using MLIR.Syntax.Types.Collections;
using MLIR.Syntax.Types.Primitives;

public sealed partial class Parser
{
    private AttributeValueSyntax ParseAttributeValueSyntax(bool stopAtOperationBoundary, string? expectedDefinitionName, params TokenKind[] stopBefore)
    {
        var result = TryParseAttributeValueSyntaxResult(stopAtOperationBoundary, expectedDefinitionName, stopBefore);
        if (result.IsSuccess)
        {
            return result.Value;
        }

        throw new ParseException(result.Diagnostic!);
    }

    private AttributeValueSyntax ParseAttributeValueSyntax(bool stopAtOperationBoundary, AttributeConstraintDefinition? expectedDefinition, params TokenKind[] stopBefore)
    {
        var result = TryParseAttributeValueSyntaxResult(stopAtOperationBoundary, expectedDefinition, stopBefore);
        if (result.IsSuccess)
        {
            return result.Value;
        }

        throw new ParseException(result.Diagnostic!);
    }

    private TypeSyntax ParseTypeSyntax(params TokenKind[] stopBefore)
    {
        var result = TryParseTypeSyntaxResult(stopBefore);
        if (result.IsSuccess)
        {
            return result.Value;
        }

        throw new ParseException(result.Diagnostic!);
    }

    private TypeSyntax ParseTypeSyntax(string[] stopBeforeKeywords, params TokenKind[] stopBefore)
    {
        var result = TryParseTypeSyntaxResult(stopBeforeKeywords, stopBefore);
        if (result.IsSuccess)
        {
            return result.Value;
        }

        throw new ParseException(result.Diagnostic!);
    }

    private TypeSyntax ParseTypeSyntaxUntilOperationBoundary()
    {
        var result = TryParseTypeSyntaxUntilOperationBoundaryResult();
        if (result.IsSuccess)
        {
            return result.Value;
        }

        throw new ParseException(result.Diagnostic!);
    }

    private TypeSyntax ParseTypeSyntaxCore(TokenKind[] stopBefore, bool stopAtOperationBoundary)
    {
        return ParseTypeSyntaxCore(stopBefore, [], stopAtOperationBoundary);
    }

    private TypeSyntax ParseTypeSyntaxCore(TokenKind[] stopBefore, string[] stopBeforeKeywords, bool stopAtOperationBoundary)
    {
        if (TryParseBuiltinTypeSyntax(stopBefore, stopAtOperationBoundary, out var syntax))
        {
            return syntax;
        }

        if (TryParseCustomTypeSyntax(out syntax))
        {
            return syntax;
        }

        return new RawTypeSyntax(
            stopAtOperationBoundary
                ? ParseRawUntilDelimiterOrBoundaryInternal(stopBefore)
                : ParseRawUntilDelimiterOrKeyword(stopBefore, stopBeforeKeywords));
    }

    private bool TryParseCustomAttributeSyntax(string? expectedDefinitionName, out AttributeValueSyntax syntax)
    {
        syntax = null!;
        if (dialectRegistry == null)
        {
            return false;
        }

        var canonicalName = TryPeekAttributeDefinitionName();
        if (canonicalName == null)
        {
            return false;
        }

        return dialectRegistry.TryGetAttribute(canonicalName, out var definition)
            && TryParseCustomAttributeSyntax(definition, out syntax);
    }

    private bool TryParseSelfIdentifyingAttributeSyntax(out AttributeValueSyntax syntax)
    {
        syntax = null!;
        if (dialectRegistry == null)
        {
            return false;
        }

        var canonicalName = TryPeekAttributeDefinitionName();
        return canonicalName != null
            && dialectRegistry.TryGetAttribute(canonicalName, out var definition)
            && TryParseCustomAttributeSyntax(definition, out syntax);
    }

    private bool TryParseBuiltinStructuredAttributeSyntax(out AttributeValueSyntax syntax)
    {
        if (Is(TokenKind.LBracket))
        {
            syntax = ParseArrayAttributeValueSyntax();
            return true;
        }

        if (Is(TokenKind.LBrace))
        {
            syntax = new DictionaryAttributeValueSyntax(ParseAttrDictInternal());
            return true;
        }

        if (TryParseWith(BuiltinAttributeConstraintDefinition("DenseArrayAttr"), DenseArrayAttributeAssemblyFormat, out syntax))
        {
            return true;
        }

        if (TryParseWith(BuiltinAttributeConstraintDefinition("ElementsAttr"), ElementsAttributeAssemblyFormat, out syntax))
        {
            return true;
        }

        syntax = null!;
        return false;
    }

    private bool TryParseWith(AttributeConstraintDefinition? definition, IAttributeAssemblyFormat assemblyFormat, out AttributeValueSyntax syntax)
    {
        syntax = null!;
        var checkpoint = Mark();
        if (assemblyFormat.TryParse(new AttributeParsingContext(this, dialectRegistry, definition), out var customSyntax))
        {
            syntax = customSyntax!;
            return true;
        }

        Reset(checkpoint);
        return false;
    }

    private ArrayAttributeValueSyntax ParseArrayAttributeValueSyntax()
    {
        var result = TryParseArrayAttributeValueSyntaxResult();
        if (result.IsSuccess)
        {
            return result.Value;
        }

        throw new ParseException(result.Diagnostic!);
    }

    private ParseResult<ArrayAttributeValueSyntax> TryParseArrayAttributeValueSyntaxResult()
    {
        var list = TryParseRequiredCommaSeparatedDelimitedList(
            TokenKind.LBracket,
            TokenKind.RBracket,
            () => TryParseAttributeValueSyntaxResult(false, (AttributeConstraintDefinition?)null, TokenKind.Comma, TokenKind.RBracket),
            "Expected '[' to start the array attribute.",
            "Expected ']' to close the array attribute.");
        if (!list.IsSuccess)
        {
            return ParseResult<ArrayAttributeValueSyntax>.Failure(list.Diagnostic!);
        }

        return ParseResult<ArrayAttributeValueSyntax>.Success(new ArrayAttributeValueSyntax(list.Value.OpenToken!.Value, list.Value.Items, list.Value.SeparatorTokens, list.Value.CloseToken!.Value));
    }

    private static AttributeConstraintDefinition BuiltinAttributeConstraintDefinition(string name)
    {
        return new AttributeConstraintDefinition(name);
    }

    private bool TryParseCustomAttributeSyntax(AttributeConstraintDefinition? definition, out AttributeValueSyntax syntax)
    {
        syntax = null!;
        if (definition?.AssemblyFormat == null)
        {
            return false;
        }

        var checkpoint = Mark();
        if (definition.AssemblyFormat.TryParse(new AttributeParsingContext(this, dialectRegistry, definition), out var customSyntax))
        {
            syntax = customSyntax!;
            return true;
        }

        Reset(checkpoint);
        return false;
    }

    private bool TryParseCustomTypeSyntax(out TypeSyntax syntax)
    {
        syntax = null!;
        if (dialectRegistry == null)
        {
            return false;
        }

        var canonicalName = TryPeekTypeDefinitionName();
        if (canonicalName == null || !dialectRegistry.TryGetType(canonicalName, out var definition) || definition.AssemblyFormat == null)
        {
            return false;
        }

        var checkpoint = Mark();
        if (definition.AssemblyFormat.TryParse(new TypeParsingContext(this), out var customSyntax))
        {
            syntax = customSyntax!;
            return true;
        }

        Reset(checkpoint);
        return false;
    }

    private bool TryParseBuiltinTypeSyntax(TokenKind[] stopBefore, bool stopAtOperationBoundary, out TypeSyntax syntax)
    {
        syntax = null!;
        var checkpoint = Mark();
        if (TryParseFunctionTypeSyntax(stopBefore, stopAtOperationBoundary, out syntax)
            || TryParseTupleTypeSyntax(out syntax)
            || TryParseTensorTypeSyntax(out syntax)
            || TryParseVectorTypeSyntax(out syntax)
            || TryParseMemRefTypeSyntax(out syntax)
            || TryParseBuiltinPrimitiveTypeSyntax(out syntax))
        {
            return true;
        }

        Reset(checkpoint);
        return false;
    }

    private bool TryParseBuiltinPrimitiveTypeSyntax(out TypeSyntax syntax)
    {
        syntax = null!;
        if (!Is(TokenKind.Identifier))
        {
            return false;
        }

        var token = ToSyntaxToken(ConsumeToken());
        if (TryParseBuiltinIntegerName(token.Text, out var signedness, out var width))
        {
            syntax = new BuiltinIntegerTypeSyntax(token, signedness, width);
            return true;
        }

        if (IsBuiltinFloatName(token.Text))
        {
            syntax = new BuiltinFloatTypeSyntax(token);
            return true;
        }

        if (token.Text == "index")
        {
            syntax = new BuiltinIndexTypeSyntax(token);
            return true;
        }

        if (token.Text == "none")
        {
            syntax = new BuiltinNoneTypeSyntax(token);
            return true;
        }

        position--;
        return false;
    }

    private bool TryParseFunctionTypeSyntax(TokenKind[] stopBefore, bool stopAtOperationBoundary, out TypeSyntax syntax)
    {
        syntax = null!;
        if (!Is(TokenKind.LParen))
        {
            return false;
        }

        var checkpoint = Mark();
        var inputs = ParseTypeList(TokenKind.LParen, TokenKind.RParen, stopAtOperationBoundary: false);
        if (!TryMatch(TokenKind.Arrow, out var arrowToken))
        {
            Reset(checkpoint);
            return false;
        }

        TypeSyntax? resultType = null;
        DelimitedSyntaxList<TypeSyntax> resultTypes;
        if (Is(TokenKind.LParen))
        {
            resultTypes = ParseTypeList(TokenKind.LParen, TokenKind.RParen, stopAtOperationBoundary);
        }
        else
        {
            resultTypes = new DelimitedSyntaxList<TypeSyntax>(null, [], [], null);
            resultType = ParseTypeSyntaxCore(stopBefore, stopAtOperationBoundary);
        }

        syntax = new FunctionTypeSyntax(inputs, ToSyntaxToken(arrowToken), resultType, resultTypes);
        return true;
    }

    private bool TryParseTupleTypeSyntax(out TypeSyntax syntax)
    {
        syntax = null!;
        if (!IsKeyword("tuple"))
        {
            return false;
        }

        var keyword = ExpectKeywordInternal("tuple", "Expected 'tuple'.");
        var elements = ParseRequiredCommaSeparatedDelimitedList(
            TokenKind.LessThan,
            TokenKind.GreaterThan,
            () => ParseTypeSyntax(TokenKind.Comma, TokenKind.GreaterThan),
            "Expected '<' after 'tuple'.",
            "Expected '>' to close the tuple type.");

        syntax = new TupleTypeSyntax(keyword, elements.OpenToken!.Value, elements.Items, elements.SeparatorTokens, elements.CloseToken!.Value);
        return true;
    }

    private bool TryParseTensorTypeSyntax(out TypeSyntax syntax)
    {
        syntax = null!;
        if (!IsKeyword("tensor"))
        {
            return false;
        }

        var keyword = ExpectKeywordInternal("tensor", "Expected 'tensor'.");
        var lessThan = ExpectToken(TokenKind.LessThan, "Expected '<' after 'tensor'.");
        var prefix = ParseRawUntilDelimiter(TokenKind.Comma, TokenKind.GreaterThan);
        if (!TryParseShapedTypeBody(prefix.Text, allowUnranked: true, minimumDimensionCount: 0, out var dimensions, out var xTokens, out var unrankedToken, out var elementTypeText))
        {
            return false;
        }

        var elementType = ParseType(elementTypeText, dialectRegistry);
        var trailingCommaTokens = new List<SyntaxToken>();
        var trailingParameters = new List<RawSyntaxText>();
        while (TryMatch(TokenKind.Comma, out var comma))
        {
            trailingCommaTokens.Add(ToSyntaxToken(comma));
            trailingParameters.Add(ParseRawUntilDelimiter(TokenKind.Comma, TokenKind.GreaterThan));
        }

        var greaterThan = ExpectToken(TokenKind.GreaterThan, "Expected '>' to close the tensor type.");
        syntax = new TensorTypeSyntax(keyword, lessThan, dimensions, xTokens, unrankedToken, elementType, trailingCommaTokens, trailingParameters, greaterThan);
        return true;
    }

    private bool TryParseVectorTypeSyntax(out TypeSyntax syntax)
    {
        syntax = null!;
        if (!IsKeyword("vector"))
        {
            return false;
        }

        var checkpoint = Mark();
        var keyword = ExpectKeywordInternal("vector", "Expected 'vector'.");
        var lessThan = ExpectToken(TokenKind.LessThan, "Expected '<' after 'vector'.");
        var prefix = ParseRawUntilDelimiter(TokenKind.GreaterThan);
        if (!TryParseShapedTypeBody(prefix.Text, allowUnranked: false, minimumDimensionCount: 1, out var dimensions, out var xTokens, out _, out var elementTypeText))
        {
            Reset(checkpoint);
            return false;
        }

        var elementType = ParseType(elementTypeText, dialectRegistry);
        var greaterThan = ExpectToken(TokenKind.GreaterThan, "Expected '>' to close the vector type.");
        syntax = new VectorTypeSyntax(keyword, lessThan, dimensions, xTokens, elementType, greaterThan);
        return true;
    }

    private bool TryParseMemRefTypeSyntax(out TypeSyntax syntax)
    {
        syntax = null!;
        if (!IsKeyword("memref"))
        {
            return false;
        }

        var keyword = ExpectKeywordInternal("memref", "Expected 'memref'.");
        var lessThan = ExpectToken(TokenKind.LessThan, "Expected '<' after 'memref'.");
        var prefix = ParseRawUntilDelimiter(TokenKind.Comma, TokenKind.GreaterThan);
        if (!TryParseShapedTypeBody(prefix.Text, allowUnranked: true, minimumDimensionCount: 0, out var dimensions, out var xTokens, out var unrankedToken, out var elementTypeText))
        {
            return false;
        }

        var elementType = ParseType(elementTypeText, dialectRegistry);
        var trailingCommaTokens = new List<SyntaxToken>();
        var trailingParameters = new List<RawSyntaxText>();
        while (TryMatch(TokenKind.Comma, out var comma))
        {
            trailingCommaTokens.Add(ToSyntaxToken(comma));
            trailingParameters.Add(ParseRawUntilDelimiter(TokenKind.Comma, TokenKind.GreaterThan));
        }

        var greaterThan = ExpectToken(TokenKind.GreaterThan, "Expected '>' to close the memref type.");
        syntax = new MemRefTypeSyntax(keyword, lessThan, dimensions, xTokens, unrankedToken, elementType, trailingCommaTokens, trailingParameters, greaterThan);
        return true;
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

    private DelimitedSyntaxList<TypeSyntax> ParseTypeList(TokenKind openKind, TokenKind closeKind, bool stopAtOperationBoundary)
    {
        var result = TryParseTypeListResult(openKind, closeKind, stopAtOperationBoundary);
        if (result.IsSuccess)
        {
            return result.Value;
        }

        throw new ParseException(result.Diagnostic!);
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
}
