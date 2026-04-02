namespace MLIR.Syntax;

/// <summary>
/// Represents the body of an MLIR operation.
/// </summary>
public abstract class OperationBodySyntax
{
    /// <summary>
    /// Writes the operation body to the supplied syntax writer.
    /// </summary>
    public abstract void WriteTo(Text.SyntaxWriter writer, int indentLevel);

    /// <summary>
    /// Rewrites the children of this operation body using the supplied syntax rewriter.
    /// Returns the original body if no children changed.
    /// </summary>
    /// <param name="rewriter">The syntax rewriter to use.</param>
    /// <returns>A rewritten body, or the original if no changes were made.</returns>
    public abstract OperationBodySyntax RewriteChildren(SyntaxRewriter rewriter);
}
