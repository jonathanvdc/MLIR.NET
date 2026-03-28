namespace MLIR.Dialects;

using System;
using System.Collections.Generic;

/// <summary>
/// Stores the set of registered dialects and operation definitions used for semantic binding.
/// </summary>
public sealed class DialectRegistry
{
    private readonly Dictionary<string, IMlirDialect> dialectsByName = new Dictionary<string, IMlirDialect>(StringComparer.Ordinal);
    private readonly Dictionary<string, OperationDefinition> operationsByName = new Dictionary<string, OperationDefinition>(StringComparer.Ordinal);
    private readonly Dictionary<string, AttributeDefinition> attributesByName = new Dictionary<string, AttributeDefinition>(StringComparer.Ordinal);
    private readonly Dictionary<string, TypeDefinition> typesByName = new Dictionary<string, TypeDefinition>(StringComparer.Ordinal);

    /// <summary>
    /// Gets the dialects currently registered in the registry.
    /// </summary>
    public IReadOnlyCollection<IMlirDialect> Dialects => dialectsByName.Values;

    /// <summary>
    /// Registers a dialect and all of its operation definitions.
    /// </summary>
    /// <param name="dialect">The dialect to register.</param>
    /// <exception cref="ArgumentException">Thrown when a dialect or definition name is registered more than once.</exception>
    public void RegisterDialect(IMlirDialect dialect)
    {
        if (dialectsByName.ContainsKey(dialect.Name))
        {
            throw new ArgumentException($"The dialect '{dialect.Name}' is already registered.", nameof(dialect));
        }

        foreach (var operation in dialect.Operations)
        {
            if (operationsByName.ContainsKey(operation.Name))
            {
                throw new ArgumentException($"The operation '{operation.Name}' is already registered.", nameof(dialect));
            }
        }

        foreach (var attribute in dialect.Attributes)
        {
            if (attributesByName.ContainsKey(attribute.Name))
            {
                throw new ArgumentException($"The attribute '{attribute.Name}' is already registered.", nameof(dialect));
            }
        }

        foreach (var type in dialect.Types)
        {
            if (typesByName.ContainsKey(type.Name))
            {
                throw new ArgumentException($"The type '{type.Name}' is already registered.", nameof(dialect));
            }
        }

        dialectsByName.Add(dialect.Name, dialect);
        foreach (var operation in dialect.Operations)
        {
            operationsByName.Add(operation.Name, operation);
        }

        foreach (var attribute in dialect.Attributes)
        {
            attributesByName.Add(attribute.Name, attribute);
        }

        foreach (var type in dialect.Types)
        {
            typesByName.Add(type.Name, type);
        }
    }

    /// <summary>
    /// Tries to resolve an operation definition by its canonical name.
    /// </summary>
    /// <param name="name">The canonical operation name.</param>
    /// <param name="operation">When this method returns, contains the operation definition if found.</param>
    /// <returns><see langword="true"/> when a definition was found; otherwise, <see langword="false"/>.</returns>
    public bool TryGetOperation(string name, out OperationDefinition operation)
    {
        return operationsByName.TryGetValue(name, out operation!);
    }

    /// <summary>
    /// Tries to resolve an attribute definition by its canonical name.
    /// </summary>
    public bool TryGetAttribute(string name, out AttributeDefinition attribute)
    {
        return attributesByName.TryGetValue(name, out attribute!);
    }

    /// <summary>
    /// Tries to resolve a type definition by its canonical name.
    /// </summary>
    public bool TryGetType(string name, out TypeDefinition type)
    {
        return typesByName.TryGetValue(name, out type!);
    }
}
