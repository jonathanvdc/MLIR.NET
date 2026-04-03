namespace DialectTests;

using System.Collections.Generic;
using MLIR.Dialects;
using MLIR.Func;
using MLIR.Semantics;
using MLIR.Syntax;
using MLIR.Syntax.Attributes;
using MLIR.Syntax.Attributes.Primitives;
using MLIR.Text;
using MLIR.Transforms;

/// <summary>
/// Minimal handwritten assembly-format support for the upstream <c>func.func</c> examples
/// covered by these dialect integration tests.
/// </summary>
/// <remarks>
/// The generated dialect registration intentionally omits <c>func.func</c> because upstream
/// marks it with <c>hasCustomAssemblyFormat = 1</c>. For these tests we only need enough
/// support to bind the declarative examples, starting with external declarations such as
/// <c>func.func private @abort()</c>.
/// </remarks>
internal sealed class FuncOpAssemblyFormat : IOperationAssemblyFormat
{
    public bool TryParse(
        SyntaxToken nameToken,
        IReadOnlyList<SyntaxToken> resultTokens,
        IReadOnlyList<SyntaxToken> resultCommaTokens,
        SyntaxToken? equalsToken,
        OperationParsingContext context,
        out OperationBodySyntax? body)
    {
        SyntaxToken? visibilityKeyword = null;
        if (context.IsKeyword("public"))
        {
            visibilityKeyword = context.ExpectKeyword("public", "Expected 'public'.");
        }
        else if (context.IsKeyword("private"))
        {
            visibilityKeyword = context.ExpectKeyword("private", "Expected 'private'.");
        }
        else if (context.IsKeyword("nested"))
        {
            visibilityKeyword = context.ExpectKeyword("nested", "Expected 'nested'.");
        }

        var symbolName = context.ParseRawUntilDelimiter(TokenKind.LParen);
        var lParenToken = context.Expect(TokenKind.LParen, "Expected '(' after the function symbol name.");
        RawSyntaxText inputTypes = context.Is(TokenKind.RParen)
            ? new RawSyntaxText(string.Empty)
            : context.ParseRawUntilDelimiter(TokenKind.RParen);
        var rParenToken = context.Expect(TokenKind.RParen, "Expected ')' to close the function signature.");

        SyntaxToken? arrowToken = null;
        RawSyntaxText? resultTypes = null;
        if (context.TryMatch(TokenKind.Arrow, out var parsedArrow))
        {
            arrowToken = parsedArrow;
            resultTypes = context.ParseRawUntilDelimiterOrKeyword(["attributes"], TokenKind.LBrace);
        }

        var trailingSyntax = context.ParseRawUntilOperationBoundary();
        var functionTypeSyntax = Parser.ParseType(
            "(" + inputTypes.Text + ") -> " + (resultTypes?.Text ?? "()"));

        body = new FuncOpBodySyntax(
            visibilityKeyword,
            symbolName,
            lParenToken,
            inputTypes,
            rParenToken,
            arrowToken,
            resultTypes,
            trailingSyntax,
            functionTypeSyntax);
        return true;
    }

    public Operation Bind(OperationSyntax syntax, OperationDefinition definition, Binder binder)
    {
        if (syntax.Body is not FuncOpBodySyntax body)
        {
            binder.Report(new AssemblyDiagnostic(syntax.Location, "Expected a FuncOpBodySyntax but found " + syntax.Body.GetType().Name + "."));
            return new UninterpretedOperation(syntax, definition.Name);
        }

        var attributes = new List<NamedAttributeSyntax>
        {
            CreateStringAttribute("sym_name", NormalizeSymbolName(body.SymbolName.Text)),
        };
        if (body.VisibilityKeyword.HasValue)
        {
            attributes.Add(CreateStringAttribute("sym_visibility", body.VisibilityKeyword.Value.Text));
        }

        var boundAttributes = new List<NamedAttribute>();
        foreach (var attribute in attributes)
        {
            boundAttributes.Add(binder.BindNamedAttribute(attribute, definition));
        }

        return new FuncOp(
            syntax,
            functionType: null,
            attributes: new NamedAttributeCollection(boundAttributes),
            typeSignatureReference: binder.BindTypeReference(body.FunctionTypeSyntax));
    }

    public OperationSyntax BuildCustomAssemblySyntax(Operation operation, ConcreteSyntaxBuilderContext context)
    {
        return context.RewriteOperation(operation, context.TransformGenericBody(operation));
    }

    private static string NormalizeSymbolName(string text)
    {
        return text.StartsWith("@", System.StringComparison.Ordinal) ? text.Substring(1) : text;
    }

    private static NamedAttributeSyntax CreateStringAttribute(string name, string value)
    {
        var literal = new SyntaxToken("\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"");
        return new NamedAttributeSyntax(
            new SyntaxToken(name),
            new SyntaxToken("="),
            new StringAttributeValueSyntax(literal, value));
    }
}

internal sealed class FuncOpBodySyntax : OperationBodySyntax
{
    public FuncOpBodySyntax(
        SyntaxToken? visibilityKeyword,
        RawSyntaxText symbolName,
        SyntaxToken lParenToken,
        RawSyntaxText inputTypes,
        SyntaxToken rParenToken,
        SyntaxToken? arrowToken,
        RawSyntaxText? resultTypes,
        RawSyntaxText trailingSyntax,
        TypeSyntax functionTypeSyntax)
    {
        VisibilityKeyword = visibilityKeyword;
        SymbolName = symbolName;
        LParenToken = lParenToken;
        InputTypes = inputTypes;
        RParenToken = rParenToken;
        ArrowToken = arrowToken;
        ResultTypes = resultTypes;
        TrailingSyntax = trailingSyntax;
        FunctionTypeSyntax = functionTypeSyntax;
    }

    public SyntaxToken? VisibilityKeyword { get; }

    public RawSyntaxText SymbolName { get; }

    public SyntaxToken LParenToken { get; }

    public RawSyntaxText InputTypes { get; }

    public SyntaxToken RParenToken { get; }

    public SyntaxToken? ArrowToken { get; }

    public RawSyntaxText? ResultTypes { get; }

    public RawSyntaxText TrailingSyntax { get; }

    public TypeSyntax FunctionTypeSyntax { get; }

    public override void WriteTo(SyntaxWriter writer, int indentLevel)
    {
        if (VisibilityKeyword.HasValue)
        {
            writer.WriteToken(VisibilityKeyword.Value, " ");
        }

        writer.WriteRaw(SymbolName, " ");
        writer.WriteToken(LParenToken, string.Empty);
        writer.WriteRaw(InputTypes, string.Empty);
        writer.WriteToken(RParenToken, string.Empty);

        if (ArrowToken.HasValue && ResultTypes != null)
        {
            writer.WriteToken(ArrowToken.Value, " ");
            writer.WriteRaw(ResultTypes, " ");
        }

        if (TrailingSyntax.Text.Length > 0)
        {
            writer.WriteRaw(TrailingSyntax, " ");
        }
    }
}
