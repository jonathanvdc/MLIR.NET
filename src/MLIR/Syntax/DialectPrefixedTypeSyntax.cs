namespace MLIR.Syntax;

using MLIR.Semantics;
using MLIR.Text;

/// <summary>
/// Abstract base class for the full assembly syntax of a dialect type of the form
/// <c>!dialect.mnemonic body</c>.
/// </summary>
/// <remarks>
/// <para>
/// In MLIR, every <c>TypeDef</c>-backed type is serialised as
/// <c>!dialect.mnemonic body</c>, where <c>!dialect.mnemonic</c> is the
/// self-identifying prefix and <c>body</c> is whatever the type's custom
/// assembly format defines (e.g. <c>&lt;0&gt;</c> for an address space parameter).
/// </para>
/// <para>
/// Code-generated syntax classes for dialect types extend this class directly,
/// which lets pattern matching on <c>DialectPrefixedTypeSyntax</c> work
/// without an extra composition layer.  The generated <c>WriteTo</c> method should
/// call <see cref="WritePrefix"/> first, then write the body tokens.
/// </para>
/// <para>
/// The parser consumes the <c>!name</c> prefix tokens before delegating to the
/// registered <see cref="Dialects.ITypeAssemblyFormat"/>; the format therefore
/// only sees the body.  The actual parsed prefix tokens are passed to the generated
/// syntax constructor via <see cref="Text.TypeParsingContext.Prefix"/>
/// so that <see cref="WritePrefix"/> emits the original source tokens.
/// When a syntax node is constructed programmatically, use
/// <see cref="DialectTypePrefix.Synthetic"/> to create placeholder tokens.
/// </para>
/// </remarks>
public abstract class DialectPrefixedTypeSyntax : TypeSyntax
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DialectPrefixedTypeSyntax"/> class
    /// with the supplied dialect type prefix tokens.
    /// </summary>
    /// <param name="prefix">The <c>!dialect.mnemonic</c> prefix tokens.</param>
    protected DialectPrefixedTypeSyntax(DialectTypePrefix prefix)
    {
        Prefix = prefix;
    }

    /// <summary>
    /// Gets the <c>!dialect.mnemonic</c> prefix tokens for this type.
    /// </summary>
    public DialectTypePrefix Prefix { get; }

    /// <summary>
    /// Writes the <c>!dialect.mnemonic</c> prefix tokens to the supplied writer.
    /// Subclasses should call this first in their <c>WriteTo</c> implementation,
    /// followed by the body tokens.
    /// </summary>
    /// <param name="writer">The syntax writer to write to.</param>
    protected void WritePrefix(SyntaxWriter writer)
        => Prefix.WriteTo(writer);
}
