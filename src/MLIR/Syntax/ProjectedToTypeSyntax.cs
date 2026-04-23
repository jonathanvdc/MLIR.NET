namespace MLIR.Syntax;

using MLIR.Semantics;
using MLIR.Text;

/// <summary>
/// Represents a projected custom-operation type signature of the form <c>sourceType to resultType</c>.
/// This is used by the parser's generic custom-like operation fallback for unregistered dialect ops
/// whose assembly resembles <c>%arg : type to type</c>.
/// </summary>
public sealed class ProjectedToTypeSyntax(TypeSyntax sourceType, Token toKeyword, TypeSyntax resultType) : TypeSyntax
{
    /// <summary>Gets the source-side type syntax.</summary>
    public TypeSyntax SourceType { get; } = sourceType;

    /// <summary>Gets the <c>to</c> keyword token.</summary>
    public Token ToKeyword { get; } = toKeyword;

    /// <summary>Gets the result-side type syntax.</summary>
    public TypeSyntax ResultType { get; } = resultType;

    /// <summary>Gets the merged source location covering the full projected signature.</summary>
    public override SourceLocation Location =>
        SourceLocation.Merge(SourceType.Location, ResultType.Location);

    /// <summary>Writes the projected <c>sourceType to resultType</c> signature back to MLIR text.</summary>
    public override void WriteTo(Text.SyntaxWriter writer)
    {
        SourceType.WriteTo(writer);
        writer.WriteToken(ToKeyword, " ");
        writer.SuggestTrivia(" ");
        ResultType.WriteTo(writer);
    }

    /// <summary>Rewrites the projected signature by visiting both nested types and the <c>to</c> token.</summary>
    public override SyntaxNode Rewrite(SyntaxRewriter rewriter)
    {
        return new ProjectedToTypeSyntax(
            (TypeSyntax)rewriter.Visit(SourceType),
            rewriter.VisitToken(ToKeyword),
            (TypeSyntax)rewriter.Visit(ResultType));
    }
}
