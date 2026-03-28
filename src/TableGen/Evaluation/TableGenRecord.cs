namespace TableGen.Evaluation;

using System.Collections.Generic;

/// <summary>
/// Represents an expanded TableGen record.
/// </summary>
public sealed class TableGenRecord(string name, IReadOnlyDictionary<string, TableGenValue> fields)
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
    /// Gets a field by name.
    /// </summary>
    /// <param name="name">The field name.</param>
    /// <returns>The field value.</returns>
    public TableGenValue GetField(string name)
    {
        return Fields[name];
    }
}
