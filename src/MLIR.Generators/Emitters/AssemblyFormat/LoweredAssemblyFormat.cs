namespace MLIR.Generators.Emitters.AssemblyFormat;

using System.Collections.Generic;
using MLIR.Generators.Emitters.Operation;
using MLIR.ODS.Model.AssemblyFormat;

internal sealed class LoweredAssemblyFormat
{
    public LoweredAssemblyFormat(IReadOnlyList<LoweredFormatElement> elements, IReadOnlyList<AssemblyFormatSyntaxField> fields)
    {
        Elements = elements;
        Fields = fields;
    }

    /// <summary>
    /// Gets the original assembly-format elements after lowering into the common element model.
    /// Non-operation formats currently support only literals and variables, but retaining
    /// unsupported elements here lets emitters make the same whole-format support decision as the
    /// operation path.
    /// </summary>
    public IReadOnlyList<LoweredFormatElement> Elements { get; }

    /// <summary>
    /// Gets the flattened syntax fields used by the generated syntax class, binder, and builder.
    /// </summary>
    public IReadOnlyList<AssemblyFormatSyntaxField> Fields { get; }

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
    /// Gets the contiguous syntax-field range produced by a lowered format element.
    /// </summary>
    public IEnumerable<AssemblyFormatSyntaxField> GetFields(LoweredFormatElement element)
    {
        for (var i = 0; i < element.FieldCount; i++)
        {
            yield return Fields[element.FieldStart + i];
        }
    }
}

internal sealed class LoweredFormatElement
{
    public LoweredFormatElement(
        Element source,
        int elementIndex,
        int fieldStart,
        int fieldCount,
        bool isSupported)
    {
        Source = source;
        ElementIndex = elementIndex;
        FieldStart = fieldStart;
        FieldCount = fieldCount;
        IsSupported = isSupported;
    }

    public Element Source { get; }
    public int ElementIndex { get; }
    public int FieldStart { get; }
    public int FieldCount { get; }
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

    public IReadOnlyList<AssemblyFormatSyntaxField> Fields => Metadata.Fields;

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
