namespace TableGen.Evaluation;

using System.Collections.Generic;

/// <summary>
/// Represents an expanded TableGen record.
/// </summary>
public sealed class Record(string name, IReadOnlyList<string> baseClasses, IReadOnlyDictionary<string, Value> fields)
{
    /// <summary>
    /// Stores the evaluated field values, including any overlays applied after the base record was built.
    /// </summary>
    private readonly Dictionary<string, Value> fieldValues = CopyFields(fields);

    /// <summary>
    /// Copies an input field dictionary into a mutable record-owned dictionary.
    /// </summary>
    /// <param name="source">The source field dictionary.</param>
    /// <returns>A mutable copy of the source fields.</returns>
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
    /// Gets the evaluated field values.
    /// </summary>
    public IReadOnlyDictionary<string, Value> Fields => fieldValues;

    /// <summary>
    /// Gets the transitive base-class names applied to the record.
    /// </summary>
    public IReadOnlyList<string> BaseClasses { get; } = baseClasses;

    /// <summary>
    /// Gets a field by name.
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
            if (BaseClasses[i] == name)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Applies a field overlay to this record.
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
