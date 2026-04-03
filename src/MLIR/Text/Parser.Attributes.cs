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

    internal ParseResult<AttributeValueSyntax> TryParseAttributeValueSyntaxInternal(params TokenKind[] delimiters)
    {
        return TryParseAttributeValueSyntaxResult(false, (AttributeDefinition?)null, delimiters);
    }

    internal ParseResult<AttributeValueSyntax> TryParseAttributeValueSyntaxInternal(string? expectedDefinitionName, params TokenKind[] delimiters)
    {
        return TryParseAttributeValueSyntaxResult(false, expectedDefinitionName, delimiters);
    }

    internal ParseResult<AttributeValueSyntax> TryParseAttributeValueSyntaxInternal(AttributeConstraintDefinition expectedDefinition, params TokenKind[] delimiters)
    {
        return TryParseAttributeValueSyntaxResult(false, expectedDefinition, delimiters);
    }

    internal ParseResult<AttributeValueSyntax> TryParseAttributeValueSyntaxOrBoundaryInternal(params TokenKind[] delimiters)
    {
        return TryParseAttributeValueSyntaxResult(true, (AttributeDefinition?)null, delimiters);
    }

    internal ParseResult<AttributeValueSyntax> TryParseAttributeValueSyntaxOrBoundaryInternal(string? expectedDefinitionName, params TokenKind[] delimiters)
    {
        return TryParseAttributeValueSyntaxResult(true, expectedDefinitionName, delimiters);
    }

    internal ParseResult<AttributeValueSyntax> TryParseAttributeValueSyntaxOrBoundaryInternal(AttributeConstraintDefinition expectedDefinition, params TokenKind[] delimiters)
    {
        return TryParseAttributeValueSyntaxResult(true, expectedDefinition, delimiters);
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

        return TryParseAttributeValueSyntaxResult(false, (AttributeConstraintDefinition?)null, TokenKind.Comma, TokenKind.RBrace)
            .Map(valueSyntax => new NamedAttributeSyntax(nameToken, separatorToken, valueSyntax));
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
        var result = definition.AssemblyFormat.TryParse(new AttributeParsingContext(this, dialectRegistry, definition));
        if (result.IsSuccess)
        {
            return result;
        }

        Reset(checkpoint);
        return result.IsError ? result : ParseResult<AttributeValueSyntax>.NoMatch();
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
