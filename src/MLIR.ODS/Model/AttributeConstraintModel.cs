namespace MLIR.ODS.Model;

/// <summary>
/// Represents an attribute constraint description extracted from ODS.
/// </summary>
public sealed class AttributeConstraintModel(string name, string recordName, AttributeConstraintKind kind = AttributeConstraintKind.None)
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
    /// Gets the supported parser/binder behavior for the constraint.
    /// </summary>
    public AttributeConstraintKind Kind { get; } = kind;
}
