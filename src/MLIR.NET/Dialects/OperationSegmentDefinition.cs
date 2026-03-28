namespace MLIR.Dialects;

/// <summary>
/// Describes a named operation segment such as an operand, result, region, or successor.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="OperationSegmentDefinition"/> class.
/// </remarks>
/// <param name="name">The segment name.</param>
/// <param name="isVariadic">Indicates whether the segment may consume zero or more entries.</param>
public sealed class OperationSegmentDefinition(string name, bool isVariadic = false)
{
    /// <summary>
    /// Gets the segment name.
    /// </summary>
    public string Name { get; } = name;

    /// <summary>
    /// Gets a value indicating whether the segment may consume zero or more entries.
    /// </summary>
    public bool IsVariadic { get; } = isVariadic;
}
