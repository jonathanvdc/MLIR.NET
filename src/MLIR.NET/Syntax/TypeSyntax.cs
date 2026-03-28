namespace MLIR.Syntax;

using MLIR.Text;

/// <summary>
/// Represents the syntax of a type.
/// </summary>
public abstract class TypeSyntax
{
    /// <summary>
    /// Attempts to project this type into preserved raw syntax text.
    /// </summary>
    public abstract bool TryGetRawText(out RawSyntaxText? rawText);

    /// <summary>
    /// Gets the preserved raw syntax text for this type.
    /// </summary>
    public RawSyntaxText GetRawText()
    {
        if (TryGetRawText(out var rawText))
        {
            return rawText!;
        }

        throw new System.InvalidOperationException("This type syntax does not provide a raw syntax-text projection.");
    }

    /// <summary>
    /// Prints the type syntax.
    /// </summary>
    public abstract void Print(SyntaxFragmentPrintingContext context);
}
