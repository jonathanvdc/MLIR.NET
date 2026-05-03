namespace MLIR.Generators.Emitters.AssemblyFormat;

using System.Collections.Generic;
using MLIR.Generators.Emitters.Operation;
using MLIR.ODS.Model.AssemblyFormat;

internal sealed class LoweredAssemblyFormat
{
    public LoweredAssemblyFormat(IReadOnlyList<LoweredFormatElement> elements, IReadOnlyList<FormatSlot> slots)
    {
        Elements = elements;
        Slots = slots;
    }

    /// <summary>
    /// Gets the original assembly-format elements after lowering into the common element model.
    /// Non-operation formats currently support only literals and variables, but retaining
    /// unsupported elements here lets emitters make the same whole-format support decision as the
    /// operation path.
    /// </summary>
    public IReadOnlyList<LoweredFormatElement> Elements { get; }

    /// <summary>
    /// Gets the flattened syntax slots used by the generated syntax class, binder, and builder.
    /// </summary>
    public IReadOnlyList<FormatSlot> Slots { get; }

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

    /// <summary>
    /// Gets the contiguous slot range produced by a lowered format element.
    /// </summary>
    public IEnumerable<FormatSlot> GetSlots(LoweredFormatElement element)
    {
        for (var i = 0; i < element.SlotCount; i++)
        {
            yield return Slots[element.SlotStart + i];
        }
    }
}

internal sealed class LoweredFormatElement
{
    public LoweredFormatElement(
        Element source,
        int elementIndex,
        int slotStart,
        int slotCount,
        bool isSupported)
    {
        Source = source;
        ElementIndex = elementIndex;
        SlotStart = slotStart;
        SlotCount = slotCount;
        IsSupported = isSupported;
    }

    public Element Source { get; }
    public int ElementIndex { get; }
    public int SlotStart { get; }
    public int SlotCount { get; }
    public bool IsSupported { get; }
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
