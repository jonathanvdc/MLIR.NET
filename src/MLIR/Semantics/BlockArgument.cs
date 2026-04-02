namespace MLIR.Semantics;

using MLIR.Syntax;

/// <summary>
/// Represents a semantic block argument.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="BlockArgument"/> class.
/// </remarks>
/// <param name="syntax">The concrete syntax node for the block argument.</param>
/// <param name="typeReference">The semantic type reference for the argument type.</param>
public sealed class BlockArgument(BlockArgumentSyntax syntax, TypeReference typeReference)
    : Value(syntax.NameToken)
{
    /// <summary>
    /// Gets the concrete syntax node for the block argument.
    /// </summary>
    public BlockArgumentSyntax Syntax { get; } = syntax;

    /// <summary>
    /// Gets the declared type text for the block argument.
    /// </summary>
    public RawSyntaxText Type => Syntax.RawType;

    /// <summary>
    /// Gets the semantic type reference for the block argument.
    /// </summary>
    public TypeReference TypeReference { get; } = typeReference;

    /// <summary>
    /// Gets the source location of the block argument name, if known.
    /// </summary>
    public new SourceLocation Location => base.Location;
}
