namespace MLIR.Syntax;

/// <summary>
/// Represents preserved raw text in custom operation assembly.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="CustomRawSyntax"/> class.
/// </remarks>
/// <param name="text">The preserved raw text.</param>
public sealed class CustomRawSyntax(RawSyntaxText text) : CustomAssemblyItemSyntax
{
    /// <summary>
    /// Gets the preserved raw text.
    /// </summary>
    public RawSyntaxText Text { get; } = text;
}
