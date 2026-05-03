namespace MLIR.Generators.Emitters.AssemblyFormat;

using System.Collections.Generic;
using MLIR.ODS.Model;
using MLIR.ODS.Model.AssemblyFormat;

/// <summary>
/// Shared lowering entry points for declarative assembly formats.
/// </summary>
/// <remarks>
/// The lowering layer owns the common walk over ODS assembly-format elements and
/// produces the stable, ordered representation consumed by syntax-class,
/// parse, bind, build, and write emitters. Domain-specific partials translate
/// the walked elements into the attr/type slot model or the operation body
/// syntax metadata model.
/// </remarks>
internal static partial class AssemblyFormatLowerer
{
    public static LoweredAssemblyFormat LowerAttribute(AttributeModel attribute, AssemblyFormatModel format)
    {
        var sink = new AttrOrTypeFormatSink(attribute.Parameters, includeTrivia: true);
        LowerElements(format.Elements, sink);
        return new LoweredAssemblyFormat(sink.Elements, sink.Slots);
    }

    public static LoweredAssemblyFormat LowerType(TypeModel type, AssemblyFormatModel format)
    {
        var sink = new AttrOrTypeFormatSink(type.Parameters, includeTrivia: false);
        LowerElements(format.Elements, sink);
        return new LoweredAssemblyFormat(sink.Elements, sink.Slots);
    }

    public static LoweredOperationAssemblyFormat LowerOperation(OperationModel operation, AssemblyFormatModel format)
    {
        var sink = new OperationFormatSink(operation);
        LowerElements(format.Elements, sink);
        return new LoweredOperationAssemblyFormat(sink.Elements, sink.Metadata);
    }

    private static void LowerElements(IReadOnlyList<Element> elements, IAssemblyFormatLoweringSink sink)
    {
        for (var i = 0; i < elements.Count; i++)
        {
            LowerElement(elements[i], i, sink);
        }
    }

    private static void LowerElement(Element element, int elementIndex, IAssemblyFormatLoweringSink sink)
    {
        switch (element)
        {
            case LiteralChunk literal:
                sink.LowerLiteral(literal, elementIndex);
                break;
            case VariableChunk variable:
                sink.LowerVariable(variable, elementIndex);
                break;
            case OptionalGroup optionalGroup:
                sink.LowerOptionalGroup(optionalGroup, elementIndex);
                break;
            case OilistDirectiveChunk oilist:
                sink.LowerOilist(oilist, elementIndex);
                break;
            case DirectiveChunk directive:
                sink.LowerDirective(directive, elementIndex);
                break;
            default:
                sink.LowerUnsupported(element, elementIndex);
                break;
        }
    }

    private interface IAssemblyFormatLoweringSink
    {
        void LowerLiteral(LiteralChunk literal, int elementIndex);

        void LowerVariable(VariableChunk variable, int elementIndex);

        void LowerDirective(DirectiveChunk directive, int elementIndex);

        void LowerOptionalGroup(OptionalGroup optionalGroup, int elementIndex);

        void LowerOilist(OilistDirectiveChunk oilist, int elementIndex);

        void LowerUnsupported(Element element, int elementIndex);
    }
}
