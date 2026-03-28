namespace TableGen.Evaluation;

using System.Collections.Generic;

/// <summary>
/// Represents an evaluated TableGen value.
/// </summary>
public abstract class TableGenValue
{
}

/// <summary>
/// Represents an evaluated integer value.
/// </summary>
public sealed class IntegerValue(int value) : TableGenValue
{
    /// <summary>
    /// Gets the integer value.
    /// </summary>
    public int Value { get; } = value;
}

/// <summary>
/// Represents an evaluated string value.
/// </summary>
public sealed class StringValue(string value) : TableGenValue
{
    /// <summary>
    /// Gets the string value.
    /// </summary>
    public string Value { get; } = value;
}

/// <summary>
/// Represents an evaluated bit value.
/// </summary>
public sealed class BitValue(bool value) : TableGenValue
{
    /// <summary>
    /// Gets the bit value.
    /// </summary>
    public bool Value { get; } = value;
}

/// <summary>
/// Represents an evaluated list value.
/// </summary>
public sealed class ListValue(IReadOnlyList<TableGenValue> items) : TableGenValue
{
    /// <summary>
    /// Gets the list items.
    /// </summary>
    public IReadOnlyList<TableGenValue> Items { get; } = items;
}
