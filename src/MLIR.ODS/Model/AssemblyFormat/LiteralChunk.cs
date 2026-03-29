namespace MLIR.ODS.Model.AssemblyFormat;

/// <summary>
/// A keyword, punctuation token, or whitespace literal surrounded by backticks.
/// Examples: `(`, `,`, `->`, `\n`, `foo`.
/// </summary>
public sealed class LiteralChunk : Chunk
{
    /// <summary>
    /// The literal value.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Creates a literal chunk.
    /// </summary>
    public LiteralChunk(string value)
    {
        Value = value;
    }
}

/// <summary>
/// Represents a compiled form of a <see cref="LiteralChunk"/>, suitable for
/// matching against tokenized MLIR input.
/// </summary>
public abstract class Literal
{
}

/// <summary>
/// A literal corresponding to a single punctuation token (e.g. <c>(</c>, <c>,</c>, <c>-&gt;</c>).
/// </summary>
public sealed class PunctuationLiteral(Text.TokenKind tokenKind) : Literal
{
    /// <summary>
    /// The specific punctuation token kind expected in the input stream.
    /// </summary>
    public Text.TokenKind TokenKind { get; } = tokenKind;
}

/// <summary>
/// A literal corresponding to an identifier-like keyword that must match exactly
/// (e.g. <c>foo</c> in <c>`foo`</c>).
/// </summary>
public sealed class KeywordLiteral(string spelling) : Literal
{
    /// <summary>
    /// The exact string spelling that must appear in the input.
    /// </summary>
    public string Spelling { get; } = spelling;
}

/// <summary>
/// A literal representing a newline (<c>\n</c>) in the assembly format.
/// </summary>
public sealed class NewlineLiteral : Literal
{
}

/// <summary>
/// A literal that matches no tokens. Used as a placeholder or for optional constructs
/// that may compile to nothing.
/// </summary>
public sealed class EmptyLiteral : Literal
{
}
