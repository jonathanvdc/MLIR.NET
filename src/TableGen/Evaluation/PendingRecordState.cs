namespace TableGen.Evaluation;

using System.Collections.Generic;
using TableGen.Syntax;

/// <summary>
/// Tracks the unevaluated field state of a record or class instantiation while inheritance and lets are applied.
/// </summary>
internal sealed class PendingRecordState
{
    /// <summary>
    /// Stores one pending state entry per field name.
    /// </summary>
    private readonly Dictionary<string, PendingFieldState> fields = new();

    /// <summary>
    /// Gets the current set of pending field entries.
    /// </summary>
    public IEnumerable<KeyValuePair<string, PendingFieldState>> Fields => fields;

    /// <summary>
    /// Attempts to retrieve the pending state for a field.
    /// </summary>
    /// <param name="name">The field name.</param>
    /// <param name="field">Receives the pending field state when the lookup succeeds.</param>
    /// <returns><see langword="true"/> when the field exists; otherwise <see langword="false"/>.</returns>
    public bool TryGetField(string name, out PendingFieldState field)
    {
        return fields.TryGetValue(name, out field);
    }

    /// <summary>
    /// Imports inherited fields from a base class state.
    /// </summary>
    /// <param name="other">The base class state to merge in.</param>
    public void Import(PendingRecordState other)
    {
        foreach (var pair in other.Fields)
        {
            fields[pair.Key] = pair.Value.Clone(isInherited: true);
        }
    }

    /// <summary>
    /// Declares or re-declares a field inside the current body.
    /// </summary>
    /// <param name="field">The parsed field declaration.</param>
    /// <param name="lexicalScope">The lexical scope visible where the declaration appears.</param>
    public void DefineField(FieldSyntax field, Scope lexicalScope)
    {
        if (fields.TryGetValue(field.Name, out var existingField))
        {
            existingField.DeclaredTypeName = field.TypeName;
            existingField.IsInherited = false;
            if (field.Initializer != null)
            {
                existingField.SetExpression(field.Initializer, lexicalScope);
            }

            return;
        }

        fields[field.Name] = new PendingFieldState(field.TypeName, field.Initializer, lexicalScope, isInherited: false);
    }

    /// <summary>
    /// Applies a body-level <c>let</c> override to an existing field or creates a synthetic pending field for later resolution.
    /// </summary>
    /// <param name="let">The let binding to apply.</param>
    /// <param name="lexicalScope">The lexical scope visible where the let appears.</param>
    public void ApplyLet(LetSyntax let, Scope lexicalScope)
    {
        if (fields.TryGetValue(let.Name, out var existingField))
        {
            existingField.SetExpression(let.Value, lexicalScope);
            return;
        }

        fields[let.Name] = new PendingFieldState(typeName: null, let.Value, lexicalScope);
    }

    /// <summary>
    /// Applies a top-level <c>let ... in</c> override, but only to inherited fields as TableGen does.
    /// </summary>
    /// <param name="let">The let binding to apply.</param>
    /// <param name="lexicalScope">The lexical scope visible where the let appears.</param>
    public void ApplyTopLevelLet(LetSyntax let, Scope lexicalScope)
    {
        if (fields.TryGetValue(let.Name, out var existingField) && existingField.IsInherited)
        {
            existingField.SetExpression(let.Value, lexicalScope);
        }
    }
}

/// <summary>
/// Stores all deferred information needed to resolve one field's final value.
/// </summary>
internal sealed class PendingFieldState
{
    /// <summary>
    /// Initializes a pending field state.
    /// </summary>
    /// <param name="typeName">The declared field type, if known.</param>
    /// <param name="expression">The unevaluated field expression.</param>
    /// <param name="lexicalScope">The lexical scope captured when the field expression was seen.</param>
    /// <param name="isInherited">Indicates whether the field came from a base class.</param>
    public PendingFieldState(string? typeName, ExpressionSyntax? expression, Scope lexicalScope, bool isInherited = false)
    {
        DeclaredTypeName = typeName;
        Expression = expression;
        LexicalScope = lexicalScope;
        IsInherited = isInherited;
    }

    /// <summary>
    /// Gets or sets the declared TableGen type name for the field.
    /// </summary>
    public string? DeclaredTypeName { get; set; }

    /// <summary>
    /// Gets the unevaluated expression that will produce the field's value.
    /// </summary>
    public ExpressionSyntax? Expression { get; private set; }

    /// <summary>
    /// Gets the lexical scope captured when <see cref="Expression"/> was recorded.
    /// </summary>
    public Scope LexicalScope { get; private set; }

    /// <summary>
    /// Gets or sets a value indicating whether the field originated from an inherited base class state.
    /// </summary>
    public bool IsInherited { get; set; }

    /// <summary>
    /// Gets or sets the cached resolved field value once evaluation succeeds.
    /// </summary>
    public Value? ResolvedValue { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the field is currently being resolved.
    /// </summary>
    public bool IsResolving { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether <see cref="ResolvedValue"/> is valid.
    /// </summary>
    public bool HasResolvedValue { get; set; }

    /// <summary>
    /// Gets a value indicating whether this field currently has an expression to evaluate.
    /// </summary>
    public bool HasExpression => Expression != null;

    /// <summary>
    /// Replaces the field expression and resets any cached resolution state.
    /// </summary>
    /// <param name="expression">The new field expression.</param>
    /// <param name="lexicalScope">The lexical scope captured for the new expression.</param>
    public void SetExpression(ExpressionSyntax expression, Scope lexicalScope)
    {
        Expression = expression;
        LexicalScope = lexicalScope;
        ResolvedValue = null;
        HasResolvedValue = false;
        IsResolving = false;
    }

    /// <summary>
    /// Creates a copy of this pending field state for import into another record state.
    /// </summary>
    /// <param name="isInherited">Optionally overrides the inherited flag on the clone.</param>
    /// <returns>A copy of the pending field state.</returns>
    public PendingFieldState Clone(bool? isInherited = null)
    {
        return new PendingFieldState(DeclaredTypeName, Expression, LexicalScope, isInherited ?? IsInherited);
    }
}
