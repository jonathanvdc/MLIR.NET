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

/// <summary>
/// Represents a reference to another TableGen record.
/// </summary>
public sealed class RecordReferenceValue(string recordName) : TableGenValue
{
    /// <summary>
    /// Gets the referenced record name.
    /// </summary>
    public string RecordName { get; } = recordName;
}

/// <summary>
/// Represents a symbolic reference that could not be resolved to a local value.
/// </summary>
public sealed class SymbolReferenceValue(string symbolName) : TableGenValue
{
    /// <summary>
    /// Gets the symbolic name.
    /// </summary>
    public string SymbolName { get; } = symbolName;
}

/// <summary>
/// Represents an evaluated dag argument.
/// </summary>
public sealed class DagArgumentValue(TableGenValue value, string? name)
{
    /// <summary>
    /// Gets the argument value.
    /// </summary>
    public TableGenValue Value { get; } = value;

    /// <summary>
    /// Gets the optional argument name.
    /// </summary>
    public string? Name { get; } = name;
}

/// <summary>
/// Represents an evaluated dag expression.
/// </summary>
public sealed class DagValue(string operatorName, IReadOnlyList<DagArgumentValue> arguments) : TableGenValue
{
    /// <summary>
    /// Gets the dag operator name.
    /// </summary>
    public string OperatorName { get; } = operatorName;

    /// <summary>
    /// Gets the dag arguments.
    /// </summary>
    public IReadOnlyList<DagArgumentValue> Arguments { get; } = arguments;
}
