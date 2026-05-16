namespace MLIR.Generators.Emitters.AssemblyFormat;

using System.Collections.Generic;
using MLIR.Generators.Emitters.Common;
using MLIR.ODS.Model;

internal sealed class AssemblyFormatPlan
{
    public AssemblyFormatPlan(FormatSubject subject, IReadOnlyList<FormatSlot> slots, IReadOnlyList<string> unsupportedFeatures)
    {
        Subject = subject;
        Slots = slots;
        UnsupportedFeatures = unsupportedFeatures;
    }

    public FormatSubject Subject { get; }
    public IReadOnlyList<FormatSlot> Slots { get; }
    public IReadOnlyList<string> UnsupportedFeatures { get; }
    public bool IsSupported => UnsupportedFeatures.Count == 0;
}

internal enum FormatSlotKind
{
    LiteralToken,
    AttributeValue,
    Type,
    SsaValue,
    AttrDict,
}

internal sealed class FormatSlot
{
    private FormatSlot(
        string sourceName,
        string baseName,
        FormatSlotKind kind,
        string csType,
        string parseExpression,
        AttrOrTypeParameterModel? parameterModel = null,
        string? tokenText = null,
        string? tokenKindExpression = null,
        bool isKeyword = false)
    {
        SourceName = sourceName;
        BaseName = baseName;
        Kind = kind;
        CsType = csType;
        ParseExpression = parseExpression;
        ParameterModel = parameterModel;
        TokenText = tokenText;
        TokenKindExpression = tokenKindExpression;
        IsKeyword = isKeyword;
    }

    public string SourceName { get; }
    public string BaseName { get; }
    public FormatSlotKind Kind { get; }
    public string CsType { get; }
    public string ParseExpression { get; }
    public AttrOrTypeParameterModel? ParameterModel { get; }
    public string? TokenText { get; }
    public string? TokenKindExpression { get; }
    public bool IsKeyword { get; }
    public string PropertyName => DialectGeneratorNaming.ToPascalCase(BaseName);
    public string ParameterName => EmitterHelpers.LowerFirst(PropertyName);

    public string ParseValueExpression
    {
        get
        {
            var value = ParameterName + "Result.Value";
            return Kind == FormatSlotKind.AttributeValue || Kind == FormatSlotKind.Type
                ? "(" + CsType + ")" + value
                : value;
        }
    }

    public string RewriteExpression => Kind switch
    {
        FormatSlotKind.LiteralToken => "rewriter.VisitToken(" + PropertyName + ")",
        FormatSlotKind.AttributeValue => "(" + CsType + ")rewriter.Visit(" + PropertyName + ")",
        FormatSlotKind.Type => "(" + CsType + ")rewriter.Visit(" + PropertyName + ")",
        FormatSlotKind.SsaValue => "rewriter.VisitToken(" + PropertyName + ")",
        FormatSlotKind.AttrDict => "rewriter.VisitDelimitedList(" + PropertyName + ")",
        _ => PropertyName,
    };

    public string LocationExpression => PropertyName + ".Location";

    public static FormatSlot ForLiteral(string name, string text, string tokenKindExpression, bool isKeyword = false)
    {
        var parseExpression = isKeyword
            ? "context.ExpectKeyword(" + EmitterHelpers.ToCSharpStringLiteral(text) + ", " + EmitterHelpers.ToCSharpStringLiteral("Expected '" + text + "'.") + ")"
            : "context.Expect(" + tokenKindExpression + ", " + EmitterHelpers.ToCSharpStringLiteral("Expected '" + text + "'.") + ")";
        return new FormatSlot(name, name, FormatSlotKind.LiteralToken, "global::MLIR.Syntax.Token", parseExpression, tokenText: text, tokenKindExpression: tokenKindExpression, isKeyword: isKeyword);
    }

    public static FormatSlot ForParameter(string name, int ordinal, AttrOrTypeParameterModel parameter)
    {
        var syntaxType = !string.IsNullOrEmpty(parameter.CsharpSyntaxType)
            ? parameter.CsharpSyntaxType!
            : "global::MLIR.Syntax.AttributeValueSyntax";
        if (syntaxType == "TypeSyntax" || syntaxType == "global::MLIR.Syntax.TypeSyntax")
        {
            return new FormatSlot(name, name, FormatSlotKind.Type, "global::MLIR.Syntax.TypeSyntax", "context.TryParseTypeSyntax()", parameter);
        }

        var parseExpression = parameter.CsharpParserTemplate != null
            ? parameter.CsharpParserTemplate.Render("parser", "context")
            : "context.TryParseAttributeValueSyntax()";
        return new FormatSlot(name, name, FormatSlotKind.AttributeValue, syntaxType, parseExpression, parameter);
    }

    public static FormatSlot ForOperationVariable(string name, int ordinal, FormatSlotKind kind, string parseExpression)
    {
        var csType = kind == FormatSlotKind.SsaValue
            ? "global::MLIR.Syntax.Token"
            : "global::MLIR.Syntax.AttributeValueSyntax";
        return new FormatSlot(name, name, kind, csType, parseExpression);
    }

    public static FormatSlot ForDirective(string name, int ordinal, FormatSlotKind kind, string parseExpression, string csType)
        => new(name, name, kind, csType, parseExpression);

    public string BuildExpression(string typedLocalName)
    {
        if (Kind == FormatSlotKind.LiteralToken)
        {
            return IsKeyword
                ? "global::MLIR.Syntax.TokenFactory.Identifier(" + EmitterHelpers.ToCSharpStringLiteral(TokenText ?? string.Empty) + ")"
                : "new global::MLIR.Syntax.Token(" + TokenKindExpression + ", " + EmitterHelpers.ToCSharpStringLiteral(TokenText ?? string.Empty) + ")";
        }

        var propertyExpression = typedLocalName + "." + DialectGeneratorNaming.ToPascalCase(SourceName);
        if (Kind == FormatSlotKind.Type)
        {
            return ParameterModel?.CsharpPrinterTemplate != null
                ? ParameterModel.CsharpPrinterTemplate.Render("self", propertyExpression)
                : propertyExpression;
        }

        if (ParameterModel?.CsharpPrinterTemplate != null)
        {
            return ParameterModel.CsharpPrinterTemplate.Render("self", propertyExpression);
        }

        return propertyExpression;
    }
}
