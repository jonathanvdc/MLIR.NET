namespace MLIR.Dialects;

/// <summary>
/// Describes a named operation attribute.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="AttributeDefinition"/> class.
/// </remarks>
/// <param name="name">The attribute name.</param>
/// <param name="isRequired">Indicates whether the attribute must be present.</param>
public sealed class AttributeDefinition(string name, bool isRequired = true)
{
    /// <summary>
    /// Gets the attribute name.
    /// </summary>
    public string Name { get; } = name;

    /// <summary>
    /// Gets a value indicating whether the attribute must be present.
    /// </summary>
    public bool IsRequired { get; } = isRequired;
}
