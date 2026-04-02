namespace MLIR.Dialects;

using System;
using System.Collections.Generic;

/// <summary>
/// Provides a fluent API for constructing <see cref="Dialect"/> instances.
/// </summary>
public sealed class DialectBuilder
{
    private readonly string name;
    private readonly List<OperationDefinition> operations = new List<OperationDefinition>();
    private readonly List<AttributeDefinition> attributes = new List<AttributeDefinition>();
    private readonly List<AttributeConstraintDefinition> attributeConstraints = new List<AttributeConstraintDefinition>();
    private readonly List<TypeDefinition> types = new List<TypeDefinition>();

    /// <summary>
    /// Initializes a new instance of the <see cref="DialectBuilder"/> class.
    /// </summary>
    /// <param name="name">The dialect namespace.</param>
    public DialectBuilder(string name)
    {
        this.name = name;
    }

    /// <summary>
    /// Adds an already constructed operation definition.
    /// </summary>
    public DialectBuilder AddOperation(OperationDefinition operation)
    {
        operations.Add(operation);
        return this;
    }

    /// <summary>
    /// Adds an operation definition configured via a fluent builder callback.
    /// </summary>
    public DialectBuilder AddOperation(string name, Action<OperationDefinitionBuilder> configure)
    {
        var builder = new OperationDefinitionBuilder(name);
        configure(builder);
        operations.Add(builder.Build());
        return this;
    }

    /// <summary>
    /// Adds an attribute definition.
    /// </summary>
    public DialectBuilder AddAttribute(AttributeDefinition attribute)
    {
        attributes.Add(attribute);
        return this;
    }

    /// <summary>
    /// Adds an attribute constraint definition.
    /// </summary>
    public DialectBuilder AddAttributeConstraint(AttributeConstraintDefinition attributeConstraint)
    {
        attributeConstraints.Add(attributeConstraint);
        return this;
    }

    /// <summary>
    /// Adds a type definition.
    /// </summary>
    public DialectBuilder AddType(TypeDefinition type)
    {
        types.Add(type);
        return this;
    }

    /// <summary>
    /// Builds the dialect.
    /// </summary>
    public Dialect Build()
    {
        return new Dialect(name, operations, attributes, types, attributeConstraints);
    }
}
