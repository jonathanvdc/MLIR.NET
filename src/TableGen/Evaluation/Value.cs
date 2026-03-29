namespace TableGen.Evaluation;

using System.Collections.Generic;

/// <summary>
/// Represents an evaluated TableGen value.
/// </summary>
public abstract class Value
{
}

/// <summary>
/// Represents an evaluated integer value.
/// </summary>
public sealed class IntegerValue(int value) : Value
{
    /// <summary>
    /// Gets the integer value.
    /// </summary>
    public int Value { get; } = value;
}

/// <summary>
/// Represents an evaluated string value.
/// </summary>
public sealed class StringValue(string value) : Value
{
    /// <summary>
    /// Gets the string value.
    /// </summary>
    public string Value { get; } = value;
}

/// <summary>
/// Represents an evaluated bit value.
/// </summary>
public sealed class BitValue(bool value) : Value
{
    /// <summary>
    /// Gets the bit value.
    /// </summary>
    public bool Value { get; } = value;
}

/// <summary>
/// Represents an evaluated list value.
/// </summary>
public sealed class ListValue(IReadOnlyList<Value> items) : Value
{
    /// <summary>
    /// Gets the list items.
    /// </summary>
    public IReadOnlyList<Value> Items { get; } = items;
}

/// <summary>
/// Represents a reference to another TableGen record.
/// </summary>
public sealed class RecordReferenceValue(string recordName) : Value
{
    /// <summary>
    /// Gets the referenced record name.
    /// </summary>
    public string RecordName { get; } = recordName;
}

/// <summary>
/// Represents a symbolic reference that could not be resolved to a local value.
/// </summary>
public sealed class SymbolReferenceValue(string symbolName) : Value
{
    /// <summary>
    /// Gets the symbolic name.
    /// </summary>
    public string SymbolName { get; } = symbolName;
}

/// <summary>
/// Represents an evaluated dag argument.
/// </summary>
public sealed class DagArgumentValue(Value value, string? name)
{
    /// <summary>
    /// Gets the argument value.
    /// </summary>
    public Value Value { get; } = value;

    /// <summary>
    /// Gets the optional argument name.
    /// </summary>
    public string? Name { get; } = name;
}

/// <summary>
/// Represents an evaluated dag expression.
/// </summary>
public sealed class DagValue(string operatorName, IReadOnlyList<DagArgumentValue> arguments) : Value
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
