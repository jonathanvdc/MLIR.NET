namespace MLIR.Semantics;

using MLIR.Syntax;

/// <summary>
/// Represents an SSA value definition in the semantic layer.
/// </summary>
public abstract class Value
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Value"/> class.
    /// </summary>
    protected Value(SyntaxToken? token, string name)
    {
        Token = token;
        Name = name;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Value"/> class from a syntax token.
    /// </summary>
    protected Value(SyntaxToken token)
        : this(token, token.Text)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Value"/> class for a synthetic value with no corresponding source token.
    /// </summary>
    protected Value(string name)
        : this(null, name)
    {
    }

    /// <summary>
    /// Gets the syntax token for the SSA value, or null if this is a synthetic value with no corresponding source token.
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
