namespace MLIR.Semantics;

using System;
using System.Collections.Generic;
using MLIR.Syntax;

/// <summary>
/// Provides a lightweight typed wrapper over a semantic <see cref="Operation"/>.
/// </summary>
public abstract class OperationView
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OperationView"/> class.
    /// </summary>
    /// <param name="operation">The wrapped semantic operation.</param>
    /// <param name="expectedName">The canonical operation name expected by the view.</param>
    /// <exception cref="ArgumentException">Thrown when the operation does not match the expected name.</exception>
    protected OperationView(Operation operation, string expectedName)
    {
        if (!string.Equals(operation.Name, expectedName, StringComparison.Ordinal))
        {
            throw new ArgumentException($"Expected operation '{expectedName}' but received '{operation.Name}'.", nameof(operation));
        }

        Operation = operation;
    }

    /// <summary>
    /// Gets the wrapped semantic operation.
    /// </summary>
    public Operation Operation { get; }

    /// <summary>
    /// Gets the operation's SSA results.
    /// </summary>
    public IReadOnlyList<string> Results => Operation.Results;

    /// <summary>
    /// Gets the operation's SSA operands.
    /// </summary>
    public IReadOnlyList<string> Operands => Operation.Operands;

    /// <summary>
    /// Gets the operation's successors.
    /// </summary>
    public IReadOnlyList<string> Successors => Operation.Successors;

    /// <summary>
    /// Gets the operation's regions.
    /// </summary>
    public IReadOnlyList<Region> Regions => Operation.Regions;

    /// <summary>
    /// Gets the operation's attributes.
    /// </summary>
    public IReadOnlyList<NamedAttribute> Attributes => Operation.Attributes;

    /// <summary>
    /// Gets the operation's raw type signature text, if present.
    /// </summary>
    public RawSyntaxText? TypeSignature => Operation.TypeSignature;

    /// <summary>
    /// Determines whether the wrapped operation has an attribute with the supplied name.
    /// </summary>
    public bool HasAttribute(string name)
    {
        return Operation.HasAttribute(name);
    }

    /// <summary>
    /// Gets an attribute by name.
    /// </summary>
    /// <param name="name">The attribute name.</param>
    /// <returns>The matching attribute.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when the attribute is not present.</exception>
    public NamedAttribute GetAttribute(string name)
    {
        return Operation.GetAttribute(name);
    }

    /// <summary>
    /// Determines whether the wrapped operation has a semantic property with the supplied name.
    /// </summary>
    public bool HasProperty(string name)
    {
        return Operation.HasProperty(name);
    }

    /// <summary>
    /// Gets a semantic property by name.
    /// </summary>
    public T GetProperty<T>(string name)
    {
        return Operation.GetProperty<T>(name);
    }
}
