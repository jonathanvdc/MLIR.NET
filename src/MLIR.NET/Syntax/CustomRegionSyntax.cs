namespace MLIR.Syntax;

/// <summary>
/// Represents a preserved nested region in custom operation assembly.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="CustomRegionSyntax"/> class.
/// </remarks>
/// <param name="region">The preserved region.</param>
public sealed class CustomRegionSyntax(RegionSyntax region) : CustomAssemblyItemSyntax
{
    /// <summary>
    /// Gets the preserved region.
    /// </summary>
    public RegionSyntax Region { get; } = region;
}
