namespace TableGen.Evaluation;

using System.Collections.Generic;
using System.Collections.ObjectModel;

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

/// <summary>
/// Represents the TableGen unset ('?') value.
/// </summary>
public sealed class UnsetValue : Value
{
}

/// <summary>
/// Represents an anonymously instantiated class record value.
/// </summary>
/// <remarks>
/// The <see cref="Fields"/> property returns an <see cref="ExtensionAwareFieldView"/>, giving
/// anonymous records the same extension-field visibility as top-level <see cref="Record"/>
/// instances. The instantiated class's own <see cref="EvaluatedClass"/> is stored directly so
/// that class-level <c>extends</c> overlays targeting that class are always found, regardless
/// of whether the class also appears in any top-level <c>def</c> base-class chain.
/// </remarks>
public sealed class AnonymousRecordValue : RecordLikeValue
{
    private readonly IReadOnlyDictionary<string, Value> localFields;

    /// <summary>
    /// Initializes an <see cref="AnonymousRecordValue"/>.
    /// </summary>
    /// <param name="ownClass">
    /// The shared <see cref="EvaluatedClass"/> for the instantiated class. Stored directly so
    /// that extensions attached to the class are visible through <see cref="Fields"/> even when
    /// no top-level <c>def</c> derives from the class.
    /// </param>
    /// <param name="localFields">The evaluated field values produced by the instantiation.</param>
    /// <param name="inheritedBaseClasses">
    /// The transitive base classes of the instantiated class (not including the class itself,
    /// which is covered by <paramref name="ownClass"/>).
    /// </param>
    internal AnonymousRecordValue(
        EvaluatedClass ownClass,
        IReadOnlyDictionary<string, Value> localFields,
        IReadOnlyList<EvaluatedClass> inheritedBaseClasses)
    {
        OwnClass = ownClass;
        this.localFields = localFields;

        // Combine own class and inherited bases into one list so that ExtensionAwareFieldView
        // can iterate them uniformly. Own class is first so that if multiple base classes
        // contribute the same extension field name, the instantiated class's extension wins.
        var allClasses = new List<EvaluatedClass>(1 + inheritedBaseClasses.Count);
        allClasses.Add(ownClass);
        allClasses.AddRange(inheritedBaseClasses);
        BaseClasses = allClasses;
    }

    /// <summary>
    /// Gets the <see cref="EvaluatedClass"/> for the class that was instantiated.
    /// </summary>
    public EvaluatedClass OwnClass { get; }

    /// <summary>
    /// Gets the class name that was instantiated.
    /// </summary>
    public string ClassName => OwnClass.Name;

    /// <summary>
    /// Gets the name used when this anonymous record participates in string-oriented TableGen contexts.
    /// </summary>
    public override string DisplayName => ClassName;

    /// <summary>
    /// Gets the combined list of base classes: the instantiated class itself followed by its
    /// transitive inherited bases, in first-seen order.
    /// </summary>
    public override IReadOnlyList<EvaluatedClass> BaseClasses { get; }

    /// <summary>
    /// Gets a unified view of all fields: locally instantiated fields first, followed by any
    /// fields contributed by class-level <c>extends</c> overlays on <see cref="OwnClass"/> or
    /// its inherited base classes.
    /// </summary>
    public override IReadOnlyDictionary<string, Value> Fields => new ExtensionAwareFieldView(localFields, BaseClasses);
}
