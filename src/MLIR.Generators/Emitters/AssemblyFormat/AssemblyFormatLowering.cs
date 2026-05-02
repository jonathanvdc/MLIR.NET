namespace MLIR.Generators.Emitters.AssemblyFormat;

using System.Collections.Generic;
using MLIR.Generators.Emitters.Common;
using MLIR.Generators.Emitters.Operation;
using MLIR.ODS.Model;
using MLIR.ODS.Model.AssemblyFormat;
using MLIR.Text;

/// <summary>
/// Shared lowering entry points for declarative assembly formats.
/// </summary>
/// <remarks>
/// The lowering layer owns the common walk over ODS assembly-format elements and
/// produces the stable, ordered representation consumed by syntax-class,
/// parse, bind, build, and write emitters. Domain-specific callers still decide
/// how to emit final C# statements from the lowered slots, but they no longer
/// rediscover the format shape independently.
/// </remarks>
internal static class AssemblyFormatLowerer
{
    public static LoweredAssemblyFormat LowerAttribute(AttributeModel attribute, AssemblyFormatModel format)
    {
        var slots = LowerAttrOrTypeSlots(
            format,
            includeTrivia: true,
            variable =>
            {
                var param = FindParameter(attribute.Parameters, variable.Name);
                return new VariableSlot
                {
                    Name = variable.Name,
                    SyntaxType = GetResolvedCSharpSyntaxType(param),
                    SyntaxShape = GetResolvedCSharpSyntaxShape(param),
                    ParamModel = param,
                };
            });

        return new LoweredAssemblyFormat(slots);
    }

    public static LoweredAssemblyFormat LowerType(TypeModel type, AssemblyFormatModel format)
    {
        var slots = LowerAttrOrTypeSlots(
            format,
            includeTrivia: false,
            variable =>
            {
                var param = FindParameter(type.Parameters, variable.Name);
                return new VariableSlot
                {
                    Name = variable.Name,
                    SyntaxType = GetResolvedCSharpSyntaxType(param),
                    SyntaxShape = GetResolvedCSharpSyntaxShape(param),
                    ParamModel = param,
                };
            });

        return new LoweredAssemblyFormat(slots);
    }

    public static LoweredOperationAssemblyFormat LowerOperation(OperationModel operation, AssemblyFormatModel format)
    {
        var metadata = new OperationBodySyntaxMetadata(DialectGeneratorNaming.GetOperationClassName(operation));
        var usedNames = new HashSet<string>(System.StringComparer.Ordinal);
        var elements = new List<LoweredOperationElement>();
        LowerOperationElements(format.Elements, operation, metadata, usedNames, elements);
        return new LoweredOperationAssemblyFormat(elements, metadata);
    }

    private static IReadOnlyList<FormatSlot> LowerAttrOrTypeSlots(
        AssemblyFormatModel format,
        bool includeTrivia,
        System.Func<VariableChunk, VariableSlot> lowerVariable)
    {
        var slots = new List<FormatSlot>();
        var literalIndex = 0;

        AssemblyFormatTraversal.VisitElements(
            format.Elements,
            onLiteral: literal =>
            {
                foreach (var lit in literal.Value)
                {
                    switch (lit)
                    {
                        case PunctuationLiteral punc:
                            slots.Add(new LiteralTokenSlot
                            {
                                LocalName = "literal" + literalIndex + "Token",
                                SyntheticText = EmitterHelpers.GetPunctuationText(punc.TokenKind),
                                KindExpr = "TokenKind." + punc.TokenKind,
                                IsKeyword = false,
                            });
                            literalIndex++;
                            break;

                        case KeywordLiteral kw:
                            slots.Add(new LiteralTokenSlot
                            {
                                LocalName = "literal" + literalIndex + "Token",
                                SyntheticText = kw.Spelling,
                                KindExpr = "TokenKind.Identifier",
                                IsKeyword = true,
                            });
                            literalIndex++;
                            break;

                        case WhitespaceLiteral ws when includeTrivia:
                            slots.Add(new TriviaSlot { Text = ws.Spaces, IsNewline = false });
                            break;

                        case NewlineLiteral when includeTrivia:
                            slots.Add(new TriviaSlot { Text = "\n", IsNewline = true });
                            break;
                    }
                }
            },
            onVariable: variable => slots.Add(lowerVariable(variable)));

        return slots;
    }

    private static void LowerOperationElements(
        IReadOnlyList<Element> elements,
        OperationModel operation,
        OperationBodySyntaxMetadata metadata,
        HashSet<string> usedNames,
        List<LoweredOperationElement> loweredElements)
    {
        for (var i = 0; i < elements.Count; i++)
        {
            var start = metadata.Fields.Count;
            EmitterHelpers.AppendBodySyntaxFields(usedNames, elements[i], operation, metadata);
            var count = metadata.Fields.Count - start;
            var kind = GetOperationElementKind(elements[i]);
            loweredElements.Add(new LoweredOperationElement(
                elements[i],
                kind,
                i,
                start,
                count,
                kind != OperationFormatElementKind.Unsupported));
        }
    }

