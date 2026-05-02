namespace MLIR.ODS.Model;

/// <summary>
/// Describes how generated code tests and unwraps an optional public attribute value.
/// </summary>
public enum OptionalValueAccessKind
{
    /// <summary>Use reference-style null checks such as <c>value != null</c>.</summary>
    NullCheck,

    /// <summary>Use nullable value-type access such as <c>value.HasValue</c> and <c>value.Value</c>.</summary>
    NullableValueType,
}
