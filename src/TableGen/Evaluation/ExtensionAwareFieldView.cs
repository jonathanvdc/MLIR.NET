namespace TableGen.Evaluation;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// A lazy, read-only view over a field dictionary that transparently merges in extension fields
/// contributed by class-level <c>extends</c> overlays attached to a set of base classes.
/// </summary>
/// <remarks>
/// Lookup priority: record-local fields win over extension fields. Extension fields from
/// base classes earlier in the <paramref name="baseClasses"/> list win over those from later
/// entries. No data is copied at construction time; extension sets are consulted on every
/// field access so that extensions attached after the view was created are immediately visible.
/// </remarks>
internal sealed class ExtensionAwareFieldView(
    IReadOnlyDictionary<string, Value> localFields,
    IReadOnlyList<EvaluatedClass> baseClasses) : IReadOnlyDictionary<string, Value>
{
    public bool TryGetValue(string key, out Value value)
    {
        if (localFields.TryGetValue(key, out value!))
        {
            return true;
        }

        for (var i = 0; i < baseClasses.Count; i++)
        {
            if (baseClasses[i].TryGetExtensionField(key, out value!))
            {
                return true;
            }
        }

        value = default!;
        return false;
    }

    public Value this[string key]
    {
        get
        {
            if (TryGetValue(key, out var value))
            {
                return value;
            }

            throw new KeyNotFoundException(key);
        }
    }

    public bool ContainsKey(string key) => TryGetValue(key, out _);

    public IEnumerable<string> Keys => this.Select(static pair => pair.Key);

    public IEnumerable<Value> Values => this.Select(static pair => pair.Value);

    /// <summary>
    /// Returns the number of unique fields visible through this view. O(n) — avoid on hot paths.
    /// </summary>
    public int Count
    {
        get
        {
            var count = 0;
            foreach (var _ in this)
            {
                count++;
            }

            return count;
        }
    }

    public IEnumerator<KeyValuePair<string, Value>> GetEnumerator()
    {
        foreach (var pair in localFields)
        {
            yield return pair;
        }

        // Extension fields from base classes are yielded in order, skipping anything already
        // present in the local dictionary. The deduplication set is only allocated when at least
        // one extension field is encountered, keeping the common (no-extension) path allocation-free.
        HashSet<string>? seenKeys = null;
        for (var i = 0; i < baseClasses.Count; i++)
        {
            foreach (var pair in baseClasses[i].GetExtensionFields())
            {
                seenKeys ??= new HashSet<string>(localFields.Keys, StringComparer.Ordinal);
                if (seenKeys.Add(pair.Key))
                {
                    yield return pair;
                }
            }
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
