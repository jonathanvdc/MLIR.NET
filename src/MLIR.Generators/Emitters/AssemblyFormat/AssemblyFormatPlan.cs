namespace MLIR.Generators.Emitters.AssemblyFormat;

using System.Collections.Generic;
using System.Linq;
using MLIR.Generators.Emitters.Common;
using MLIR.ODS.Model;

internal sealed class AssemblyFormatPlan
{
    public AssemblyFormatPlan(FormatSubject subject, IReadOnlyList<FormatNode> nodes, IReadOnlyList<string> unsupportedFeatures)
    {
        Subject = subject;
        Nodes = nodes;
        UnsupportedFeatures = unsupportedFeatures;
    }

    public FormatSubject Subject { get; }
    public IReadOnlyList<FormatNode> Nodes { get; }
    public IEnumerable<FormatSlot> Slots => Nodes.DescendantSlots();
    public IEnumerable<FormatNode> SyntaxNodes => Nodes.DescendantSyntaxNodes();
    public IEnumerable<FormatSlot> SyntaxSlots => Slots.Where(static slot => slot.IsSyntaxSlot);
    public IReadOnlyList<string> UnsupportedFeatures { get; }
    public bool IsSupported => UnsupportedFeatures.Count == 0;
}

internal abstract class FormatNode
{
    public virtual bool IsSyntaxNode => false;
    public virtual string PropertyName => string.Empty;
    public virtual string ParameterName => string.Empty;
    public virtual string CsType => string.Empty;
    public virtual string LocationExpression => PropertyName + ".Location";
    public virtual string RewriteExpression => PropertyName;

    public virtual IEnumerable<FormatSlot> DescendantSlots()
    {
        yield break;
    }

    public virtual IEnumerable<FormatNode> DescendantSyntaxNodes()
    {
        if (IsSyntaxNode)
        {
            yield return this;
        }
    }
}

internal sealed class OptionalGroupNode : FormatNode
{
    public OptionalGroupNode(string name, string syntaxClassName, string anchorName, IReadOnlyList<FormatNode> nodes)
    {
        Name = name;
        SyntaxClassName = syntaxClassName;
        AnchorName = anchorName;
        Nodes = nodes;
        foreach (var slot in nodes.DescendantSlots())
        {
            slot.ContainingOptionalGroup = this;
        }
    }

    public string Name { get; }
    public string SyntaxClassName { get; }
    public string AnchorName { get; }
    public IReadOnlyList<FormatNode> Nodes { get; }
    public FormatSlot? AnchorSlot => Nodes.DescendantSlots().FirstOrDefault(slot => slot.SourceName == AnchorName);
    public override bool IsSyntaxNode => true;
    public override string PropertyName => Name;
    public override string ParameterName => EmitterHelpers.LowerFirst(Name);
    public override string CsType => SyntaxClassName + "?";
    public override string LocationExpression => PropertyName + "?.Location ?? SourceLocation.Unknown";
    public override string RewriteExpression => PropertyName + " is null ? null : (" + SyntaxClassName + ")rewriter.Visit(" + PropertyName + ")";

    public override IEnumerable<FormatSlot> DescendantSlots()
        => Nodes.DescendantSlots();
}

internal static class FormatNodeExtensions
{
    public static IEnumerable<FormatSlot> DescendantSlots(this IEnumerable<FormatNode> nodes)
        => nodes.SelectMany(static node => node.DescendantSlots());

    public static IEnumerable<FormatNode> DescendantSyntaxNodes(this IEnumerable<FormatNode> nodes)
        => nodes.SelectMany(static node => node.DescendantSyntaxNodes());
}

internal sealed class OilistNode : FormatNode
{
    public OilistNode(IReadOnlyList<OptionalGroupNode> clauses)
    {
        Clauses = clauses;
    }

    public IReadOnlyList<OptionalGroupNode> Clauses { get; }

    public override IEnumerable<FormatSlot> DescendantSlots()
        => Clauses.DescendantSlots();

    public override IEnumerable<FormatNode> DescendantSyntaxNodes()
        => Clauses;
}

