namespace MLIR.ODS.Model;

/// <summary>
/// Represents a type constraint description extracted from ODS.
/// </summary>
public sealed class TypeConstraintModel(
    string name,
    string recordName,
    TypeConstraintKind kind = TypeConstraintKind.None,
    string? canonicalTypeName = null)
{
    /// <summary>
    /// Gets the logical constraint name.
    /// </summary>
    public string Name { get; } = name;

    /// <summary>
    /// Gets the originating ODS record name.
    /// </summary>
    public string RecordName { get; } = recordName;

    /// <summary>
    /// Gets the supported builtin/runtime behavior for the constraint.
    /// </summary>
    public TypeConstraintKind Kind { get; } = kind;

    /// <summary>
    /// Gets the canonical syntax name used to bind this constraint from standalone type text,
    /// when the constraint is self-identifying.
    /// </summary>
    public string? CanonicalTypeName { get; } = canonicalTypeName;
}
