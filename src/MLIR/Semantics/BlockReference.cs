namespace MLIR.Semantics;

using MLIR.Syntax;

/// <summary>
/// Represents a typed reference to a block label in the semantic layer.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="BlockReference"/> struct.
/// </remarks>
/// <param name="token">The syntax token for the block label.</param>
public readonly struct BlockReference(SyntaxToken token)
{
    /// <summary>
    /// Gets the syntax token for the block label.
    /// </summary>
    public SyntaxToken Token { get; } = token;

    /// <summary>
    /// Gets the block label text.
    /// </summary>
    public string Label => Token.Text;

    /// <summary>
    /// Gets the source location of the block label, if known.
    /// </summary>
    public SourceLocation Location => SourceLocation.FromToken(Token);
}
