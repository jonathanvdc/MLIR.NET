namespace MLIR.Syntax.Attributes.Collections;

using MLIR.Text;

using MLIR.Semantics;
using MLIR.Syntax;

/// <summary>
/// Represents a dictionary attribute value.
/// </summary>
public sealed class DictionaryAttributeValueSyntax(DelimitedSyntaxList<NamedAttributeSyntax> attributes) : AttributeValueSyntax
{
    /// <summary>
    /// Gets the dictionary entries.
    /// </summary>
    public DelimitedSyntaxList<NamedAttributeSyntax> Attributes { get; } = attributes;

    /// <inheritdoc/>
    public override SourceLocation Location =>
        SourceLocation.Merge(
            Attributes.OpenToken.HasValue ? Attributes.OpenToken.Value.Location : SourceLocation.Unknown,
            Attributes.CloseToken.HasValue ? Attributes.CloseToken.Value.Location : SourceLocation.Unknown);

    /// <inheritdoc/>
    public override void WriteTo(Text.SyntaxWriter writer)
    {
        Attributes.WriteTo(writer, static (attr, w) => attr.WriteTo(w));
    }

    /// <inheritdoc/>
    public override SyntaxNode Rewrite(SyntaxRewriter rewriter)
    {
        return new DictionaryAttributeValueSyntax(rewriter.VisitDelimitedList(Attributes));
    }

    private string BuildText()
    {
        if (!Attributes.IsPresent)
        {
            return string.Empty;
        }

        var text = "{";
        for (var i = 0; i < Attributes.Count; i++)
        {
            if (i > 0)
            {
                text += ", ";
            }

            text += Attributes[i].Name + " = " + Attributes[i].ValueSyntax.ToString();
        }

        return text + "}";
    }
}
