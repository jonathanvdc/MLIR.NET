namespace MLIR.Dialects;

using System.Collections.Generic;

/// <summary>
/// Describes a dialect that contributes operation metadata to the semantic layer.
/// </summary>
public interface IMlirDialect
{
    /// <summary>
    /// Gets the dialect namespace, such as <c>arith</c> or <c>func</c>.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the operation definitions exported by the dialect.
    /// </summary>
    IReadOnlyList<OperationDefinition> Operations { get; }

    /// <summary>
    /// Gets the attribute definitions exported by the dialect.
    /// </summary>
    IReadOnlyList<AttributeDefinition> Attributes { get; }

    /// <summary>
    /// Gets the type definitions exported by the dialect.
    /// </summary>
    IReadOnlyList<TypeDefinition> Types { get; }
}
