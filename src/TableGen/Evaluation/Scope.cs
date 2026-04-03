namespace TableGen.Evaluation;

using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Represents a layered lexical scope with cheap binding extension.
/// </summary>
internal sealed class Scope : IReadOnlyDictionary<string, Value>
{
    private static readonly Scope empty = new(parent: null, name: null, value: null);

    private readonly Scope? parent;
    private readonly string? name;
    private readonly Value? value;

    private Scope(Scope? parent, string? name, Value? value)
    {
        this.parent = parent;
        this.name = name;
        this.value = value;
    }

    public static Scope Empty => empty;

    public IEnumerable<string> Keys => Materialize().Keys;

    public IEnumerable<Value> Values => Materialize().Values;

    public int Count => Materialize().Count;

    public Value this[string key]
    {
        get
        {
            if (TryGetValue(key, out var result))
            {
                return result;
            }

            throw new KeyNotFoundException($"The given key '{key}' was not present in the scope.");
        }
    }

    public Scope With(string bindingName, Value bindingValue)
    {
        return new Scope(this, bindingName, bindingValue);
    }

    public bool ContainsKey(string key)
    {
        return TryGetValue(key, out _);
    }

    public bool TryGetValue(string key, out Value result)
    {
        for (var current = this; current != null; current = current.parent)
        {
            if (current.name == key)
            {
                result = current.value!;
                return true;
            }
        }

        result = null!;
        return false;
    }

    public IEnumerator<KeyValuePair<string, Value>> GetEnumerator()
    {
        return Materialize().GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    private Dictionary<string, Value> Materialize()
    {
        var materialized = new Dictionary<string, Value>();
        var stack = new Stack<Scope>();
        for (var current = this; current != null; current = current.parent)
        {
            if (current.name != null)
            {
                stack.Push(current);
            }
        }

        while (stack.Count > 0)
        {
            var scope = stack.Pop();
            materialized[scope.name!] = scope.value!;
        }

        return materialized;
    }
}
