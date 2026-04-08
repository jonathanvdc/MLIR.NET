namespace MLIR.Dialects.Extensions;

using System.Collections.Generic;
using MLIR.Semantics;
using MLIR.Syntax;
using MLIR.Syntax.Types.Collections;
using MLIR.Text;
using MLIR.Transforms;

/// <summary>
/// Runtime-backed custom assembly strategy for select-like operations whose upstream ODS
/// definitions still rely on handwritten C++ assembly parsing.
/// </summary>
/// <remarks>
/// This keeps binding on the intended parser hook path instead of relying on the binder to
/// recover structure from raw custom syntax later. The current implementation matches the
/// upstream <c>arith.select</c> custom spelling.
/// </remarks>
public sealed class SelectLikeOperationAssemblyFormat : IOperationAssemblyFormat
{
    private SelectLikeOperationAssemblyFormat()
    {
    }

    /// <summary>
    /// Gets the singleton instance of the select-like operation assembly format.
    /// </summary>
    public static SelectLikeOperationAssemblyFormat Instance { get; } = new();

    /// <inheritdoc/>
    public ParseResult<OperationBodySyntax> TryParse(
        Token nameToken,
        SeparatedSyntaxList<Token> resultList,
        Token? equalsToken,
        OperationParsingContext context)
    {
        var conditionResult = context.TryParseSsaToken();
        if (!conditionResult.IsSuccess)
        {
            return ParseResult<OperationBodySyntax>.Failure(conditionResult.Diagnostic!);
        }

        var commaTokenResult = context.Expect(TokenKind.Comma, "Expected ','.");
        if (!commaTokenResult.IsSuccess)
        {
            return ParseResult<OperationBodySyntax>.Failure(commaTokenResult.Diagnostic!);
        }

        var trueValueResult = context.TryParseSsaToken();
        if (!trueValueResult.IsSuccess)
        {
            return ParseResult<OperationBodySyntax>.Failure(trueValueResult.Diagnostic!);
        }

        var commaToken2Result = context.Expect(TokenKind.Comma, "Expected ','.");
        if (!commaToken2Result.IsSuccess)
        {
            return ParseResult<OperationBodySyntax>.Failure(commaToken2Result.Diagnostic!);
        }

        var falseValueResult = context.TryParseSsaToken();
        if (!falseValueResult.IsSuccess)
        {
            return ParseResult<OperationBodySyntax>.Failure(falseValueResult.Diagnostic!);
        }

        var attrDictResult = context.TryParseAttrDict();
        if (!attrDictResult.IsSuccess)
        {
            return ParseResult<OperationBodySyntax>.Failure(attrDictResult.Diagnostic!);
        }

        var colonTokenResult = context.Expect(TokenKind.Colon, "Expected ':'.");
        if (!colonTokenResult.IsSuccess)
        {
            return ParseResult<OperationBodySyntax>.Failure(colonTokenResult.Diagnostic!);
        }

        var firstTypeResult = context.TryParseTypeSyntax(TokenKind.Comma);
        if (!firstTypeResult.IsSuccess)
        {
            return ParseResult<OperationBodySyntax>.Failure(firstTypeResult.Diagnostic!);
        }

        Token? typeCommaToken = null;
        TypeSyntax? secondType = null;
        if (context.TryMatch(TokenKind.Comma, out var parsedTypeCommaToken))
        {
            typeCommaToken = parsedTypeCommaToken;
            var secondTypeResult = context.TryParseTypeSyntax();
            if (!secondTypeResult.IsSuccess)
            {
                return ParseResult<OperationBodySyntax>.Failure(secondTypeResult.Diagnostic!);
            }

            secondType = secondTypeResult.Value;
        }

        return ParseResult<OperationBodySyntax>.Success(new SelectLikeOperationBodySyntax(
            conditionResult.Value,
            commaTokenResult.Value,
            trueValueResult.Value,
            commaToken2Result.Value,
            falseValueResult.Value,
            attrDictResult.Value,
            colonTokenResult.Value,
            firstTypeResult.Value,
            typeCommaToken,
            secondType));
    }

