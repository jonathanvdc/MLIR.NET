using MLIR.Semantics;

namespace MLIR.Syntax;

/// <summary>
/// Represents the syntax of a type.
/// </summary>
/// <remarks>
/// Implementations of <see cref="SyntaxNode.WriteTo(Text.SyntaxWriter)"/> should call
/// <see cref="Text.SyntaxWriter.WriteToken(Token)"/> for the first token to consume
/// any pending suggested trivia, and use explicit-trivia overloads for subsequent tokens.
///
/// Dialect-defined custom type syntax nodes that carry a canonical <c>!dialect.type</c>
/// prefix should derive from <see cref="DialectNamedTypeSyntax"/> so the binder can recover
/// the registered type definition from the parsed syntax.
/// </remarks>
public abstract class TypeSyntax : SyntaxNode
{
}
