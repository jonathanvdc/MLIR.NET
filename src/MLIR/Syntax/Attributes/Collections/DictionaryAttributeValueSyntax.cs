namespace MLIR.Syntax.Attributes.Collections;

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
    public override bool TryGetRawText(out RawSyntaxText? rawText)
    {
        rawText = new RawSyntaxText(BuildText());
        return true;
    }

    /// <inheritdoc/>
    public override void WriteTo(Text.SyntaxWriter writer, string defaultLeadingTrivia)
    {
        writer.WriteDelimitedList(Attributes, defaultLeadingTrivia);
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

            text += Attributes[i].Name + " = " + Attributes[i].ValueSyntax.GetRawText().Text;
        }

        return text + "}";
    }
}
