namespace MLIR.Semantics;

using MLIR.Syntax;

/// <summary>
/// Represents a typed reference to a block label in the semantic layer.
/// </summary>
public readonly struct BlockReference
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BlockReference"/> struct from a syntax token.
    /// </summary>
    /// <param name="token">The syntax token for the block label.</param>
    public BlockReference(SyntaxToken token)
    {
        Token = token;
        Label = token.Text;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BlockReference"/> struct from a label text.
    /// </summary>
    /// <param name="label">The block label text.</param>
    public BlockReference(string label)
    {
        Token = null;
        Label = label;
    }

    /// <summary>
    /// Gets the syntax token for the block label.
    /// </summary>
    public SyntaxToken? Token { get; }

    /// <summary>
    /// Gets the block label text.
    /// </summary>
    public string Label { get; }

    /// <summary>
    /// Gets the source location of the block label, if known.
    /// </summary>
    public SourceLocation Location => Token.HasValue ? SourceLocation.FromToken(Token.Value) : SourceLocation.Unknown;
}
