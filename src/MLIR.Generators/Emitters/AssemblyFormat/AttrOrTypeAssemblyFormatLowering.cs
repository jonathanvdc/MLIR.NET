namespace MLIR.Generators.Emitters.AssemblyFormat;

using System.Collections.Generic;
using MLIR.Generators.Emitters.Common;
using MLIR.ODS.Model;
using MLIR.ODS.Model.AssemblyFormat;
using MLIR.Text;

internal static partial class AssemblyFormatLowerer
{
    private sealed class AttrOrTypeFormatSink : IAssemblyFormatLoweringSink
    {
        private readonly IReadOnlyList<AttrOrTypeParameterModel> parameters;
        private readonly bool includeTrivia;
        private int literalIndex;

        public AttrOrTypeFormatSink(IReadOnlyList<AttrOrTypeParameterModel> parameters, bool includeTrivia)
        {
            this.parameters = parameters;
            this.includeTrivia = includeTrivia;
            Slots = new List<FormatSlot>();
            Elements = new List<LoweredFormatElement>();
        }

        public List<FormatSlot> Slots { get; }
        public List<LoweredFormatElement> Elements { get; }

        public void LowerLiteral(LiteralChunk literal, int elementIndex)
        {
            var start = Slots.Count;
            foreach (var lit in literal.Value)
            {
                switch (lit)
                {
                    case PunctuationLiteral punc:
                        AddLiteralTokenSlot(EmitterHelpers.GetPunctuationText(punc.TokenKind), "TokenKind." + punc.TokenKind, isKeyword: false);
                        break;

                    case KeywordLiteral kw:
                        AddLiteralTokenSlot(kw.Spelling, "TokenKind.Identifier", isKeyword: true);
                        break;

                    case WhitespaceLiteral ws when includeTrivia:
                        Slots.Add(new TriviaSlot { Text = ws.Spaces, IsNewline = false });
                        break;

                    case NewlineLiteral when includeTrivia:
                        Slots.Add(new TriviaSlot { Text = "\n", IsNewline = true });
                        break;
                }
            }

            Elements.Add(new LoweredFormatElement(
                literal,
                elementIndex,
                slotStart: start,
                slotCount: Slots.Count - start,
                isSupported: true));
        }

        public void LowerVariable(VariableChunk variable, int elementIndex)
        {
            var start = Slots.Count;
            var param = FindParameter(parameters, variable.Name);
            Slots.Add(new VariableSlot
            {
                Name = variable.Name,
                SyntaxType = GetResolvedCSharpSyntaxType(param),
                SyntaxShape = GetResolvedCSharpSyntaxShape(param),
                ParamModel = param,
            });
            Elements.Add(new LoweredFormatElement(
                variable,
                elementIndex,
                slotStart: start,
                slotCount: 1,
                isSupported: true));
        }

        public void LowerDirective(DirectiveChunk directive, int elementIndex)
        {
            AddUnsupportedElement(directive, elementIndex);
        }

        public void LowerOptionalGroup(OptionalGroup optionalGroup, int elementIndex)
        {
            AddUnsupportedElement(optionalGroup, elementIndex);
        }

        public void LowerOilist(OilistDirectiveChunk oilist, int elementIndex)
        {
            AddUnsupportedElement(oilist, elementIndex);
        }

        public void LowerUnsupported(Element element, int elementIndex)
        {
            AddUnsupportedElement(element, elementIndex);
        }

        private void AddUnsupportedElement(Element element, int elementIndex)
        {
            Elements.Add(new LoweredFormatElement(
                element,
                elementIndex,
                slotStart: Slots.Count,
                slotCount: 0,
                isSupported: false));
        }

        private void AddLiteralTokenSlot(string syntheticText, string kindExpr, bool isKeyword)
        {
            Slots.Add(new LiteralTokenSlot
            {
                LocalName = "literal" + literalIndex + "Token",
                SyntheticText = syntheticText,
                KindExpr = kindExpr,
                IsKeyword = isKeyword,
            });
            literalIndex++;
        }
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
