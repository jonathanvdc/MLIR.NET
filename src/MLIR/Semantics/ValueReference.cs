namespace MLIR.Semantics;

using MLIR.Syntax;

/// <summary>
/// Represents a typed reference to an SSA value in the semantic layer.
/// </summary>
public readonly struct ValueReference
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ValueReference"/> struct from a syntax token.
    /// </summary>
    /// <param name="token">The syntax token for the SSA value.</param>
    public ValueReference(SyntaxToken token)
    {
        Token = token;
        Name = token.Text;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ValueReference"/> struct for a synthetic value with no corresponding source token.
    /// </summary>
    /// <param name="name">The SSA value name.</param>
    public ValueReference(string name)
    {
        Token = null;
        Name = name;
    }

    /// <summary>
    /// Gets the syntax token for the SSA value, or null if this is a synthetic reference with no corresponding source token.
    /// </summary>
    public SyntaxToken? Token { get; }

    /// <summary>
    /// Gets the SSA value name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the source location of the SSA value, if known.
    /// </summary>
    public SourceLocation Location => Token.HasValue ? SourceLocation.FromToken(Token.Value) : SourceLocation.Unknown;
}
