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
    private readonly Dictionary<string, TypeConstraintDefinition> typeConstraintsByName = new Dictionary<string, TypeConstraintDefinition>(StringComparer.Ordinal);

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
        RegisterDialect(dialect, isDependency: false);
    }

    private void RegisterDialect(Dialect dialect, bool isDependency)
    {
        if (dialectsByName.ContainsKey(dialect.Name))
        {
            if (isDependency)
            {
                return;
            }

            throw new ArgumentException($"The dialect '{dialect.Name}' is already registered.", nameof(dialect));
        }

        foreach (var dependency in dialect.Dependencies)
        {
            RegisterDialect(dependency(), isDependency: true);
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

        foreach (var typeConstraint in dialect.TypeConstraints)
        {
            var constraintName = typeConstraint.Name;
            if (constraintName != null && typeConstraintsByName.ContainsKey(constraintName))
            {
                throw new ArgumentException($"The type constraint '{constraintName}' is already registered.", nameof(dialect));
            }

            if (constraintName != null && typesByName.ContainsKey(constraintName))
            {
                throw new ArgumentException($"The type '{constraintName}' is already registered.", nameof(dialect));
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
            // TypeDefinition derives from TypeConstraintDefinition. When a prelude constraint
            // names the same concrete type family, the registered concrete definition is the
            // strongest constraint and should become the canonical resolver result.
            typeConstraintsByName[type.Name] = type;
        }

        foreach (var typeConstraint in dialect.TypeConstraints)
        {
            var constraintName = typeConstraint.Name;
            if (constraintName != null)
            {
                typeConstraintsByName.Add(constraintName, typeConstraint);
            }
        }
    }

    /// <summary>
    /// Replaces an already-registered operation definition with a new one that has the same canonical name.
    /// This is intended for narrowly overriding generated assembly-format behavior in tests or extensions
    /// without re-registering the entire dialect.
    /// </summary>
    public void ReplaceOperation(OperationDefinition operation)
    {
        if (!operationsByName.ContainsKey(operation.Name))
        {
            throw new ArgumentException($"The operation '{operation.Name}' is not registered.", nameof(operation));
        }

        operationsByName[operation.Name] = operation;
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
    /// Tries to resolve an operation definition by its parser-facing name.
    /// Unqualified builtin operations such as <c>module</c> are resolved against
    /// the builtin dialect automatically.
    /// </summary>
    public bool TryGetOperationForParsing(string name, out OperationDefinition operation)
    {
        if (TryGetOperation(name, out operation))
        {
            return true;
        }

        return name.IndexOf('.') < 0 && TryGetOperation("builtin." + name, out operation);
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

    /// <summary>
    /// Tries to resolve a type constraint definition by name.
    /// </summary>
    public bool TryResolveTypeConstraint(string name, out TypeConstraintDefinition typeConstraint)
    {
        return typeConstraintsByName.TryGetValue(name, out typeConstraint!);
    }
}