    /// <inheritdoc/>
    public Operation Bind(OperationSyntax syntax, OperationDefinition definition, Binder binder)
    {
        if (syntax.Body is not SelectLikeOperationBodySyntax body)
        {
            binder.Report(new AssemblyDiagnostic(syntax.Location, "Expected a SelectLikeOperationBodySyntax but found " + syntax.Body.GetType().Name + "."));
            return new UninterpretedOperation(syntax, definition.Name);
        }

        if (syntax.ResultList.Count != definition.ResultCount.GetValueOrDefault(1))
        {
            binder.Report(new AssemblyDiagnostic(syntax.Location, "Expected exactly 1 result(s) but found " + syntax.ResultList.Count + "."));
            return new UninterpretedOperation(syntax, definition.Name);
        }

        var conditionType = body.SecondType != null ? body.FirstType : new RawTypeSyntax(new RawSyntaxText("i1"));
        var valueType = body.SecondType ?? body.FirstType;
        var typeSignature = binder.BindTypeReference(new RawSyntaxText(
            "(" + conditionType.ToString() + ", "
            + valueType.ToString() + ", "
            + valueType.ToString() + ") -> "
            + valueType.ToString()));

        var attributes = new List<NamedAttribute>(body.AttrDict.Items.Count);
        foreach (var attribute in body.AttrDict.Items)
        {
            attributes.Add(binder.BindNamedAttribute(attribute, definition));
        }

        return definition.Factory(new OperationConstructionContext(
            syntax,
            definition.Name,
            definition,
            new List<Region>(),
            new NamedAttributeCollection(attributes),
            typeSignature,
            binder.BindOperationResults(syntax.ResultList),
            binder.BindValueUses([body.Condition, body.TrueValue, body.FalseValue]),
            new Block?[] { }));
    }

    /// <inheritdoc/>
    public OperationSyntax BuildCustomAssemblySyntax(Operation operation, ConcreteSyntaxBuilderContext context)
    {
        if (!TryGetPrintedTypes(operation, out var conditionType, out var valueType, out var includeConditionType))
        {
            return context.RewriteOperation(operation, context.TransformGenericBody(operation));
        }

        var operands = operation.NonNullOperandValues.ToArray();
        if (operands.Length < 3)
        {
            return context.RewriteOperation(operation, context.TransformGenericBody(operation));
        }

        var condition = TokenFactory.SsaName(operands[0].Name);
        var trueValue = TokenFactory.SsaName(operands[1].Name);
        var falseValue = TokenFactory.SsaName(operands[2].Name);
        var body = new SelectLikeOperationBodySyntax(
            condition,
            TokenFactory.Comma(),
            trueValue,
            TokenFactory.Comma(),
            falseValue,
            context.BuildAttrDict(operation.Attributes),
            TokenFactory.Colon(),
            includeConditionType ? conditionType! : valueType!,
            includeConditionType ? TokenFactory.Comma() : null,
            includeConditionType ? valueType : null);
        return context.RewriteOperation(operation, body, TokenFactory.Identifier(operation.Name));
    }

    private static bool TryGetPrintedTypes(
        Operation operation,
        out TypeSyntax? conditionType,
        out TypeSyntax? valueType,
        out bool includeConditionType)
    {
        conditionType = null;
        valueType = null;
        includeConditionType = false;

        if (operation.TypeSignatureReference?.Syntax is not FunctionTypeSyntax functionType
            || functionType.InputTypes.Items.Count != 3)
        {
            return false;
        }

        conditionType = functionType.InputTypes.Items[0];
        valueType = functionType.InputTypes.Items[1];
        if (functionType.ResultType != null)
        {
            valueType = functionType.ResultType;
        }
        else if (functionType.ResultTypes.Items.Count == 1)
        {
            valueType = functionType.ResultTypes.Items[0];
        }

        if (valueType == null)
        {
            return false;
        }

        includeConditionType = !string.Equals(conditionType.ToString(), "i1", System.StringComparison.Ordinal);
        return true;
    }
}

