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
}
