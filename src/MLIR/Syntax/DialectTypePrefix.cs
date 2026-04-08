namespace MLIR.Syntax;

using MLIR.Text;

/// <summary>
/// A pair of tokens that represent the <c>!dialect.mnemonic</c> prefix appearing before
/// every dialect-backed type in MLIR assembly syntax.
/// </summary>
/// <remarks>
/// <para>
/// In MLIR, a dialect-registered type is always written as
/// <c>!dialect.mnemonic body</c>, where the <c>!dialect.mnemonic</c> portion is the
/// self-identifying prefix.  The two tokens that form that prefix — the <c>!</c>
/// punctuation and the <c>dialect.mnemonic</c> identifier — are bundled here so they
/// can be passed around as a unit.
/// </para>
/// <para>
/// When a syntax node is constructed from actual parsed text, the tokens carry real
/// source locations.  When a syntax node is constructed programmatically (e.g., from a
/// typed <c>TypeReference</c> in <c>BuildCustomAssemblySyntax</c>), use
/// <see cref="Synthetic"/> to create placeholder tokens with no source location.
/// </para>
/// </remarks>
public readonly struct DialectTypePrefix
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DialectTypePrefix"/> struct with
    /// the supplied bang and name tokens.
    /// </summary>
    /// <param name="bangToken">The <c>!</c> token.</param>
    /// <param name="nameToken">The <c>dialect.mnemonic</c> identifier token.</param>
    public DialectTypePrefix(Token bangToken, Token nameToken)
    {
        BangToken = bangToken;
        NameToken = nameToken;
    }

    /// <summary>Gets the <c>!</c> token.</summary>
    public Token BangToken { get; }

    /// <summary>Gets the <c>dialect.mnemonic</c> identifier token.</summary>
    public Token NameToken { get; }

    /// <summary>
    /// Creates a <see cref="DialectTypePrefix"/> with synthetic (source-location-free)
    /// tokens for the given canonical type name.
    /// </summary>
    /// <param name="dialectTypeName">
    /// The canonical type name, e.g. <c>"llvm.ptr"</c>.
    /// </param>
    public static DialectTypePrefix Synthetic(string dialectTypeName)
        => new DialectTypePrefix(TokenFactory.Bang(), TokenFactory.Identifier(dialectTypeName));

    /// <summary>
    /// Writes the <c>!dialect.mnemonic</c> prefix tokens to the supplied writer.
    /// </summary>
    /// <param name="writer">The syntax writer to write to.</param>
    public void WriteTo(SyntaxWriter writer)
    {
        writer.WriteToken(BangToken);
        writer.WriteToken(NameToken);
    }
}
