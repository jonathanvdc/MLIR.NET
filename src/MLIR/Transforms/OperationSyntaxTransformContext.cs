namespace MLIR.Transforms;

using MLIR.Semantics;
using MLIR.Syntax;

/// <summary>
/// Provides dialect assembly rewriters controlled access to semantic-to-syntax transforms.
/// </summary>
public sealed class OperationSyntaxTransformContext
{
    private readonly ConcreteSyntaxBuilder.Builder builder;

    internal OperationSyntaxTransformContext(ConcreteSyntaxBuilder.Builder builder)
    {
        this.builder = builder;
    }

    /// <summary>
    /// Transforms a semantic region to syntax, recursively applying custom assembly rewrites.
    /// </summary>
    public RegionSyntax TransformRegion(Region region)
    {
        return builder.BuildRegion(region);
    }

    /// <summary>
    /// Transforms a semantic block to syntax, recursively applying custom assembly rewrites.
    /// </summary>
    public BlockSyntax TransformBlock(Block block)
    {
        return builder.BuildBlock(block);
    }

    /// <summary>
    /// Transforms a semantic operation to syntax, recursively applying custom assembly rewrites.
    /// </summary>
    public OperationSyntax TransformOperation(Operation operation)
    {
        return builder.BuildOperation(operation);
    }

    /// <summary>
    /// Builds the generic MLIR operation body for the supplied operation, recursively rewriting nested regions.
    /// </summary>
    public GenericOperationBodySyntax TransformGenericBody(Operation operation)
    {
        return builder.BuildGenericBody(operation);
    }

    /// <summary>
    /// Replaces the body of an operation while preserving its outer shell tokens.
    /// </summary>
    public OperationSyntax WithBody(Operation operation, OperationBodySyntax body)
    {
        return builder.WithBody(operation, body);
    }

    /// <summary>
    /// Rewrites an operation while preserving its outer shell tokens except where replacements are supplied.
    /// </summary>
    public OperationSyntax RewriteOperation(Operation operation, OperationBodySyntax body, SyntaxToken? nameToken = null)
    {
        return builder.RewriteOperation(operation, body, nameToken);
    }
}