/// <summary>
/// Concrete syntax node for select-like custom assembly.
/// </summary>
public sealed class SelectLikeOperationBodySyntax : OperationBodySyntax
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SelectLikeOperationBodySyntax"/> class.
    /// </summary>
    public SelectLikeOperationBodySyntax(
        Token condition,
        Token commaToken,
        Token trueValue,
        Token commaToken2,
        Token falseValue,
        DelimitedSyntaxList<NamedAttributeSyntax> attrDict,
        Token colonToken,
        TypeSyntax firstType,
        Token? typeCommaToken,
        TypeSyntax? secondType)
    {
        Condition = condition;
        CommaToken = commaToken;
        TrueValue = trueValue;
        CommaToken2 = commaToken2;
        FalseValue = falseValue;
        AttrDict = attrDict;
        ColonToken = colonToken;
        FirstType = firstType;
        TypeCommaToken = typeCommaToken;
        SecondType = secondType;
    }

    /// <summary>
    /// Gets the condition operand token.
    /// </summary>
    public Token Condition { get; }

    /// <summary>
    /// Gets the comma token after the condition operand.
    /// </summary>
    public Token CommaToken { get; }

    /// <summary>
    /// Gets the true-value operand token.
    /// </summary>
    public Token TrueValue { get; }

    /// <summary>
    /// Gets the comma token after the true-value operand.
    /// </summary>
    public Token CommaToken2 { get; }

    /// <summary>
    /// Gets the false-value operand token.
    /// </summary>
    public Token FalseValue { get; }

    /// <summary>
    /// Gets the optional attribute dictionary.
    /// </summary>
    public DelimitedSyntaxList<NamedAttributeSyntax> AttrDict { get; }

    /// <summary>
    /// Gets the colon token that introduces the printed type list.
    /// </summary>
    public Token ColonToken { get; }

    /// <summary>
    /// Gets the first printed type.
    /// </summary>
    public TypeSyntax FirstType { get; }

    /// <summary>
    /// Gets the comma token between the printed condition and value types, when present.
    /// </summary>
    public Token? TypeCommaToken { get; }

    /// <summary>
    /// Gets the second printed type when the custom syntax spells both condition and value types.
    /// </summary>
    public TypeSyntax? SecondType { get; }

    /// <inheritdoc/>
    public override SourceLocation Location
    {
        get
        {
            // Cover from the condition operand through to the last type token.
            var result = Condition.Location;
            var lastType = SecondType ?? FirstType;
            result = SourceLocation.Merge(result, lastType.Location);
            return result;
        }
    }

    /// <inheritdoc/>
    public override void WriteTo(Text.SyntaxWriter writer)
    {
        writer.WriteToken(Condition, " ");
        writer.WriteToken(CommaToken);
        writer.WriteToken(TrueValue, " ");
        writer.WriteToken(CommaToken2);
        writer.WriteToken(FalseValue, " ");
        writer.WriteDelimitedList(AttrDict, " ");
        writer.WriteToken(ColonToken);
        writer.SuggestTrivia(" ");
        FirstType.WriteTo(writer);
        if (TypeCommaToken.HasValue && SecondType != null)
        {
            writer.WriteToken(TypeCommaToken.Value);
            writer.SuggestTrivia(" ");
            SecondType.WriteTo(writer);
        }
    }

    /// <inheritdoc/>
    public override SyntaxNode Rewrite(SyntaxRewriter rewriter)
    {
        return new SelectLikeOperationBodySyntax(
            rewriter.VisitToken(Condition),
            rewriter.VisitToken(CommaToken),
            rewriter.VisitToken(TrueValue),
            rewriter.VisitToken(CommaToken2),
            rewriter.VisitToken(FalseValue),
            rewriter.VisitDelimitedList(AttrDict),
            rewriter.VisitToken(ColonToken),
            (TypeSyntax)rewriter.Visit(FirstType),
            rewriter.VisitToken(TypeCommaToken),
            SecondType != null ? (TypeSyntax)rewriter.Visit(SecondType) : null);
    }
}