    public static OperationFormatElementKind GetOperationElementKind(Element element)
    {
        return element switch
        {
            LiteralChunk _ => OperationFormatElementKind.Literal,
            VariableChunk _ => OperationFormatElementKind.Variable,
            AttrDictDirectiveChunk _ => OperationFormatElementKind.AttrDict,
            AttrDictWithKeywordDirectiveChunk _ => OperationFormatElementKind.AttrDictWithKeyword,
            PropDictDirectiveChunk _ => OperationFormatElementKind.PropDict,
            TypeDirectiveChunk _ => OperationFormatElementKind.Type,
            QualifiedDirectiveChunk _ => OperationFormatElementKind.QualifiedType,
            ResultsDirectiveChunk _ => OperationFormatElementKind.ResultsType,
            FunctionalTypeDirectiveChunk _ => OperationFormatElementKind.FunctionalType,
            RegionsDirectiveChunk _ => OperationFormatElementKind.Regions,
            SuccessorsDirectiveChunk _ => OperationFormatElementKind.Successors,
            OperandsDirectiveChunk _ => OperationFormatElementKind.Operands,
            OptionalGroup _ => OperationFormatElementKind.OptionalGroup,
            OilistDirectiveChunk _ => OperationFormatElementKind.Oilist,
            _ => OperationFormatElementKind.Unsupported,
        };
    }

    private static AttrOrTypeParameterModel? FindParameter(
        IReadOnlyList<AttrOrTypeParameterModel> parameters,
        string variableName)
    {
        foreach (var param in parameters)
        {
            if (string.Equals(param.Name, variableName, System.StringComparison.Ordinal))
            {
                return param;
            }
        }

        return null;
    }

    public static string GetResolvedCSharpType(AttrOrTypeParameterModel? param)
    {
        if (param == null)
        {
            return "AttributeValueSyntax";
        }

        if (!string.IsNullOrEmpty(param.CsharpType))
        {
            return param.CsharpType!;
        }

        return "AttributeValueSyntax";
    }

    private static string GetResolvedCSharpSyntaxType(AttrOrTypeParameterModel? param)
    {
        if (!string.IsNullOrEmpty(param?.CsharpSyntaxType))
        {
            return param!.CsharpSyntaxType!;
        }

        return "AttributeValueSyntax";
    }

    private static SyntaxValueShape GetResolvedCSharpSyntaxShape(AttrOrTypeParameterModel? param)
    {
        return param?.CsharpSyntaxShape ?? SyntaxValueShape.SyntaxNode;
    }
}

internal sealed class LoweredAssemblyFormat
{
    public LoweredAssemblyFormat(IReadOnlyList<FormatSlot> slots)
    {
        Slots = slots;
    }

    public IReadOnlyList<FormatSlot> Slots { get; }
}

internal sealed class LoweredOperationAssemblyFormat
{
    public LoweredOperationAssemblyFormat(
        IReadOnlyList<LoweredOperationElement> elements,
        OperationBodySyntaxMetadata metadata)
    {
        Elements = elements;
        Metadata = metadata;
    }

    public IReadOnlyList<LoweredOperationElement> Elements { get; }

    public OperationBodySyntaxMetadata Metadata { get; }

    public bool IsSupported
    {
        get
        {
            foreach (var element in Elements)
            {
                if (!element.IsSupported)
                {
                    return false;
                }
            }

            return true;
        }
    }
}

internal enum OperationFormatElementKind
{
    Unsupported,
    Literal,
    Variable,
    AttrDict,
    AttrDictWithKeyword,
    PropDict,
    Type,
    QualifiedType,
    ResultsType,
    FunctionalType,
    Regions,
    Successors,
    Operands,
    OptionalGroup,
    Oilist,
}

internal sealed class LoweredOperationElement
{
    public LoweredOperationElement(
        Element source,
        OperationFormatElementKind kind,
        int siblingIndex,
        int fieldStart,
        int fieldCount,
        bool isSupported)
    {
        Source = source;
        Kind = kind;
        SiblingIndex = siblingIndex;
        FieldStart = fieldStart;
        FieldCount = fieldCount;
        IsSupported = isSupported;
    }

    public Element Source { get; }

    public OperationFormatElementKind Kind { get; }

    public int SiblingIndex { get; }

    public int FieldStart { get; }

    public int FieldCount { get; }

    public bool IsSupported { get; }
}

internal abstract class FormatSlot
{
}

internal sealed class LiteralTokenSlot : FormatSlot
{
    public string LocalName { get; set; } = string.Empty;

    public string SyntheticText { get; set; } = string.Empty;

    public string KindExpr { get; set; } = string.Empty;

    public bool IsKeyword { get; set; }
}

internal sealed class VariableSlot : FormatSlot
{
    public string Name { get; set; } = string.Empty;

    public string SyntaxType { get; set; } = "AttributeValueSyntax";

    public SyntaxValueShape SyntaxShape { get; set; } = SyntaxValueShape.SyntaxNode;

    public AttrOrTypeParameterModel? ParamModel { get; set; }
}

internal sealed class TriviaSlot : FormatSlot
{
    public string Text { get; set; } = string.Empty;

    public bool IsNewline { get; set; }
}
