namespace MLIR.ODS.Model;

/// <summary>
/// Describes how a generated syntax property participates in concrete-syntax rewriting.
/// </summary>
/// <remarks>
/// The shape is resolved once while importing ODS parameter metadata so generators do not
/// rediscover syntax behavior by matching raw C# type-name strings at every use site.
/// </remarks>
public enum SyntaxValueShape
{
    /// <summary>A single required syntax token.</summary>
    Token,

    /// <summary>A single optional syntax token.</summary>
    OptionalToken,

    /// <summary>A raw source-text fragment.</summary>
    RawText,

    /// <summary>A concrete syntax node.</summary>
    SyntaxNode,

    /// <summary>An optional concrete syntax node.</summary>
    OptionalSyntaxNode,

    /// <summary>A delimited list of concrete syntax nodes.</summary>
    DelimitedList,

    /// <summary>A delimited list of tokens.</summary>
    DelimitedTokenList,

    /// <summary>A separated list of concrete syntax nodes.</summary>
    SeparatedList,

    /// <summary>A separated list of tokens.</summary>
    SeparatedTokenList,

    /// <summary>An unstructured list of tokens.</summary>
    TokenList,

    /// <summary>An unstructured list of raw source-text fragments.</summary>
    RawTextList,

    /// <summary>An unstructured list of concrete syntax nodes.</summary>
    SyntaxList,

    /// <summary>A non-syntax value that should be preserved as-is during rewriting.</summary>
    PlainValue,
}
