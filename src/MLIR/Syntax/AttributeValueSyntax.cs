using MLIR.Semantics;

namespace MLIR.Syntax;

/// <summary>
/// Represents the syntax of an attribute value.
/// </summary>
/// <remarks>
/// Implementations of <see cref="SyntaxNode.WriteTo(Text.SyntaxWriter)"/> should call
/// <see cref="Text.SyntaxWriter.WriteToken(SyntaxToken)"/> for the first token to consume
/// any pending suggested trivia, and use explicit-trivia overloads for subsequent tokens.
/// </remarks>
public abstract class AttributeValueSyntax : SyntaxNode
{
    /// <summary>
    /// Gets the source location of this attribute value, if known.
    /// </summary>
    public abstract SourceLocation Location { get; }
}
