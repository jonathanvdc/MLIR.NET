namespace MLIR.Syntax;

using MLIR.Semantics;
using MLIR.Text;

/// <summary>
/// A pair of tokens that represent the <c>!dialect.type</c> prefix appearing before
/// dialect-backed type syntax nodes in MLIR assembly syntax.
/// </summary>
/// <remarks>
/// <para>
/// In MLIR, a dialect-registered type is always written as <c>!dialect.type&lt;...&gt;</c>,
/// where the <c>!</c> punctuation and the identifier token are bundled together here so
/// they can be passed around as a unit.
/// </para>
/// <para>
/// When a syntax node is constructed from actual parsed text, the tokens carry real source
/// locations. When a syntax node is constructed programmatically, use
/// <see cref="Synthetic(string)"/> to create placeholder tokens with no source location.
/// </para>
/// </remarks>
public readonly struct DialectTypePrefix
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DialectTypePrefix"/> struct with the
    /// supplied bang and name tokens.
    /// </summary>
    /// <param name="bangToken">The <c>!</c> token.</param>
    /// <param name="nameToken">The <c>dialect.type</c> identifier token.</param>
    public DialectTypePrefix(Token bangToken, Token nameToken)
    {
        BangToken = bangToken;
        NameToken = nameToken;
    }

    /// <summary>Gets the <c>!</c> token.</summary>
    public Token BangToken { get; }

    /// <summary>Gets the <c>dialect.type</c> identifier token.</summary>
    public Token NameToken { get; }

    /// <summary>Gets the source location spanning the prefix tokens.</summary>
    public SourceLocation Location => SourceLocation.Merge(BangToken.Location, NameToken.Location);

    /// <summary>
    /// Creates a <see cref="DialectTypePrefix"/> with synthetic (source-location-free)
    /// tokens for the given canonical type name.
    /// </summary>
    /// <param name="dialectTypeName">The canonical type name, e.g. <c>"typed.opaque"</c>.</param>
    public static DialectTypePrefix Synthetic(string dialectTypeName)
        => new DialectTypePrefix(TokenFactory.Bang(), TokenFactory.Identifier(dialectTypeName));

    /// <summary>
    /// Writes the <c>!dialect.type</c> prefix tokens to the supplied writer.
    /// </summary>
    /// <param name="writer">The syntax writer to write to.</param>
    public void WriteTo(SyntaxWriter writer)
    {
        writer.WriteToken(BangToken);
        writer.WriteToken(NameToken);
    }
}
