namespace MLIR.Syntax;

/// <summary>
/// Marks a type syntax node that exposes the canonical type-name token used by dialect-defined types.
/// </summary>
public interface ITypeNameSyntax
{
    /// <summary>
    /// Gets the canonical type-name token.
    /// </summary>
    SyntaxToken NameToken { get; }
}
