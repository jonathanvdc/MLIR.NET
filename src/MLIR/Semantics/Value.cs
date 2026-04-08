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
    protected Value(Token? token, string name)
    {
        Token = token;
        Name = name;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Value"/> class from a syntax token.
    /// </summary>
    protected Value(Token token)
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
    public Token? Token { get; }

    /// <summary>
    /// Gets the SSA value name.
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// Gets the source location of the SSA value, if known.
    /// </summary>
    public SourceLocation Location => Token.HasValue ? Token.Value.Location : SourceLocation.Unknown;

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

    /// <summary>
    /// Renames this SSA value, ensuring uniqueness within its owning block when possible.
    /// </summary>
    /// <param name="preferredName">The preferred new SSA name.</param>
    /// <param name="uniquify">Whether to uniquify the name automatically when a conflict exists.</param>
    /// <returns>The final SSA name after any uniquification.</returns>
    public string Rename(string preferredName, bool uniquify = true)
    {
        var ownerBlock = GetOwningBlock();
        if (ownerBlock == null)
        {
            Name = preferredName;
            OnNameChanged();
            return Name;
        }

        var finalName = ownerBlock.AssignValueName(this, preferredName, uniquify);
        OnNameChanged();
        return finalName;
    }

    internal void AddUse(OpOperand operand)
    {
        uses.Add(operand);
    }

    internal void RemoveUse(OpOperand operand)
    {
        uses.Remove(operand);
    }

    internal void SetNameWithoutValidation(string name)
    {
        Name = name;
    }

    /// <summary>
    /// Called after this value's SSA name changes.
    /// </summary>
    protected virtual void OnNameChanged()
    {
    }

    /// <summary>
    /// Gets the block that owns this value when one exists.
    /// </summary>
    protected virtual Block? GetOwningBlock()
    {
        return null;
    }
}
