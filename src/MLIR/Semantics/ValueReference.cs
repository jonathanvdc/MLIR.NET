namespace MLIR.Semantics;

using MLIR.Syntax;

/// <summary>
/// Represents a typed reference to an SSA value in the semantic layer.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ValueReference"/> struct.
/// </remarks>
/// <param name="token">The syntax token for the SSA value.</param>
public readonly struct ValueReference(SyntaxToken token)
{
    /// <summary>
    /// Gets the syntax token for the SSA value.
    /// </summary>
    public SyntaxToken Token { get; } = token;

    /// <summary>
    /// Gets the SSA value name.
    /// </summary>
    public string Name => Token.Text;

    /// <summary>
    /// Gets the source location of the SSA value, if known.
    /// </summary>
    public SourceLocation Location => SourceLocation.FromToken(Token);
}
