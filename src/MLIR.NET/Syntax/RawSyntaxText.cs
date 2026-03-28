namespace MLIR.Syntax;

/// <summary>
/// Stores a fragment of MLIR syntax that is preserved as raw text.
/// </summary>
public sealed class RawSyntaxText
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RawSyntaxText"/> class.
    /// </summary>
    /// <param name="text">The preserved syntax text.</param>
    public RawSyntaxText(string text)
    {
        Text = text;
    }

    /// <summary>
    /// Gets the preserved syntax text.
    /// </summary>
    public string Text { get; }

    /// <summary>
    /// Returns the preserved syntax text.
    /// </summary>
    /// <returns>The underlying text.</returns>
    public override string ToString()
    {
        return Text;
    }
}
