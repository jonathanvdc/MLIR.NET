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
            fields[pair.Key] = pair.Value.Clone();
        }
    }

    public void DefineField(FieldSyntax field, IReadOnlyDictionary<string, Value> lexicalScope)
    {
        if (fields.TryGetValue(field.Name, out var existingField))
        {
            existingField.DeclaredTypeName = field.TypeName;
            if (field.Initializer != null)
            {
                existingField.SetExpression(field.Initializer, lexicalScope);
            }

            return;
        }

        fields[field.Name] = new PendingFieldState(field.TypeName, field.Initializer, lexicalScope);
    }

    public void ApplyLet(LetSyntax let, IReadOnlyDictionary<string, Value> lexicalScope)
    {
        if (fields.TryGetValue(let.Name, out var existingField))
        {
            existingField.SetExpression(let.Value, lexicalScope);
            return;
        }

        fields[let.Name] = new PendingFieldState(typeName: null, let.Value, lexicalScope);
    }
}

internal sealed class PendingFieldState
{
    public PendingFieldState(string? typeName, ExpressionSyntax? expression, IReadOnlyDictionary<string, Value> lexicalScope)
    {
        DeclaredTypeName = typeName;
        Expression = expression;
        LexicalScope = lexicalScope;
    }

    public string? DeclaredTypeName { get; set; }

    public ExpressionSyntax? Expression { get; private set; }

    public IReadOnlyDictionary<string, Value> LexicalScope { get; private set; }

    public Value? ResolvedValue { get; set; }

    public bool IsResolving { get; set; }

    public bool HasResolvedValue { get; set; }

    public bool HasExpression => Expression != null;

    public void SetExpression(ExpressionSyntax expression, IReadOnlyDictionary<string, Value> lexicalScope)
    {
        Expression = expression;
        LexicalScope = lexicalScope;
        ResolvedValue = null;
        HasResolvedValue = false;
        IsResolving = false;
    }

    public PendingFieldState Clone()
    {
        return new PendingFieldState(DeclaredTypeName, Expression, LexicalScope);
    }
}
