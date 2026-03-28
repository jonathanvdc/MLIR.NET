namespace MLIR.Semantics;

using MLIR.Syntax;

/// <summary>
/// Represents a semantic block argument.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="BlockArgument"/> class.
/// </remarks>
/// <param name="syntax">The concrete syntax node for the block argument.</param>
public sealed class BlockArgument(BlockArgumentSyntax syntax)
{
    /// <summary>
    /// Gets the concrete syntax node for the block argument.
    /// </summary>
    public BlockArgumentSyntax Syntax { get; } = syntax;

    /// <summary>
    /// Gets the SSA name of the block argument.
    /// </summary>
    public string Name => Syntax.Name;

    /// <summary>
    /// Gets the semantic SSA value reference for the block argument.
    /// </summary>
    public ValueReference Value { get; } = new ValueReference(syntax.NameToken);

    /// <summary>
    /// Gets the declared type text for the block argument.
    /// </summary>
    public RawSyntaxText Type => Syntax.Type;

    /// <summary>
    /// Gets the source location of the block argument name, if known.
    /// </summary>
    public SourceLocation Location => Value.Location;
}
