namespace MLIR.Syntax;

using MLIR.Text;

/// <summary>
/// Represents the syntax of an attribute value.
/// </summary>
public abstract class AttributeValueSyntax
{
    /// <summary>
    /// Attempts to project this value into preserved raw syntax text.
    /// </summary>
    public abstract bool TryGetRawText(out RawSyntaxText? rawText);

    /// <summary>
    /// Gets the preserved raw syntax text for this value.
    /// </summary>
    public RawSyntaxText GetRawText()
    {
        if (TryGetRawText(out var rawText))
        {
            return rawText!;
        }

        throw new System.InvalidOperationException("This attribute value does not provide a raw syntax-text projection.");
    }

    /// <summary>
    /// Prints the attribute value.
    /// </summary>
    public abstract void Print(SyntaxFragmentPrintingContext context);
}
