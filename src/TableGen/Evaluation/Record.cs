namespace TableGen.Evaluation;

using System.Collections.Generic;

/// <summary>
/// Represents an expanded TableGen record.
/// </summary>
public sealed class Record(string name, IReadOnlyList<EvaluatedClass> baseClasses, IReadOnlyDictionary<string, Value> fields)
{
    /// <summary>
    /// Stores the record-local evaluated field values built during instantiation.
    /// Extension fields contributed by class-level <c>extends</c> overlays are NOT stored
    /// here; they are resolved on demand through the <see cref="Fields"/> view.
    /// </summary>
    private readonly Dictionary<string, Value> fieldValues = CopyFields(fields);

    /// <summary>
    /// Copies an input field dictionary into a mutable record-owned dictionary.
    /// </summary>
    private static Dictionary<string, Value> CopyFields(IReadOnlyDictionary<string, Value> source)
    {
        var copy = new Dictionary<string, Value>(source.Count);
        foreach (var pair in source)
        {
            copy[pair.Key] = pair.Value;
        }

        return copy;
    }

    /// <summary>
    /// Gets the record name.
    /// </summary>
    public string Name { get; } = name;

    /// <summary>
    /// Gets a unified view of all fields visible on this record: record-local fields first,
    /// followed by any fields contributed by class-level <c>extends</c> overlays on the
    /// record's base classes. Record-local fields always shadow extension fields with the
    /// same name.
    /// </summary>
    public IReadOnlyDictionary<string, Value> Fields => new ExtensionAwareFieldView(fieldValues, BaseClasses);

    /// <summary>
    /// Gets the transitive base-class objects applied to the record, in first-seen order.
    /// Each object carries any class-level extension field sets attached via <c>extends</c>,
    /// which are surfaced through the <see cref="Fields"/> view without mutating the record.
    /// </summary>
    public IReadOnlyList<EvaluatedClass> BaseClasses { get; } = baseClasses;

    /// <summary>
    /// Gets the transitive base-class names in first-seen order. Convenience shorthand for
    /// <c>BaseClasses.Select(c => c.Name)</c>.
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
    /// Determines whether the record derives from a base class with the given name.
    /// </summary>
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

    /// <summary>
    /// Applies a field overlay to this record, used when processing <c>extends</c> on a
    /// <c>def</c> target.
    /// </summary>
    /// <param name="overlayFields">The field values to merge into the record.</param>
    /// <returns>A success flag or a diagnostic when the overlay conflicts with an existing field.</returns>
    internal EvaluationResult<bool> ApplyOverlayFields(IReadOnlyDictionary<string, Value> overlayFields)
    {
        foreach (var pair in overlayFields)
        {
            if (fieldValues.ContainsKey(pair.Key))
            {
                return EvaluationResult<bool>.Failure(
                    new EvaluationDiagnostic(
                        EvaluationDiagnosticKind.InvalidOperation,
                        $"Record '{Name}' already defines field '{pair.Key}'."));
            }
        }

        foreach (var pair in overlayFields)
        {
            fieldValues[pair.Key] = pair.Value;
        }

        return EvaluationResult<bool>.Success(true);
    }
}
