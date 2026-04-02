namespace MLIR.Dialects;

/// <summary>
/// Describes an attribute slot on an operation definition.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="OperationAttributeDefinition"/> class.
/// </remarks>
/// <param name="name">The attribute name.</param>
/// <param name="isRequired">Indicates whether the attribute must be present.</param>
/// <param name="constraintDefinition">The expected attribute constraint definition, if one is known.</param>
public sealed class OperationAttributeDefinition(string name, bool isRequired = true, AttributeConstraintDefinition? constraintDefinition = null)
{
    /// <summary>
    /// Gets the attribute name.
    /// </summary>
    public string Name { get; } = name;

    /// <summary>
    /// Gets a value indicating whether the attribute must be present.
    /// </summary>
    public bool IsRequired { get; } = isRequired;

    /// <summary>
    /// Gets the expected attribute constraint definition, if one is known.
    /// </summary>
    public AttributeConstraintDefinition? ConstraintDefinition { get; } = constraintDefinition;
}
