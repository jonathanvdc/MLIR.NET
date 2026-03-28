namespace MLIR.Syntax;

using MLIR.Text;

/// <summary>
/// Represents the body of an MLIR operation.
/// </summary>
public abstract class OperationBodySyntax
{
    /// <summary>
    /// Attempts to project this body into the generic MLIR operation-body shape.
    /// </summary>
    /// <param name="genericBody">
    /// When this method returns, contains the generic projection of the body when the
    /// projection succeeded; otherwise, <see langword="null"/>.
    /// </param>
    /// <returns><see langword="true"/> when a generic projection is available; otherwise, <see langword="false"/>.</returns>
    public abstract bool TryGetGenericBody(out GenericOperationBodySyntax? genericBody);

    /// <summary>
    /// Gets the generic MLIR operation-body projection for this body.
    /// </summary>
    /// <returns>The generic body projection.</returns>
    /// <exception cref="System.InvalidOperationException">
    /// Thrown when the body does not provide a generic projection.
    /// </exception>
    public GenericOperationBodySyntax GetGenericBody()
    {
        if (TryGetGenericBody(out var genericBody))
        {
            return genericBody!;
        }

        throw new System.InvalidOperationException("This operation body does not provide a generic MLIR body projection.");
    }

    /// <summary>
    /// Prints the operation body.
    /// </summary>
    /// <param name="context">The printing context.</param>
    public abstract void Print(OperationBodyPrintingContext context);
}
