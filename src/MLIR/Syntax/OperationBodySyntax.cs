namespace MLIR.Syntax;

/// <summary>
/// Represents the body of an MLIR operation.
/// </summary>
public abstract class OperationBodySyntax : SyntaxNode
{
    /// <summary>
    /// Writes the operation body to the supplied syntax writer.
    /// Uses <see cref="Text.SyntaxWriter.IndentLevel"/> for indentation decisions.
    /// </summary>
    public abstract void WriteTo(Text.SyntaxWriter writer);
}
