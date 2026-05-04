namespace MLIR.Syntax;

/// <summary>
/// Carries the operation header parsed before dispatching to a custom operation assembly format.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="OperationHeader"/> class.
/// </remarks>
/// <param name="nameToken">The parsed operation name token.</param>
/// <param name="resultList">The parsed SSA result tokens with their separator tokens.</param>
/// <param name="equalsToken">The parsed equals token, if present.</param>
public readonly struct OperationHeader(
    Token nameToken,
    SeparatedSyntaxList<Token> resultList,
    Token? equalsToken)
{
    /// <summary>
    /// Gets the parsed operation name token.
    /// </summary>
    public Token NameToken { get; } = nameToken;

    /// <summary>
    /// Gets the parsed SSA result tokens with their separator tokens.
    /// </summary>
    public SeparatedSyntaxList<Token> ResultList { get; } = resultList;

    /// <summary>
    /// Gets the parsed equals token, if present.
    /// </summary>
    public Token? EqualsToken { get; } = equalsToken;
}
