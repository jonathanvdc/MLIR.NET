namespace TableGen.Evaluation;

using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Represents a layered lexical scope with cheap binding extension.
/// </summary>
internal sealed class Scope : IReadOnlyDictionary<string, Value>
{
    /// <summary>
    /// Represents the singleton empty scope.
    /// </summary>
    private static readonly Scope empty = new(parent: null, name: null, value: null);

    /// <summary>
    /// Points to the next outer scope frame.
    /// </summary>
    private readonly Scope? parent;

    /// <summary>
    /// Stores the binding introduced by this frame, if any.
    /// </summary>
    private readonly string? name;

    /// <summary>
    /// Stores the value paired with <see cref="name"/> for this frame.
    /// </summary>
    private readonly Value? value;

    /// <summary>
    /// Initializes a new scope frame.
    /// </summary>
    /// <param name="parent">The enclosing scope frame.</param>
    /// <param name="name">The name introduced by this frame.</param>
    /// <param name="value">The bound value introduced by this frame.</param>
    private Scope(Scope? parent, string? name, Value? value)
    {
        this.parent = parent;
        this.name = name;
        this.value = value;
    }

    /// <summary>
    /// Gets the canonical empty scope.
    /// </summary>
    public static Scope Empty => empty;

    /// <summary>
    /// Gets all bound names, materialized into a dictionary view.
    /// </summary>
    public IEnumerable<string> Keys => Materialize().Keys;

    /// <summary>
    /// Gets all bound values, materialized into a dictionary view.
    /// </summary>
    public IEnumerable<Value> Values => Materialize().Values;

    /// <summary>
    /// Gets the number of visible bindings in this scope.
    /// </summary>
    public int Count => Materialize().Count;

    /// <summary>
    /// Gets the bound value for a name, throwing when the name is missing.
    /// </summary>
    /// <param name="key">The name to resolve.</param>
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

    /// <summary>
    /// Returns a new child scope that adds one more lexical binding.
    /// </summary>
    /// <param name="bindingName">The name to bind.</param>
    /// <param name="bindingValue">The bound value.</param>
    /// <returns>A new scope frame layered over the current scope.</returns>
    public Scope With(string bindingName, Value bindingValue)
    {
        return new Scope(this, bindingName, bindingValue);
    }

    /// <summary>
    /// Determines whether the scope contains a binding for the given name.
    /// </summary>
    /// <param name="key">The name to check.</param>
    /// <returns><see langword="true"/> when a binding exists; otherwise <see langword="false"/>.</returns>
    public bool ContainsKey(string key)
    {
        return TryGetValue(key, out _);
    }

    /// <summary>
    /// Looks up a binding by walking from the innermost frame outward.
    /// </summary>
    /// <param name="key">The name to resolve.</param>
    /// <param name="result">Receives the resolved value when the lookup succeeds.</param>
    /// <returns><see langword="true"/> when a binding is found; otherwise <see langword="false"/>.</returns>
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

    /// <summary>
    /// Enumerates the visible bindings in the scope.
    /// </summary>
    /// <returns>An enumerator over the materialized bindings.</returns>
    public IEnumerator<KeyValuePair<string, Value>> GetEnumerator()
    {
        return Materialize().GetEnumerator();
    }

    /// <summary>
    /// Enumerates the visible bindings in the scope through the non-generic interface.
    /// </summary>
    /// <returns>An enumerator over the materialized bindings.</returns>
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    /// <summary>
    /// Materializes the persistent linked-list scope into dictionary form for enumeration APIs.
    /// </summary>
    /// <returns>A dictionary containing the visible bindings.</returns>
    private Dictionary<string, Value> Materialize()
    {
        var materialized = new Dictionary<string, Value>();
        var stack = new Stack<Scope>();

        // Walk outward first so the later unwind replays bindings from outermost to innermost,
        // allowing inner frames to overwrite shadowed names naturally.
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