internal enum FormatSlotKind
{
    LiteralToken,
    Whitespace,
    Newline,
    AttributeValue,
    Type,
    SsaValue,
    SsaValueList,
    AttrDict,
}

internal sealed class FormatSlot : FormatNode
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
        string? triviaText = null,
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
        TriviaText = triviaText;
        IsKeyword = isKeyword;
    }

    public string SourceName { get; }
    public string BaseName { get; }
    public FormatSlotKind Kind { get; }
    public override string CsType { get; }
    public string ParseExpression { get; }
    public AttrOrTypeParameterModel? ParameterModel { get; }
    public string? TokenText { get; }
    public string? TokenKindExpression { get; }
    public string? TriviaText { get; }
    public OptionalGroupNode? ContainingOptionalGroup { get; internal set; }
    public bool IsKeyword { get; }
    public bool IsSyntaxSlot => Kind != FormatSlotKind.Whitespace && Kind != FormatSlotKind.Newline;
    public override bool IsSyntaxNode => IsSyntaxSlot;
    public override string PropertyName => DialectGeneratorNaming.ToPascalCase(BaseName);
    public override string ParameterName => EmitterHelpers.LowerFirst(PropertyName);

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

    public override string RewriteExpression => Kind switch
    {
        FormatSlotKind.LiteralToken => "rewriter.VisitToken(" + PropertyName + ")",
        FormatSlotKind.AttributeValue => "(" + CsType + ")rewriter.Visit(" + PropertyName + ")",
        FormatSlotKind.Type => "(" + CsType + ")rewriter.Visit(" + PropertyName + ")",
        FormatSlotKind.SsaValue => "rewriter.VisitToken(" + PropertyName + ")",
        FormatSlotKind.SsaValueList => "rewriter.VisitSeparatedTokenList(" + PropertyName + ")",
        FormatSlotKind.AttrDict => "rewriter.VisitDelimitedList(" + PropertyName + ")",
        FormatSlotKind.Whitespace => string.Empty,
        FormatSlotKind.Newline => string.Empty,
        _ => PropertyName,
    };

    public override string LocationExpression => PropertyName + ".Location";
    public string BodyAccessExpression => ContainingOptionalGroup == null
        ? "body." + PropertyName
        : "body." + ContainingOptionalGroup.PropertyName + "!." + PropertyName;
    public string OptionalBodyAccessExpression => ContainingOptionalGroup == null
        ? "body." + PropertyName
        : "body." + ContainingOptionalGroup.PropertyName + "?." + PropertyName;

    public override IEnumerable<FormatSlot> DescendantSlots()
    {
        yield return this;
    }

    public static FormatSlot ForLiteral(string name, string text, string tokenKindExpression, bool isKeyword = false)
    {
        var parseExpression = isKeyword
            ? "context.ExpectKeyword(" + EmitterHelpers.ToCSharpStringLiteral(text) + ", " + EmitterHelpers.ToCSharpStringLiteral("Expected '" + text + "'.") + ")"
            : "context.Expect(" + tokenKindExpression + ", " + EmitterHelpers.ToCSharpStringLiteral("Expected '" + text + "'.") + ")";
        return new FormatSlot(name, name, FormatSlotKind.LiteralToken, "global::MLIR.Syntax.Token", parseExpression, tokenText: text, tokenKindExpression: tokenKindExpression, isKeyword: isKeyword);
    }

    public static FormatSlot ForWhitespace(string spaces)
        => new("whitespace", "whitespace", FormatSlotKind.Whitespace, string.Empty, string.Empty, triviaText: spaces);

    public static FormatSlot ForNewline()
        => new("newline", "newline", FormatSlotKind.Newline, string.Empty, string.Empty, triviaText: "\n");

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
        var csType = kind switch
        {
            FormatSlotKind.SsaValue => "global::MLIR.Syntax.Token",
            FormatSlotKind.SsaValueList => "global::MLIR.Syntax.SeparatedSyntaxList<global::MLIR.Syntax.Token>",
            _ => "global::MLIR.Syntax.AttributeValueSyntax",
        };
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
