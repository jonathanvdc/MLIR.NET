namespace MLIR.Syntax;

/// <summary>
/// Represents the body of an MLIR operation.
/// </summary>
public abstract class OperationBodySyntax : SyntaxNode
{
    /// <summary>
    /// Writes the operation body to the supplied syntax writer.
    /// </summary>
    public abstract void WriteTo(Text.SyntaxWriter writer, int indentLevel);

    /// <inheritdoc/>
    public override void WriteTo(Text.SyntaxWriter writer)
    {
        WriteTo(writer, indentLevel: 0);
    }
}
