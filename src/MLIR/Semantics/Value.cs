namespace MLIR.Semantics;

using System.Collections.Generic;
using MLIR.Syntax;

/// <summary>
/// Represents an SSA value definition in the semantic layer.
/// </summary>
public abstract class Value
{
    private readonly List<OpOperand> uses = [];

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

    /// <summary>
    /// Gets the uses of this SSA value.
    /// </summary>
    public IReadOnlyList<OpOperand> Uses => uses;

    /// <summary>
    /// Replaces every use of this value with <paramref name="other"/>.
    /// </summary>
    public void ReplaceAllUsesWith(Value other)
    {
        if (ReferenceEquals(this, other))
        {
            return;
        }

        var existingUses = uses.ToArray();
        foreach (var use in existingUses)
        {
            use.Value = other;
        }
    }

    internal void AddUse(OpOperand operand)
    {
        uses.Add(operand);
    }

    internal void RemoveUse(OpOperand operand)
    {
        uses.Remove(operand);
    }
}
