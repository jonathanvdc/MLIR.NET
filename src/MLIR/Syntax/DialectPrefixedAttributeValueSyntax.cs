namespace MLIR.Syntax;

using MLIR.Semantics;
using MLIR.Text;

/// <summary>
/// Abstract base class for the full assembly syntax of a dialect attribute of the form
/// <c>#dialect.mnemonic body</c>.
/// </summary>
/// <remarks>
/// <para>
/// In MLIR, every <c>AttrDef</c>-backed attribute is serialised as
/// <c>#dialect.mnemonic body</c>, where <c>#dialect.mnemonic</c> is the
/// self-identifying prefix and <c>body</c> is whatever the attribute's custom
/// assembly format defines (e.g. <c>&lt;"NULL"&gt;</c> for a string parameter).
/// </para>
/// <para>
/// Code-generated syntax classes for dialect attributes extend this class directly,
/// which lets pattern matching on <c>DialectPrefixedAttributeValueSyntax</c> work
/// without an extra composition layer.  The generated <c>WriteTo</c> method should
/// call <see cref="WritePrefix"/> first, then write the body tokens.
/// </para>
/// <para>
/// The parser consumes the <c>#name</c> prefix tokens before delegating to the
/// registered <see cref="Dialects.IAttributeAssemblyFormat"/>; the format therefore
/// only sees the body.  The actual parsed prefix tokens are passed to the generated
/// syntax constructor via <see cref="Text.AttributeParsingContext.Prefix"/>
/// so that <see cref="WritePrefix"/> emits the original source tokens.
/// When a syntax node is constructed programmatically, use
/// <see cref="DialectAttributePrefix.Synthetic"/> to create placeholder tokens.
/// </para>
/// </remarks>
public abstract class DialectPrefixedAttributeValueSyntax : AttributeValueSyntax
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DialectPrefixedAttributeValueSyntax"/> class
    /// with the supplied dialect attribute prefix tokens.
    /// </summary>
    /// <param name="prefix">The <c>#dialect.mnemonic</c> prefix tokens.</param>
    protected DialectPrefixedAttributeValueSyntax(DialectAttributePrefix prefix)
    {
        Prefix = prefix;
    }

    /// <summary>
    /// Gets the <c>#dialect.mnemonic</c> prefix tokens for this attribute.
    /// </summary>
    public DialectAttributePrefix Prefix { get; }

    /// <summary>
    /// Writes the <c>#dialect.mnemonic</c> prefix tokens to the supplied writer.
    /// Subclasses should call this first in their <c>WriteTo</c> implementation,
    /// followed by the body tokens.
    /// </summary>
    /// <param name="writer">The syntax writer to write to.</param>
    protected void WritePrefix(SyntaxWriter writer)
        => Prefix.WriteTo(writer);
}
