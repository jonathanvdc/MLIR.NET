using MLIR.Semantics;

namespace MLIR.Syntax.Types.Collections;

/// <summary>
/// Represents a builtin function type such as <c>(i32) -> i64</c>.
/// </summary>
public sealed class FunctionTypeSyntax(
    DelimitedSyntaxList<TypeSyntax> inputTypes,
    SyntaxToken arrowToken,
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
    public SyntaxToken ArrowToken { get; } = arrowToken;

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
    public override SourceLocation Location => InputTypes.OpenToken.HasValue ? InputTypes.OpenToken.Value.Location : SourceLocation.Unknown;

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

    private static IEnumerable<object> Interleave(IReadOnlyList<TypeSyntax> items, IReadOnlyList<SyntaxToken> separators)
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
