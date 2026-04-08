namespace MLIR.Syntax;

using MLIR.Semantics;
using MLIR.Text;

/// <summary>
/// Abstract base class for the full assembly syntax of a dialect-defined named type.
/// </summary>
/// <remarks>
/// <para>
/// Code-generated syntax classes for dialect-defined types extend this class directly, which
/// keeps the type-name token plumbing in one place and mirrors the role played by
/// <see cref="DialectPrefixedAttributeValueSyntax"/> for attributes.
/// </para>
/// <para>
/// The generated <c>WriteTo</c> method should call <see cref="WriteName"/> first, then write
/// the rest of the type-specific body tokens.
/// </para>
/// </remarks>
public abstract class DialectNamedTypeSyntax : TypeSyntax, ITypeNameSyntax
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DialectNamedTypeSyntax"/> class.
    /// </summary>
    /// <param name="nameToken">The canonical type-name token.</param>
    protected DialectNamedTypeSyntax(SyntaxToken nameToken)
    {
        NameToken = nameToken;
    }

    /// <summary>
    /// Gets the canonical type-name token for this syntax node.
    /// </summary>
    public SyntaxToken NameToken { get; }

    /// <summary>
    /// Gets the canonical type name text.
    /// </summary>
    public string Name => NameToken.Text;

    /// <summary>
    /// Writes the canonical type-name token to the supplied writer.
    /// Subclasses should call this first in their <c>WriteTo</c> implementation,
    /// followed by the body tokens.
    /// </summary>
    /// <param name="writer">The syntax writer to write to.</param>
    protected void WriteName(SyntaxWriter writer)
        => writer.WriteToken(NameToken);
}
