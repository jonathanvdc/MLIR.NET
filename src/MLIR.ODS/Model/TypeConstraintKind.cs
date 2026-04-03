namespace MLIR.ODS.Model;

/// <summary>
/// Classifies supported ODS type constraints for generator/runtime interop.
/// </summary>
public enum TypeConstraintKind
{
    /// <summary>
    /// No special builtin/runtime behavior is known.
    /// </summary>
    None,

    /// <summary>
    /// An exact builtin integer type such as <c>I32</c>, <c>SI64</c>, or <c>UI8</c>.
    /// </summary>
    ExactInteger,

    /// <summary>
    /// An exact builtin floating-point type such as <c>F32</c> or <c>BF16</c>.
    /// </summary>
    ExactFloat,

    /// <summary>
    /// The builtin <c>index</c> type.
    /// </summary>
    IndexType,

    /// <summary>
    /// The builtin <c>none</c> type.
    /// </summary>
    NoneType,

    /// <summary>
    /// A tuple type constraint.
    /// </summary>
    TupleType,

    /// <summary>
    /// The builtin function type family.
    /// </summary>
    FunctionType,

    /// <summary>
    /// The builtin tensor type family.
    /// </summary>
    TensorType,

    /// <summary>
    /// The builtin vector type family.
    /// </summary>
    VectorType,

    /// <summary>
    /// The builtin memref type family.
    /// </summary>
    MemRefType,
}
