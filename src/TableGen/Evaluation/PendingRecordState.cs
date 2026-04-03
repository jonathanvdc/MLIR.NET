namespace TableGen.Evaluation;

using System.Collections.Generic;
using TableGen.Syntax;

internal sealed class PendingRecordState
{
    private readonly Dictionary<string, PendingFieldState> fields = new();

    public IEnumerable<KeyValuePair<string, PendingFieldState>> Fields => fields;

    public bool TryGetField(string name, out PendingFieldState field)
    {
        return fields.TryGetValue(name, out field);
    }

    public void Import(PendingRecordState other)
    {
        foreach (var pair in other.Fields)
        {
            fields[pair.Key] = pair.Value.Clone(isInherited: true);
        }
    }

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

    public void ApplyLet(LetSyntax let, Scope lexicalScope)
    {
        if (fields.TryGetValue(let.Name, out var existingField))
        {
            existingField.SetExpression(let.Value, lexicalScope);
            return;
        }

        fields[let.Name] = new PendingFieldState(typeName: null, let.Value, lexicalScope);
    }

    public void ApplyTopLevelLet(LetSyntax let, Scope lexicalScope)
    {
        if (fields.TryGetValue(let.Name, out var existingField) && existingField.IsInherited)
        {
            existingField.SetExpression(let.Value, lexicalScope);
        }
    }
}

internal sealed class PendingFieldState
{
    public PendingFieldState(string? typeName, ExpressionSyntax? expression, Scope lexicalScope, bool isInherited = false)
    {
        DeclaredTypeName = typeName;
        Expression = expression;
        LexicalScope = lexicalScope;
        IsInherited = isInherited;
    }

    public string? DeclaredTypeName { get; set; }

    public ExpressionSyntax? Expression { get; private set; }

    public Scope LexicalScope { get; private set; }

    public bool IsInherited { get; set; }

    public Value? ResolvedValue { get; set; }

    public bool IsResolving { get; set; }

    public bool HasResolvedValue { get; set; }

    public bool HasExpression => Expression != null;

    public void SetExpression(ExpressionSyntax expression, Scope lexicalScope)
    {
        Expression = expression;
        LexicalScope = lexicalScope;
        ResolvedValue = null;
        HasResolvedValue = false;
        IsResolving = false;
    }

    public PendingFieldState Clone(bool? isInherited = null)
    {
        return new PendingFieldState(DeclaredTypeName, Expression, LexicalScope, isInherited ?? IsInherited);
    }
}
