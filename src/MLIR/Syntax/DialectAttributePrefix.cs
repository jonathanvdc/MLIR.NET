namespace MLIR.Syntax;

using MLIR.Text;

/// <summary>
/// A pair of tokens that represent the <c>#dialect.mnemonic</c> prefix appearing before
/// every dialect-backed attribute in MLIR assembly syntax.
/// </summary>
/// <remarks>
/// <para>
/// In MLIR, a dialect-registered attribute is always written as
/// <c>#dialect.mnemonic body</c>, where the <c>#dialect.mnemonic</c> portion is the
/// self-identifying prefix.  The two tokens that form that prefix — the <c>#</c>
/// punctuation and the <c>dialect.mnemonic</c> identifier — are bundled here so they
/// can be passed around as a unit.
/// </para>
/// <para>
/// When a syntax node is constructed from actual parsed text, the tokens carry real
/// source locations.  When a syntax node is constructed programmatically (e.g., from a
/// typed <c>AttributeValue</c> in <c>BuildCustomAssemblySyntax</c>), use
/// <see cref="Synthetic"/> to create placeholder tokens with no source location.
/// </para>
/// </remarks>
public readonly struct DialectAttributePrefix
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DialectAttributePrefix"/> struct with
    /// the supplied hash and name tokens.
    /// </summary>
    /// <param name="hashToken">The <c>#</c> token.</param>
    /// <param name="nameToken">The <c>dialect.mnemonic</c> identifier token.</param>
    public DialectAttributePrefix(Token hashToken, Token nameToken)
    {
        HashToken = hashToken;
        NameToken = nameToken;
    }

    /// <summary>Gets the <c>#</c> token.</summary>
    public Token HashToken { get; }

    /// <summary>Gets the <c>dialect.mnemonic</c> identifier token.</summary>
    public Token NameToken { get; }

    /// <summary>
    /// Creates a <see cref="DialectAttributePrefix"/> with synthetic (source-location-free)
    /// tokens for the given canonical attribute name.
    /// </summary>
    /// <param name="dialectAttributeName">
    /// The canonical attribute name, e.g. <c>"miniemitc.opaque"</c>.
    /// </param>
    public static DialectAttributePrefix Synthetic(string dialectAttributeName)
        => new DialectAttributePrefix(TokenFactory.Hash(), TokenFactory.Identifier(dialectAttributeName));

    /// <summary>
    /// Writes the <c>#dialect.mnemonic</c> prefix tokens to the supplied writer.
    /// </summary>
    /// <param name="writer">The syntax writer to write to.</param>
    public void WriteTo(SyntaxWriter writer)
    {
        writer.WriteToken(HashToken);
        writer.WriteToken(NameToken);
    }
}
