namespace TableGen.Evaluation;

using MLIR.Text;
using System.Collections.Generic;

/// <summary>
/// Represents an expanded TableGen record.
/// </summary>
public sealed class Record(string name, IReadOnlyList<EvaluatedClass> baseClasses, IReadOnlyDictionary<string, Value> fields) : RecordLikeValue
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
    /// Gets the name used when this record participates in string-oriented TableGen contexts.
    /// </summary>
    public override string DisplayName => Name;

    /// <summary>
    /// Gets a unified view of all fields visible on this record: record-local fields first,
    /// followed by any fields contributed by class-level <c>extends</c> overlays on the
    /// record's base classes. Record-local fields always shadow extension fields with the
    /// same name.
    /// </summary>
    public override IReadOnlyDictionary<string, Value> Fields => new ExtensionAwareFieldView(fieldValues, BaseClasses);

    /// <summary>
    /// Gets the transitive base-class objects applied to the record, in first-seen order.
    /// Each object carries any class-level extension field sets attached via <c>extends</c>,
    /// which are surfaced through the <see cref="Fields"/> view without mutating the record.
    /// </summary>
    public override IReadOnlyList<EvaluatedClass> BaseClasses { get; } = baseClasses;

    /// <summary>
    /// Applies a field overlay to this record, used when processing <c>extends</c> on a
    /// <c>def</c> target.
    /// </summary>
    /// <param name="overlayFields">The field values to merge into the record.</param>
    /// <param name="location">The source location to attach to any conflict diagnostic.</param>
    /// <returns>A success flag or a diagnostic when the overlay conflicts with an existing field.</returns>
    internal ParseResult<bool> ApplyOverlayFields(IReadOnlyDictionary<string, Value> overlayFields, SourceLocation location)
    {
        foreach (var pair in overlayFields)
        {
            if (fieldValues.ContainsKey(pair.Key))
            {
                return ParseResult<bool>.Failure(
                    new Diagnostic(
                        $"Record '{Name}' already defines field '{pair.Key}'.",
                        location));
            }
        }

        foreach (var pair in overlayFields)
        {
            fieldValues[pair.Key] = pair.Value;
        }

        return ParseResult<bool>.Success(true);
    }
}
