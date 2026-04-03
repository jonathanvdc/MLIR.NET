namespace MLIR.Dialects.Extensions;

using System.Collections.Generic;
using System.Linq;
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
    /// <inheritdoc/>
    public bool TryParse(
        SyntaxToken nameToken,
        IReadOnlyList<SyntaxToken> resultTokens,
        IReadOnlyList<SyntaxToken> resultCommaTokens,
        SyntaxToken? equalsToken,
        OperationParsingContext context,
        out OperationBodySyntax? body)
    {
        var condition = context.ParseSsaToken();
        var commaToken = context.Expect(TokenKind.Comma, "Expected ','.");
        var trueValue = context.ParseSsaToken();
        var commaToken2 = context.Expect(TokenKind.Comma, "Expected ','.");
        var falseValue = context.ParseSsaToken();
        var attrDict = context.ParseAttrDict();
        var colonToken = context.Expect(TokenKind.Colon, "Expected ':'.");
        var firstType = context.ParseTypeSyntax(TokenKind.Comma);

        SyntaxToken? typeCommaToken = null;
        TypeSyntax? secondType = null;
        if (context.TryMatch(TokenKind.Comma, out var parsedTypeCommaToken))
        {
            typeCommaToken = parsedTypeCommaToken;
            secondType = context.ParseTypeSyntax();
        }

        body = new SelectLikeOperationBodySyntax(
            condition,
            commaToken,
            trueValue,
            commaToken2,
            falseValue,
            attrDict,
            colonToken,
            firstType,
            typeCommaToken,
            secondType);
        return true;
    }

    /// <inheritdoc/>
    public Operation Bind(OperationSyntax syntax, OperationDefinition definition, Binder binder)
    {
        if (syntax.Body is not SelectLikeOperationBodySyntax body)
        {
            binder.Report(new AssemblyDiagnostic(syntax.Location, "Expected a SelectLikeOperationBodySyntax but found " + syntax.Body.GetType().Name + "."));
            return new UninterpretedOperation(syntax, definition.Name);
        }

        if (syntax.ResultTokens.Count != definition.ResultCount.GetValueOrDefault(1))
        {
            binder.Report(new AssemblyDiagnostic(syntax.Location, "Expected exactly 1 result(s) but found " + syntax.ResultTokens.Count + "."));
            return new UninterpretedOperation(syntax, definition.Name);
        }

        var conditionType = body.SecondType != null ? body.FirstType : new RawTypeSyntax(new RawSyntaxText("i1"));
        var valueType = body.SecondType ?? body.FirstType;
        var typeSignature = binder.BindTypeReference(new RawSyntaxText(
            "(" + conditionType.GetRawText().Text + ", "
            + valueType.GetRawText().Text + ", "
            + valueType.GetRawText().Text + ") -> "
            + valueType.GetRawText().Text));

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
            binder.BindOperationResults(syntax.ResultTokens),
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

        var operands = operation.OperandValues;
        if (operands.Count < 3)
        {
            return context.RewriteOperation(operation, context.TransformGenericBody(operation));
        }

        var condition = new SyntaxToken(operands[0].Name);
        var trueValue = new SyntaxToken(operands[1].Name);
        var falseValue = new SyntaxToken(operands[2].Name);
        var body = new SelectLikeOperationBodySyntax(
            condition,
            new SyntaxToken(","),
            trueValue,
            new SyntaxToken(","),
            falseValue,
            context.BuildAttrDict(operation.Attributes),
            new SyntaxToken(":"),
            includeConditionType ? conditionType! : valueType!,
            includeConditionType ? new SyntaxToken(",") : null,
            includeConditionType ? valueType : null);
        return context.RewriteOperation(operation, body, new SyntaxToken(operation.Name));
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

        includeConditionType = !string.Equals(conditionType.GetRawText().Text, "i1", System.StringComparison.Ordinal);
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
        SyntaxToken condition,
        SyntaxToken commaToken,
        SyntaxToken trueValue,
        SyntaxToken commaToken2,
        SyntaxToken falseValue,
        DelimitedSyntaxList<NamedAttributeSyntax> attrDict,
        SyntaxToken colonToken,
        TypeSyntax firstType,
        SyntaxToken? typeCommaToken,
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
    public SyntaxToken Condition { get; }

    /// <summary>
    /// Gets the comma token after the condition operand.
    /// </summary>
    public SyntaxToken CommaToken { get; }

    /// <summary>
    /// Gets the true-value operand token.
    /// </summary>
    public SyntaxToken TrueValue { get; }

    /// <summary>
    /// Gets the comma token after the true-value operand.
    /// </summary>
    public SyntaxToken CommaToken2 { get; }

    /// <summary>
    /// Gets the false-value operand token.
    /// </summary>
    public SyntaxToken FalseValue { get; }

    /// <summary>
    /// Gets the optional attribute dictionary.
    /// </summary>
    public DelimitedSyntaxList<NamedAttributeSyntax> AttrDict { get; }

    /// <summary>
    /// Gets the colon token that introduces the printed type list.
    /// </summary>
    public SyntaxToken ColonToken { get; }

    /// <summary>
    /// Gets the first printed type.
    /// </summary>
    public TypeSyntax FirstType { get; }

    /// <summary>
    /// Gets the comma token between the printed condition and value types, when present.
    /// </summary>
    public SyntaxToken? TypeCommaToken { get; }

    /// <summary>
    /// Gets the second printed type when the custom syntax spells both condition and value types.
    /// </summary>
    public TypeSyntax? SecondType { get; }

    /// <inheritdoc/>
    public override void WriteTo(Text.SyntaxWriter writer, int indentLevel)
    {
        writer.WriteToken(Condition, " ");
        writer.WriteToken(CommaToken, string.Empty);
        writer.WriteToken(TrueValue, " ");
        writer.WriteToken(CommaToken2, string.Empty);
        writer.WriteToken(FalseValue, " ");
        writer.WriteDelimitedList(AttrDict, " ");
        writer.WriteToken(ColonToken, string.Empty);
        FirstType.WriteTo(writer, " ");
        if (TypeCommaToken.HasValue && SecondType != null)
        {
            writer.WriteToken(TypeCommaToken.Value, string.Empty);
            SecondType.WriteTo(writer, " ");
        }
    }
}
