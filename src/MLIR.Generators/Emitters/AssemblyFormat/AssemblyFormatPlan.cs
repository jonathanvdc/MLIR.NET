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
    public IEnumerable<FormatSlot> SyntaxSlots => Slots;
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
    public virtual string CanStartExpression => "false";
    public virtual string? LiteralTextForSpacing => null;

    public abstract void Accept(IFormatNodeVisitor visitor);

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

internal interface IFormatNodeVisitor
{
    void VisitTrivia(TriviaNode trivia);

    void VisitLiteralToken(LiteralTokenSlot slot);

    void VisitAttributeValue(AttributeValueSlot slot);

    void VisitType(TypeSlot slot);

    void VisitTypeList(TypeListSlot slot);

    void VisitSsaValue(SsaValueSlot slot);

    void VisitSsaValueList(SsaValueListSlot slot);

    void VisitAttrDict(AttrDictSlot slot);

    void VisitAttrDictWithKeyword(AttrDictWithKeywordSlot slot);

    void VisitRegion(RegionSlot slot);

    void VisitOptionalSyntax(OptionalSyntaxNode optionalSyntax);

    void VisitOilist(OilistNode oilist);
}

internal abstract class OptionalSyntaxNode : FormatNode
{
    protected OptionalSyntaxNode(string name, string syntaxClassName, string anchorName, IReadOnlyList<FormatNode> nodes)
    {
        Name = name;
        SyntaxClassName = syntaxClassName;
        AnchorName = anchorName;
        Nodes = nodes;
        foreach (var slot in nodes.DescendantSlots())
        {
            slot.ContainingOptionalSyntax = this;
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
    public override string CanStartExpression => Nodes.FirstOrDefault(static node => node.IsSyntaxNode)?.CanStartExpression ?? "false";

    public override void Accept(IFormatNodeVisitor visitor)
        => visitor.VisitOptionalSyntax(this);

    public override IEnumerable<FormatSlot> DescendantSlots()
        => Nodes.DescendantSlots();
}

internal sealed class OptionalGroupNode : OptionalSyntaxNode
{
    public OptionalGroupNode(string name, string syntaxClassName, string anchorName, IReadOnlyList<FormatNode> nodes)
        : base(name, syntaxClassName, anchorName, nodes)
    {
    }
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

    public override void Accept(IFormatNodeVisitor visitor)
        => visitor.VisitOilist(this);
}

internal sealed class TriviaNode : FormatNode
{
    public TriviaNode(string text)
    {
        Text = text;
    }

    public string Text { get; }

    public override void Accept(IFormatNodeVisitor visitor)
        => visitor.VisitTrivia(this);
}

internal abstract class FormatSlot : FormatNode
{
    protected FormatSlot(string sourceName, string baseName, string csType, string parseExpression)
    {
        SourceName = sourceName;
        BaseName = baseName;
        CsType = csType;
        ParseExpression = parseExpression;
    }

    public string SourceName { get; }
    public string BaseName { get; }
    public override string CsType { get; }
    public string ParseExpression { get; }
    public OptionalSyntaxNode? ContainingOptionalSyntax { get; internal set; }
    public override bool IsSyntaxNode => true;
    public override string PropertyName => DialectGeneratorNaming.ToPascalCase(BaseName);
    public override string ParameterName => EmitterHelpers.LowerFirst(PropertyName);

    public virtual string ParseValueExpression => ParameterName + "Result.Value";

    public override string LocationExpression => PropertyName + ".Location";
    public string BodyAccessExpression => ContainingOptionalSyntax == null
        ? "body." + PropertyName
        : "body." + ContainingOptionalSyntax.PropertyName + "!." + PropertyName;
    public string OptionalBodyAccessExpression => ContainingOptionalSyntax == null
        ? "body." + PropertyName
        : "body." + ContainingOptionalSyntax.PropertyName + "?." + PropertyName;

    public override IEnumerable<FormatSlot> DescendantSlots()
    {
        yield return this;
    }

    public abstract string BuildExpression(string typedLocalName);

    public static LiteralTokenSlot ForLiteral(string name, string text, string tokenKindExpression, bool isKeyword = false)
        => new(name, text, tokenKindExpression, isKeyword);

    public static TriviaNode ForWhitespace(string spaces)
        => new(spaces);

    public static TriviaNode ForNewline()
        => new("\n");

    public static FormatSlot ForParameter(string name, int ordinal, AttrOrTypeParameterModel parameter)
    {
        var syntaxType = !string.IsNullOrEmpty(parameter.CsharpSyntaxType)
            ? parameter.CsharpSyntaxType!
            : "global::MLIR.Syntax.AttributeValueSyntax";
        if (syntaxType == "TypeSyntax" || syntaxType == "global::MLIR.Syntax.TypeSyntax")
        {
            return new TypeSlot(name, name, "context.TryParseTypeSyntax()", parameter);
        }

        var parseExpression = parameter.CsharpParserTemplate != null
            ? parameter.CsharpParserTemplate.Render("parser", "context")
            : "context.TryParseAttributeValueSyntax()";
        return new AttributeValueSlot(name, name, syntaxType, parseExpression, parameter);
    }

    public static FormatSlot ForOperationVariable(string name, int ordinal, OperationVariableSlotKind kind, string parseExpression)
    {
        return kind switch
        {
            OperationVariableSlotKind.SsaValue => new SsaValueSlot(name, name, parseExpression),
            OperationVariableSlotKind.SsaValueList => new SsaValueListSlot(name, name, parseExpression),
            OperationVariableSlotKind.AttributeValue => new AttributeValueSlot(name, name, "global::MLIR.Syntax.AttributeValueSyntax", parseExpression, null),
            OperationVariableSlotKind.Region => new RegionSlot(name, name, parseExpression),
            _ => throw new System.ArgumentOutOfRangeException(nameof(kind)),
        };
    }

    public static AttrDictSlot ForAttrDictDirective(string name, int ordinal, string parseExpression, string csType)
        => new(name, name, parseExpression, csType);

    public static AttrDictWithKeywordSlot ForAttrDictWithKeywordDirective(string name, int ordinal)
        => new(name, name);

    public static TypeSlot ForTypeDirective(string name, int ordinal, string parseExpression)
        => new(name, name, parseExpression, null);

    public static TypeListSlot ForTypeListDirective(string name, int ordinal, string parseExpression)
        => new(name, name, parseExpression);
}

internal enum OperationVariableSlotKind
{
    AttributeValue,
    Region,
    SsaValue,
    SsaValueList,
}

internal sealed class LiteralTokenSlot : FormatSlot
{
    public LiteralTokenSlot(string name, string text, string tokenKindExpression, bool isKeyword)
        : base(name, name, "global::MLIR.Syntax.Token", isKeyword
            ? "context.ExpectKeyword(" + EmitterHelpers.ToCSharpStringLiteral(text) + ", " + EmitterHelpers.ToCSharpStringLiteral("Expected '" + text + "'.") + ")"
            : "context.Expect(" + tokenKindExpression + ", " + EmitterHelpers.ToCSharpStringLiteral("Expected '" + text + "'.") + ")")
    {
        Text = text;
        TokenKindExpression = tokenKindExpression;
        IsKeyword = isKeyword;
    }

    public string Text { get; }
    public string TokenKindExpression { get; }
    public bool IsKeyword { get; }
    public override string RewriteExpression => "rewriter.VisitToken(" + PropertyName + ")";
    public override string CanStartExpression => IsKeyword
        ? "context.IsKeyword(" + EmitterHelpers.ToCSharpStringLiteral(Text) + ")"
        : "context.Is(" + TokenKindExpression + ")";
    public override string? LiteralTextForSpacing => Text;

    public override void Accept(IFormatNodeVisitor visitor)
        => visitor.VisitLiteralToken(this);

    public override string BuildExpression(string typedLocalName)
        => IsKeyword
            ? "global::MLIR.Syntax.TokenFactory.Identifier(" + EmitterHelpers.ToCSharpStringLiteral(Text) + ")"
            : "new global::MLIR.Syntax.Token(" + TokenKindExpression + ", " + EmitterHelpers.ToCSharpStringLiteral(Text) + ")";
}

internal sealed class AttributeValueSlot : FormatSlot
{
    public AttributeValueSlot(string sourceName, string baseName, string csType, string parseExpression, AttrOrTypeParameterModel? parameterModel)
        : base(sourceName, baseName, csType, parseExpression)
    {
        ParameterModel = parameterModel;
    }

    public AttrOrTypeParameterModel? ParameterModel { get; }
    public override string ParseValueExpression => "(" + CsType + ")" + base.ParseValueExpression;
    public override string RewriteExpression => "(" + CsType + ")rewriter.Visit(" + PropertyName + ")";

    public override void Accept(IFormatNodeVisitor visitor)
        => visitor.VisitAttributeValue(this);

    public override string BuildExpression(string typedLocalName)
    {
        var propertyExpression = typedLocalName + "." + DialectGeneratorNaming.ToPascalCase(SourceName);
        return ParameterModel?.CsharpPrinterTemplate != null
            ? ParameterModel.CsharpPrinterTemplate.Render("self", propertyExpression)
            : propertyExpression;
    }
}

internal sealed class TypeSlot : FormatSlot
{
    public TypeSlot(string sourceName, string baseName, string parseExpression, AttrOrTypeParameterModel? parameterModel)
        : base(sourceName, baseName, "global::MLIR.Syntax.TypeSyntax", parseExpression)
    {
        ParameterModel = parameterModel;
    }

    public AttrOrTypeParameterModel? ParameterModel { get; }
    public override string ParseValueExpression => "(" + CsType + ")" + base.ParseValueExpression;
    public override string RewriteExpression => "(" + CsType + ")rewriter.Visit(" + PropertyName + ")";

    public override void Accept(IFormatNodeVisitor visitor)
        => visitor.VisitType(this);

    public override string BuildExpression(string typedLocalName)
    {
        var propertyExpression = typedLocalName + "." + DialectGeneratorNaming.ToPascalCase(SourceName);
        return ParameterModel?.CsharpPrinterTemplate != null
            ? ParameterModel.CsharpPrinterTemplate.Render("self", propertyExpression)
            : propertyExpression;
    }
}

internal sealed class TypeListSlot : FormatSlot
{
    public TypeListSlot(string sourceName, string baseName, string parseExpression)
        : base(sourceName, baseName, "global::MLIR.Syntax.SeparatedSyntaxList<global::MLIR.Syntax.TypeSyntax>", parseExpression)
    {
    }

    public override string RewriteExpression => "rewriter.VisitSeparatedList(" + PropertyName + ")";

    public override void Accept(IFormatNodeVisitor visitor)
        => visitor.VisitTypeList(this);

    public override string BuildExpression(string typedLocalName)
        => typedLocalName + "." + DialectGeneratorNaming.ToPascalCase(SourceName);
}

internal sealed class SsaValueSlot : FormatSlot
{
    public SsaValueSlot(string sourceName, string baseName, string parseExpression)
        : base(sourceName, baseName, "global::MLIR.Syntax.Token", parseExpression)
    {
    }

    public override string RewriteExpression => "rewriter.VisitToken(" + PropertyName + ")";
    public override string CanStartExpression => "context.Is(global::MLIR.Text.TokenKind.SsaName)";

    public override void Accept(IFormatNodeVisitor visitor)
        => visitor.VisitSsaValue(this);

    public override string BuildExpression(string typedLocalName)
        => typedLocalName + "." + DialectGeneratorNaming.ToPascalCase(SourceName);
}

internal sealed class SsaValueListSlot : FormatSlot
{
    public SsaValueListSlot(string sourceName, string baseName, string parseExpression)
        : base(sourceName, baseName, "global::MLIR.Syntax.SeparatedSyntaxList<global::MLIR.Syntax.Token>", parseExpression)
    {
    }

    public override string RewriteExpression => "rewriter.VisitSeparatedTokenList(" + PropertyName + ")";
    public override string CanStartExpression => "context.Is(global::MLIR.Text.TokenKind.SsaName)";

    public override void Accept(IFormatNodeVisitor visitor)
        => visitor.VisitSsaValueList(this);

    public override string BuildExpression(string typedLocalName)
        => typedLocalName + "." + DialectGeneratorNaming.ToPascalCase(SourceName);
}

internal sealed class AttrDictSlot : FormatSlot
{
    public AttrDictSlot(string sourceName, string baseName, string parseExpression, string csType)
        : base(sourceName, baseName, csType, parseExpression)
    {
    }

    public override string RewriteExpression => "rewriter.VisitDelimitedList(" + PropertyName + ")";

    public override void Accept(IFormatNodeVisitor visitor)
        => visitor.VisitAttrDict(this);

    public override string BuildExpression(string typedLocalName)
        => typedLocalName + "." + DialectGeneratorNaming.ToPascalCase(SourceName);
}

internal sealed class AttrDictWithKeywordSlot : FormatSlot
{
    public AttrDictWithKeywordSlot(string sourceName, string baseName)
        : base(
            sourceName,
            baseName,
            "global::MLIR.Syntax.KeywordedAttributeDictionarySyntax",
            "context.TryParseKeywordedAttrDictSyntax()")
    {
    }

    public override string RewriteExpression => "(global::MLIR.Syntax.KeywordedAttributeDictionarySyntax)rewriter.Visit(" + PropertyName + ")";

    public override string CanStartExpression => "context.IsKeyword(\"attributes\")";

    public override void Accept(IFormatNodeVisitor visitor)
        => visitor.VisitAttrDictWithKeyword(this);

    public override string BuildExpression(string typedLocalName)
        => typedLocalName + "." + DialectGeneratorNaming.ToPascalCase(SourceName);
}

internal sealed class RegionSlot : FormatSlot
{
    public RegionSlot(string sourceName, string baseName, string parseExpression)
        : base(sourceName, baseName, "global::MLIR.Syntax.RegionSyntax", parseExpression)
    {
    }

    public override string RewriteExpression => "(global::MLIR.Syntax.RegionSyntax)rewriter.Visit(" + PropertyName + ")";

    public override string CanStartExpression => "context.Is(global::MLIR.Text.TokenKind.LBrace)";

    public override void Accept(IFormatNodeVisitor visitor)
        => visitor.VisitRegion(this);

    public override string BuildExpression(string typedLocalName)
        => typedLocalName + "." + DialectGeneratorNaming.ToPascalCase(SourceName);
}
