using MLIR.Semantics;

namespace MLIR.Syntax.Types.Collections;

using MLIR.Text;

/// <summary>
/// Represents a builtin function type such as <c>(i32) -> i64</c>.
/// </summary>
public sealed class FunctionTypeSyntax(
    DelimitedSyntaxList<TypeSyntax> inputTypes,
    Token arrowToken,
    TypeSyntax? resultType,
    DelimitedSyntaxList<TypeSyntax> resultTypes) : TypeSyntax
{
    /// <summary>
    /// Gets the input type list.
    /// </summary>
    public DelimitedSyntaxList<TypeSyntax> InputTypes { get; } = inputTypes;

    /// <summary>
    /// Gets the arrow token.
    /// </summary>
    public Token ArrowToken { get; } = arrowToken;

    /// <summary>
    /// Gets the single bare result type when the result was not parenthesized.
    /// </summary>
    public TypeSyntax? ResultType { get; } = resultType;

    /// <summary>
    /// Gets the parenthesized result type list when present.
    /// </summary>
    public DelimitedSyntaxList<TypeSyntax> ResultTypes { get; } = resultTypes;

    /// <summary>
    /// Gets a value indicating whether the result list is parenthesized.
    /// </summary>
    public bool HasDelimitedResults => ResultTypes.IsPresent;

    /// <inheritdoc/>
    public override SourceLocation Location
    {
        get
        {
            // Start at the opening parenthesis of the input type list.
            var start = InputTypes.OpenToken.HasValue
                ? InputTypes.OpenToken.Value.Location
                : SourceLocation.Unknown;

            // End at the last token of the result: the close paren of a delimited result
            // list, the single result type, or unknown when there are no results.
            SourceLocation end;
            if (HasDelimitedResults && ResultTypes.CloseToken.HasValue)
                end = ResultTypes.CloseToken.Value.Location;
            else if (HasDelimitedResults && ResultTypes.Items.Count > 0)
                end = ResultTypes.Items[ResultTypes.Items.Count - 1].Location;
            else if (!HasDelimitedResults && ResultType != null)
                end = ResultType.Location;
            else
                end = SourceLocation.Unknown;

            return SourceLocation.Merge(start, end);
        }
    }

    /// <inheritdoc/>
    public override void WriteTo(Text.SyntaxWriter writer)
    {
        InputTypes.WriteTo(writer, static (type, w) => type.WriteTo(w));
        writer.WriteToken(ArrowToken, " ");
        if (HasDelimitedResults)
        {
            writer.WriteDelimitedList(ResultTypes, " ");
        }
        else
        {
            writer.SuggestTrivia(" ");
            ResultType!.WriteTo(writer);
        }
    }

    /// <inheritdoc/>
    public override SyntaxNode Rewrite(SyntaxRewriter rewriter)
    {
        return new FunctionTypeSyntax(
            rewriter.VisitDelimitedList(InputTypes),
            rewriter.VisitToken(ArrowToken),
            ResultType != null ? (TypeSyntax)rewriter.Visit(ResultType) : null,
            HasDelimitedResults
                ? rewriter.VisitDelimitedList(ResultTypes)
                : new DelimitedSyntaxList<TypeSyntax>(null, ResultType != null ? [(TypeSyntax)rewriter.Visit(ResultType)] : [], [], null));
    }

    private static IEnumerable<object> Interleave(IReadOnlyList<TypeSyntax> items, IReadOnlyList<Token> separators)
    {
        for (var i = 0; i < items.Count; i++)
        {
            if (i > 0)
            {
                yield return separators[i - 1];
            }

            yield return items[i];
        }
    }
}
