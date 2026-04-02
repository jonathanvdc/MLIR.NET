namespace MLIR.Dialects;

using System;
using System.Collections.Generic;

/// <summary>
/// Stores the set of registered dialects and operation definitions used for semantic binding.
/// </summary>
public sealed class DialectRegistry
{
    private readonly Dictionary<string, Dialect> dialectsByName = new Dictionary<string, Dialect>(StringComparer.Ordinal);
    private readonly Dictionary<string, OperationDefinition> operationsByName = new Dictionary<string, OperationDefinition>(StringComparer.Ordinal);
    private readonly Dictionary<string, AttributeDefinition> attributesByName = new Dictionary<string, AttributeDefinition>(StringComparer.Ordinal);
    private readonly Dictionary<string, AttributeDefinition> attributesByParserName = new Dictionary<string, AttributeDefinition>(StringComparer.Ordinal);
    private readonly Dictionary<string, AttributeConstraintDefinition> attributeConstraintsByName = new Dictionary<string, AttributeConstraintDefinition>(StringComparer.Ordinal);
    private readonly Dictionary<string, TypeDefinition> typesByName = new Dictionary<string, TypeDefinition>(StringComparer.Ordinal);

    /// <summary>
    /// Gets the dialects currently registered in the registry.
    /// </summary>
    public IReadOnlyCollection<Dialect> Dialects => dialectsByName.Values;

    /// <summary>
    /// Registers a dialect and all of its operation definitions.
    /// </summary>
    /// <param name="dialect">The dialect to register.</param>
    /// <exception cref="ArgumentException">Thrown when a dialect or definition name is registered more than once.</exception>
    public void RegisterDialect(Dialect dialect)
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
            if (attribute.Name != null && attributesByName.ContainsKey(attribute.Name))
            {
                throw new ArgumentException($"The attribute '{attribute.Name}' is already registered.", nameof(dialect));
            }

            if (attribute.Name != null && attributesByParserName.ContainsKey(attribute.Name))
            {
                throw new ArgumentException($"The attribute parser name '{attribute.Name}' is already registered.", nameof(dialect));
            }

            foreach (var alias in attribute.ParserAliases)
            {
                if (attributesByParserName.ContainsKey(alias))
                {
                    throw new ArgumentException($"The attribute parser name '{alias}' is already registered.", nameof(dialect));
                }
            }
        }

        foreach (var attributeConstraint in dialect.AttributeConstraints)
        {
            if (attributeConstraint.Name != null && attributeConstraintsByName.ContainsKey(attributeConstraint.Name))
            {
                throw new ArgumentException($"The attribute constraint '{attributeConstraint.Name}' is already registered.", nameof(dialect));
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
            attributesByParserName.Add(attribute.Name, attribute);
            attributeConstraintsByName.Add(attribute.Name, attribute);
            foreach (var alias in attribute.ParserAliases)
            {
                attributesByParserName.Add(alias, attribute);
            }
        }

        foreach (var attributeConstraint in dialect.AttributeConstraints)
        {
            if (attributeConstraint.Name != null)
            {
                attributeConstraintsByName.Add(attributeConstraint.Name, attributeConstraint);
            }
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
    /// Tries to resolve an attribute definition by a parser-facing name, including registered aliases.
    /// </summary>
    public bool TryResolveAttributeForParsing(string name, out AttributeDefinition attribute)
    {
        return attributesByParserName.TryGetValue(name, out attribute!);
    }

    /// <summary>
    /// Tries to resolve an attribute constraint definition by name.
    /// </summary>
    public bool TryResolveAttributeConstraint(string name, out AttributeConstraintDefinition attributeConstraint)
    {
        if (attributesByParserName.TryGetValue(name, out var attribute))
        {
            attributeConstraint = attribute;
            return true;
        }

        return attributeConstraintsByName.TryGetValue(name, out attributeConstraint!);
    }

    /// <summary>
    /// Tries to resolve a type definition by its canonical name.
    /// </summary>
    public bool TryGetType(string name, out TypeDefinition type)
    {
        return typesByName.TryGetValue(name, out type!);
    }
}
