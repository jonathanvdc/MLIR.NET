namespace TableGen.Evaluation;

using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Represents a runtime value that exposes record-like field and base-class semantics.
/// </summary>
/// <remarks>
/// This unifies the common surface shared by named top-level <c>def</c> records and
/// expression-time anonymous class instantiations. Callers that only need field lookup,
/// base-class queries, or a display name can work through this abstraction instead of
/// branching on <see cref="Record"/> versus <see cref="AnonymousRecordValue"/>.
/// </remarks>
public abstract class RecordLikeValue : Value
{
    /// <summary>
    /// Gets the visible fields for this value, including any class-extension overlays.
    /// </summary>
    public abstract IReadOnlyDictionary<string, Value> Fields { get; }

    /// <summary>
    /// Gets the ordered list of transitive base classes visible on this value.
    /// </summary>
    public abstract IReadOnlyList<EvaluatedClass> BaseClasses { get; }

    /// <summary>
    /// Gets the name used when this value is rendered in string-oriented TableGen contexts.
    /// </summary>
    public abstract string DisplayName { get; }

    /// <summary>
    /// Gets the transitive base-class names in first-seen order.
    /// </summary>
    public IEnumerable<string> BaseClassNames => BaseClasses.Select(static c => c.Name);

    /// <summary>
    /// Gets a field by name, including any extension fields contributed by base-class overlays.
    /// </summary>
    /// <param name="name">The field name.</param>
    /// <returns>The field value.</returns>
    public Value GetField(string name)
    {
        return Fields[name];
    }

    /// <summary>
    /// Determines whether this value derives from a base class with the given name.
    /// </summary>
    /// <param name="name">The base-class name to search for.</param>
    /// <returns><see langword="true"/> when the base class is present.</returns>
    public bool HasBaseClass(string name)
    {
        for (var i = 0; i < BaseClasses.Count; i++)
        {
            if (BaseClasses[i].Name == name)
            {
                return true;
            }
        }

        return false;
    }
}
