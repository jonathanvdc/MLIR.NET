namespace MLIR.Syntax;

using MLIR.Semantics;
using MLIR.Text;

/// <summary>
/// Abstract base class for the full assembly syntax of a dialect-defined named type.
/// </summary>
/// <remarks>
/// <para>
/// Code-generated syntax classes for dialect-defined types extend this class directly, which
/// keeps the type-prefix plumbing in one place and mirrors the role played by
/// <see cref="DialectPrefixedAttributeValueSyntax"/> for attributes.
/// </para>
/// <para>
/// The generated <c>WriteTo</c> method should call <see cref="WritePrefix"/> first, then write
/// the rest of the type-specific body tokens.
/// </para>
/// </remarks>
public abstract class DialectNamedTypeSyntax : TypeSyntax
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DialectNamedTypeSyntax"/> class.
    /// </summary>
    /// <param name="prefix">The <c>!dialect.type</c> prefix tokens.</param>
    protected DialectNamedTypeSyntax(DialectTypePrefix prefix)
    {
        Prefix = prefix;
    }

    /// <summary>
    /// Gets the <c>!dialect.type</c> prefix tokens for this syntax node.
    /// </summary>
    public DialectTypePrefix Prefix { get; }

    /// <summary>
    /// Gets the canonical type name text.
    /// </summary>
    public string Name => Prefix.NameToken.Text;

    /// <summary>
    /// Gets the merged source location for the type prefix.
    /// </summary>
    public override SourceLocation Location => Prefix.Location;

    /// <summary>
    /// Writes the <c>!dialect.type</c> prefix tokens to the supplied writer.
    /// Subclasses should call this first in their <c>WriteTo</c> implementation,
    /// followed by the body tokens.
    /// </summary>
    /// <param name="writer">The syntax writer to write to.</param>
    protected void WritePrefix(SyntaxWriter writer)
        => Prefix.WriteTo(writer);
}
