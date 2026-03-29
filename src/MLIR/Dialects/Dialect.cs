namespace MLIR.Dialects;

using System.Collections.Generic;

/// <summary>
/// Represents a concrete dialect registration made up of operation definitions.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="Dialect"/> class.
/// </remarks>
/// <param name="name">The dialect namespace, such as <c>arith</c>.</param>
/// <param name="operations">The operation definitions exported by the dialect.</param>
/// <param name="attributes">The attribute definitions exported by the dialect.</param>
/// <param name="types">The type definitions exported by the dialect.</param>
public sealed class Dialect(
    string name,
    IReadOnlyList<OperationDefinition> operations,
    IReadOnlyList<AttributeDefinition>? attributes = null,
    IReadOnlyList<TypeDefinition>? types = null)
{
    /// <summary>
    /// Creates a dialect from a fluent builder callback.
    /// </summary>
    /// <param name="name">The dialect namespace.</param>
    /// <param name="configure">The callback that configures the dialect.</param>
    /// <returns>The built dialect.</returns>
    public static Dialect Create(string name, System.Action<DialectBuilder> configure)
    {
        var builder = new DialectBuilder(name);
        configure(builder);
        return builder.Build();
    }

    /// <summary>
    /// Gets the dialect namespace.
    /// </summary>
    public string Name { get; } = name;

    /// <summary>
    /// Gets the operation definitions exported by the dialect.
    /// </summary>
    public IReadOnlyList<OperationDefinition> Operations { get; } = operations;

    /// <summary>
    /// Gets the attribute definitions exported by the dialect.
    /// </summary>
    public IReadOnlyList<AttributeDefinition> Attributes { get; } = attributes ?? EmptyAttributes;

    /// <summary>
    /// Gets the type definitions exported by the dialect.
    /// </summary>
    public IReadOnlyList<TypeDefinition> Types { get; } = types ?? EmptyTypes;

    private static readonly IReadOnlyList<AttributeDefinition> EmptyAttributes = new AttributeDefinition[0];
    private static readonly IReadOnlyList<TypeDefinition> EmptyTypes = new TypeDefinition[0];
}
