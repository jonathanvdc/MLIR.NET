namespace MLIR.Text;

using MLIR.Dialects;
using MLIR.Dialects.Attributes.Collections;
using MLIR.Dialects.Attributes.Primitives;
using MLIR.Syntax;
using MLIR.Syntax.Attributes.Collections;

public sealed partial class Parser
{
    private static readonly BooleanLiteralAttributeAssemblyFormat BooleanLiteralAttributeAssemblyFormat = new();
    private static readonly IntegerLiteralAttributeAssemblyFormat IntegerLiteralAttributeAssemblyFormat = new();
    private static readonly FloatingPointLiteralAttributeAssemblyFormat FloatingPointLiteralAttributeAssemblyFormat = new();
    private static readonly StringLiteralAttributeAssemblyFormat StringLiteralAttributeAssemblyFormat = new();
    private static readonly DenseIntegerArrayAttributeAssemblyFormat DenseArrayAttributeAssemblyFormat = new();
    private static readonly ElementsAttributeAssemblyFormat ElementsAttributeAssemblyFormat = new();

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

        var rawResult = stopAtOperationBoundary
            ? TryParseRawUntilDelimiterOrBoundaryResult(stopBefore)
            : TryParseRawUntilDelimiterResult(stopBefore);
        return rawResult.IsSuccess
            ? ParseResult<AttributeValueSyntax>.Success(new RawAttributeValueSyntax(rawResult.Value))
            : ParseResult<AttributeValueSyntax>.Failure(rawResult.Diagnostic!);
    }

    private ParseResult<AttributeValueSyntax> TryParseAttributeValueSyntaxResult(bool stopAtOperationBoundary, string? expectedDefinitionName, params TokenKind[] stopBefore)
    {
        AttributeConstraintDefinition? expectedDefinition = null;
        if (!string.IsNullOrEmpty(expectedDefinitionName) && dialectRegistry != null)
        {
            dialectRegistry.TryResolveAttributeConstraint(expectedDefinitionName!, out expectedDefinition);
        }

        return TryParseAttributeValueSyntaxResult(stopAtOperationBoundary, expectedDefinition, stopBefore);
    }

    private ParseResult<AttributeValueSyntax> TryParseBuiltinStructuredAttributeSyntaxResult()
    {
        if (Is(TokenKind.LBracket))
        {
            var arrayResult = TryParseArrayAttributeValueSyntaxResult();
            return arrayResult.IsSuccess
                ? ParseResult<AttributeValueSyntax>.Success(arrayResult.Value)
                : ParseResult<AttributeValueSyntax>.Failure(arrayResult.Diagnostic!);
        }

        if (Is(TokenKind.LBrace))
        {
            var dictResult = TryParseAttrDictResult();
            return dictResult.IsSuccess
                ? ParseResult<AttributeValueSyntax>.Success(new DictionaryAttributeValueSyntax(dictResult.Value))
                : ParseResult<AttributeValueSyntax>.Failure(dictResult.Diagnostic!);
        }

        var denseArrayResult = TryParseAttributeAssemblyFormatResult(BuiltinAttributeConstraintDefinition("DenseArrayAttr"), DenseArrayAttributeAssemblyFormat);
        if (!denseArrayResult.IsNoMatch)
        {
            return denseArrayResult;
        }

        return TryParseAttributeAssemblyFormatResult(BuiltinAttributeConstraintDefinition("ElementsAttr"), ElementsAttributeAssemblyFormat);
    }

    private ParseResult<AttributeValueSyntax> TryParseAttributeAssemblyFormatResult(
        AttributeConstraintDefinition? definition,
        IAttributeAssemblyFormat assemblyFormat)
    {
        var checkpoint = Mark();
        if (assemblyFormat.TryParse(new AttributeParsingContext(this, dialectRegistry, definition), out var syntax))
        {
            return ParseResult<AttributeValueSyntax>.Success(syntax!);
        }

        Reset(checkpoint);
        return ParseResult<AttributeValueSyntax>.NoMatch();
    }

    internal AttributeValueSyntax ParseAttributeValueSyntaxInternal(params TokenKind[] delimiters)
    {
        var result = TryParseAttributeValueSyntaxResult(false, (AttributeDefinition?)null, delimiters);
        if (result.IsSuccess)
        {
            return result.Value;
        }

        throw new ParseException(result.Diagnostic!);
    }

    internal AttributeValueSyntax ParseAttributeValueSyntaxInternal(string? expectedDefinitionName, params TokenKind[] delimiters)
    {
        var result = TryParseAttributeValueSyntaxResult(false, expectedDefinitionName, delimiters);
        if (result.IsSuccess)
        {
            return result.Value;
        }

        throw new ParseException(result.Diagnostic!);
    }

    internal AttributeValueSyntax ParseAttributeValueSyntaxInternal(AttributeConstraintDefinition expectedDefinition, params TokenKind[] delimiters)
    {
        var result = TryParseAttributeValueSyntaxResult(false, expectedDefinition, delimiters);
        if (result.IsSuccess)
        {
            return result.Value;
        }

        throw new ParseException(result.Diagnostic!);
    }

    internal AttributeValueSyntax ParseAttributeValueSyntaxOrBoundaryInternal(params TokenKind[] delimiters)
    {
        var result = TryParseAttributeValueSyntaxResult(true, (AttributeDefinition?)null, delimiters);
        if (result.IsSuccess)
        {
            return result.Value;
        }

        throw new ParseException(result.Diagnostic!);
    }

    internal AttributeValueSyntax ParseAttributeValueSyntaxOrBoundaryInternal(string? expectedDefinitionName, params TokenKind[] delimiters)
    {
        var result = TryParseAttributeValueSyntaxResult(true, expectedDefinitionName, delimiters);
        if (result.IsSuccess)
        {
            return result.Value;
        }

        throw new ParseException(result.Diagnostic!);
    }

    internal AttributeValueSyntax ParseAttributeValueSyntaxOrBoundaryInternal(AttributeConstraintDefinition expectedDefinition, params TokenKind[] delimiters)
    {
        var result = TryParseAttributeValueSyntaxResult(true, expectedDefinition, delimiters);
        if (result.IsSuccess)
        {
            return result.Value;
        }

        throw new ParseException(result.Diagnostic!);
    }

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

        var valueResult = TryParseAttributeValueSyntaxResult(false, (AttributeConstraintDefinition?)null, TokenKind.Comma, TokenKind.RBrace);
        if (!valueResult.IsSuccess)
        {
            return ParseResult<NamedAttributeSyntax>.Failure(valueResult.Diagnostic!);
        }

        return ParseResult<NamedAttributeSyntax>.Success(new NamedAttributeSyntax(nameToken, separatorToken, valueResult.Value));
    }

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

    private ParseResult<AttributeValueSyntax> TryParseCustomAttributeSyntaxResult(AttributeConstraintDefinition? definition)
    {
        if (definition?.AssemblyFormat == null)
        {
            return ParseResult<AttributeValueSyntax>.NoMatch();
        }

        var checkpoint = Mark();
        if (definition.AssemblyFormat.TryParse(new AttributeParsingContext(this, dialectRegistry, definition), out var syntax))
        {
            return ParseResult<AttributeValueSyntax>.Success(syntax!);
        }

        Reset(checkpoint);
        return ParseResult<AttributeValueSyntax>.NoMatch();
    }

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
