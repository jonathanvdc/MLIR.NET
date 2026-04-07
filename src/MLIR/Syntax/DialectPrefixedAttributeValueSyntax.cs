namespace MLIR.Syntax;

using MLIR.Semantics;

/// <summary>
/// Represents the full assembly syntax for a dialect attribute of the form
/// <c>#dialect.attr_name&lt;body&gt;</c>.
/// </summary>
/// <remarks>
/// <para>
/// In MLIR, every <c>AttrDef</c>-backed attribute is serialised as
/// <c>#dialect.mnemonic body</c>, where <c>#dialect.mnemonic</c> is the
/// self-identifying prefix and <c>body</c> is whatever the attribute's custom
/// assembly format defines (e.g. <c>&lt;"NULL"&gt;</c> for a string parameter).
/// </para>
/// <para>
/// The parser consumes the <c>#name</c> prefix tokens before delegating to
/// the registered <see cref="Dialects.IAttributeAssemblyFormat"/>; the format
/// therefore only sees the body.  On the print path the generated
/// <c>BuildCustomAssemblySyntax</c> wraps the body in a
/// <see cref="DialectPrefixedAttributeValueSyntax"/> so that <see cref="WriteTo"/>
/// emits the complete <c>#name body</c> form.
/// </para>
/// </remarks>
public sealed class DialectPrefixedAttributeValueSyntax : AttributeValueSyntax
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DialectPrefixedAttributeValueSyntax"/> class.
    /// </summary>
    /// <param name="dialectAttributeName">
    /// The canonical dialect attribute name, e.g. <c>"miniemitc.opaque"</c>.
    /// Written as <c>#miniemitc.opaque</c> in the output.
    /// </param>
    /// <param name="body">
    /// The body syntax produced by the attribute's custom assembly format.
    /// </param>
    public DialectPrefixedAttributeValueSyntax(string dialectAttributeName, AttributeValueSyntax body)
    {
        DialectAttributeName = dialectAttributeName;
        Body = body;
    }

    /// <summary>
    /// Gets the canonical dialect attribute name (without the leading <c>#</c>).
    /// </summary>
    public string DialectAttributeName { get; }

    /// <summary>
    /// Gets the body syntax that follows the <c>#name</c> prefix.
    /// </summary>
    public AttributeValueSyntax Body { get; }

    /// <inheritdoc/>
    public override SourceLocation Location => Body.Location;

    /// <inheritdoc/>
    public override void WriteTo(Text.SyntaxWriter writer)
    {
        writer.WriteToken(new SyntaxToken("#" + DialectAttributeName));
        Body.WriteTo(writer);
    }
}
