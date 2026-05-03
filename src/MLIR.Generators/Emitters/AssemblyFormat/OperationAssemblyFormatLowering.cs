namespace MLIR.Generators.Emitters.AssemblyFormat;

using System.Collections.Generic;
using MLIR.Generators.Emitters.Common;
using MLIR.Generators.Emitters.Operation;
using MLIR.ODS.Model;
using MLIR.ODS.Model.AssemblyFormat;

internal static partial class AssemblyFormatLowerer
{
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

    private sealed class OperationFormatSink : IAssemblyFormatLoweringSink
    {
        private readonly OperationModel operation;
        private readonly HashSet<string> usedNames;

        public OperationFormatSink(OperationModel operation)
        {
            this.operation = operation;
            Metadata = new OperationBodySyntaxMetadata(DialectGeneratorNaming.GetOperationClassName(operation));
            Elements = new List<LoweredOperationElement>();
            usedNames = new HashSet<string>(System.StringComparer.Ordinal);
        }

        public OperationBodySyntaxMetadata Metadata { get; }

        public List<LoweredOperationElement> Elements { get; }

        public void LowerLiteral(LiteralChunk literal, int elementIndex)
        {
            LowerSupportedOperationElement(literal, elementIndex);
        }

        public void LowerVariable(VariableChunk variable, int elementIndex)
        {
            LowerSupportedOperationElement(variable, elementIndex);
        }

        public void LowerDirective(DirectiveChunk directive, int elementIndex)
        {
            LowerSupportedOperationElement(directive, elementIndex);
        }

        public void LowerOptionalGroup(OptionalGroup optionalGroup, int elementIndex)
        {
            LowerSupportedOperationElement(optionalGroup, elementIndex);
        }

        public void LowerOilist(OilistDirectiveChunk oilist, int elementIndex)
        {
            LowerSupportedOperationElement(oilist, elementIndex);
        }

        public void LowerUnsupported(Element element, int elementIndex)
        {
            AddElement(element, elementIndex, fieldStart: Metadata.Fields.Count, fieldCount: 0);
        }

        private void LowerSupportedOperationElement(Element element, int elementIndex)
        {
            var start = Metadata.Fields.Count;
            AppendBodySyntaxFields(usedNames, element, operation, Metadata);
            AddElement(element, elementIndex, start, Metadata.Fields.Count - start);
        }

        private void AddElement(Element element, int elementIndex, int fieldStart, int fieldCount)
        {
            var kind = GetOperationElementKind(element);
            Elements.Add(new LoweredOperationElement(
                element,
                kind,
                elementIndex,
                fieldStart,
                fieldCount,
                kind != OperationFormatElementKind.Unsupported));
        }
    }
}
