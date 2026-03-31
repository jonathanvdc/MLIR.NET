namespace MLIR.Semantics;

using MLIR.Dialects;
using MLIR.Syntax;

/// <summary>
/// Represents an attribute value whose dialect-specific semantic type is unknown.
/// </summary>
public sealed class UnknownAttributeValue : AttributeValue
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UnknownAttributeValue"/> class.
    /// </summary>
    public UnknownAttributeValue(AttributeValueSyntax syntax, string? name, AttributeDefinition? definition, SourceLocation location)
        : base(syntax, name, definition, location)
    {
    }
}
