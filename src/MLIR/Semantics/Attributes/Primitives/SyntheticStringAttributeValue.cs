namespace MLIR.Semantics.Attributes.Primitives;

using MLIR.Dialects;
using MLIR.Semantics;

/// <summary>
/// Represents a string attribute value constructed programmatically rather than parsed from source text.
/// Used for setting string-valued attributes (such as <c>sym_name</c>) on operations at runtime.
/// </summary>
public sealed class SyntheticStringAttributeValue : StringAttributeValue
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SyntheticStringAttributeValue"/> class
    /// with the given string value.
    /// </summary>
    /// <param name="value">The string value.</param>
    public SyntheticStringAttributeValue(string value)
        : base(value)
    {
    }

    /// <inheritdoc/>
    public override string? Name => null;

    /// <inheritdoc/>
    public override AttributeConstraintDefinition? Definition => null;
}
