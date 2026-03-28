namespace MLIR.Syntax;

/// <summary>
/// Represents a preserved token in custom operation assembly.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="CustomTokenSyntax"/> class.
/// </remarks>
/// <param name="token">The preserved token.</param>
public sealed class CustomTokenSyntax(SyntaxToken token) : CustomAssemblyItemSyntax
{
    /// <summary>
    /// Gets the preserved token.
    /// </summary>
    public SyntaxToken Token { get; } = token;
}
