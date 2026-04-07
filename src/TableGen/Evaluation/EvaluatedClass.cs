namespace TableGen.Evaluation;

using System.Collections.Generic;

/// <summary>
/// Represents an evaluated class definition and accumulates any field sets contributed by
/// class-level <c>extends</c> overlays declared against the class.
/// </summary>
/// <remarks>
/// A single <see cref="EvaluatedClass"/> instance is shared by every <see cref="Record"/> whose
/// transitive base-class chain includes the class. Attaching extension fields to the object
/// therefore makes them visible on all derived records simultaneously through their
/// <see cref="Record.Fields"/> view, without mutating any individual record.
/// </remarks>
public sealed class EvaluatedClass
{
    private readonly List<IReadOnlyDictionary<string, Value>> extensionFieldSets = new();

    /// <summary>
    /// Initializes an <see cref="EvaluatedClass"/> with the given name.
    /// </summary>
    /// <param name="name">The class name as it appears in the TableGen source.</param>
    internal EvaluatedClass(string name)
    {
        Name = name;
    }

    /// <summary>
    /// Gets the class name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Attaches a resolved extension field set to this class.
    /// Called by <see cref="RecordBuilder"/> when processing a class-level <c>extends</c> overlay.
    /// </summary>
    /// <param name="fields">The evaluated extension fields to attach.</param>
    internal void AddExtensionFields(IReadOnlyDictionary<string, Value> fields)
    {
        extensionFieldSets.Add(fields);
    }

    /// <summary>
    /// Attempts to find a field contributed by any of this class's extension sets,
    /// returning the value from the first set that contains the key.
    /// </summary>
    /// <param name="key">The field name to look up.</param>
    /// <param name="value">Receives the field value when the lookup succeeds.</param>
    /// <returns><see langword="true"/> when a matching extension field was found.</returns>
    internal bool TryGetExtensionField(string key, out Value value)
    {
        foreach (var set in extensionFieldSets)
        {
            if (set.TryGetValue(key, out value!))
            {
                return true;
            }
        }

        value = default!;
        return false;
    }

    /// <summary>
    /// Enumerates all extension fields contributed by this class, yielding each key at most once
    /// (first-set-wins across multiple extension declarations targeting the same class).
    /// </summary>
    internal IEnumerable<KeyValuePair<string, Value>> GetExtensionFields()
    {
        if (extensionFieldSets.Count == 0)
        {
            yield break;
        }

        if (extensionFieldSets.Count == 1)
        {
            foreach (var pair in extensionFieldSets[0])
            {
                yield return pair;
            }

            yield break;
        }

        // Multiple extension sets: deduplicate by key, first-set-wins.
        var seenKeys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var set in extensionFieldSets)
        {
            foreach (var pair in set)
            {
                if (seenKeys.Add(pair.Key))
                {
                    yield return pair;
                }
            }
        }
    }
}
