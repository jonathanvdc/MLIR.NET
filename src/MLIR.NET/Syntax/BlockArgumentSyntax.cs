namespace MLIR.Syntax;

/// <summary>
/// Represents a block argument in a region block header.
/// </summary>
public sealed class BlockArgumentSyntax
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BlockArgumentSyntax"/> class.
    /// </summary>
    /// <param name="name">The SSA name of the block argument.</param>
    /// <param name="type">The declared argument type.</param>
    public BlockArgumentSyntax(string name, RawSyntaxText type)
    {
        Name = name;
        Type = type;
    }

    /// <summary>
    /// Gets the SSA name of the block argument.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the declared type text for the block argument.
    /// </summary>
    public RawSyntaxText Type { get; }
}
