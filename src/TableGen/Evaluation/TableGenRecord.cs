namespace TableGen.Evaluation;

using System.Collections.Generic;

/// <summary>
/// Represents an expanded TableGen record.
/// </summary>
public sealed class TableGenRecord(string name, IReadOnlyList<string> baseClasses, IReadOnlyDictionary<string, TableGenValue> fields)
{
    /// <summary>
    /// Gets the record name.
    /// </summary>
    public string Name { get; } = name;

    /// <summary>
    /// Gets the evaluated field values.
    /// </summary>
    public IReadOnlyDictionary<string, TableGenValue> Fields { get; } = fields;

    /// <summary>
    /// Gets the transitive base-class names applied to the record.
    /// </summary>
    public IReadOnlyList<string> BaseClasses { get; } = baseClasses;

    /// <summary>
    /// Gets a field by name.
    /// </summary>
    /// <param name="name">The field name.</param>
    /// <returns>The field value.</returns>
    public TableGenValue GetField(string name)
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
}
