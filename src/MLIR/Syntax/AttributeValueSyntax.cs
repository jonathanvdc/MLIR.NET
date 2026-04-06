using MLIR.Semantics;

namespace MLIR.Syntax;

/// <summary>
/// Represents the syntax of an attribute value.
/// </summary>
public abstract class AttributeValueSyntax : SyntaxNode
{
    /// <summary>
    /// Gets the source location of this attribute value, if known.
    /// </summary>
    public abstract SourceLocation Location { get; }
}
