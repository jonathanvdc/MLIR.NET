namespace MLIR.Text;

using MLIR.Syntax;

/// <summary>
/// Carries the operation header parsed before dispatching to a custom operation assembly format.
/// </summary>
public sealed class OperationParseHeader
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OperationParseHeader"/> class.
    /// </summary>
    /// <param name="nameToken">The parsed operation name token.</param>
    /// <param name="resultList">The parsed SSA result tokens with their separator tokens.</param>
    /// <param name="equalsToken">The parsed equals token, if present.</param>
    public OperationParseHeader(
        Token nameToken,
        SeparatedSyntaxList<Token> resultList,
        Token? equalsToken)
    {
        NameToken = nameToken;
        ResultList = resultList;
        EqualsToken = equalsToken;
    }

    /// <summary>
    /// Gets the parsed operation name token.
    /// </summary>
    public Token NameToken { get; }

    /// <summary>
    /// Gets the parsed SSA result tokens with their separator tokens.
    /// </summary>
    public SeparatedSyntaxList<Token> ResultList { get; }

    /// <summary>
    /// Gets the parsed equals token, if present.
    /// </summary>
    public Token? EqualsToken { get; }
}
