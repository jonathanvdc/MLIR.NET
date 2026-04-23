namespace MLIR.Syntax;

using MLIR.Text;

/// <summary>
/// Structured syntax for a dialect-defined type whose assembly form is just the canonical
/// <c>!dialect.type</c> prefix with no trailing body.
/// </summary>
public sealed class BareDialectTypeSyntax(DialectTypePrefix prefix) : DialectNamedTypeSyntax(prefix)
{
    /// <inheritdoc/>
    public override void WriteTo(SyntaxWriter writer)
    {
        WritePrefix(writer);
    }

    /// <inheritdoc/>
    public override SyntaxNode Rewrite(SyntaxRewriter rewriter)
    {
        return new BareDialectTypeSyntax(
            new DialectTypePrefix(
                rewriter.VisitToken(Prefix.BangToken),
                rewriter.VisitToken(Prefix.NameToken)));
    }
}
