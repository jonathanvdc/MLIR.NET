namespace MLIR.Transforms;

using System.Collections.Generic;
using MLIR.Syntax;

/// <summary>
/// Rewrites MLIR concrete syntax trees into their generic operation-body form when a generic projection is available.
/// </summary>
public static class GenericSyntaxBuilder
{
    /// <summary>
    /// Rewrites a module syntax tree into generic MLIR syntax.
    /// </summary>
    /// <param name="module">The module syntax tree to rewrite.</param>
    /// <returns>The rewritten module syntax tree.</returns>
    public static ModuleSyntax BuildModule(ModuleSyntax module)
    {
        var builder = new Builder();
        return builder.BuildModule(module);
    }

    private sealed class Builder
    {
        public ModuleSyntax BuildModule(ModuleSyntax module)
        {
            var operations = new List<OperationSyntax>(module.Operations.Count);
            foreach (var operation in module.Operations)
            {
                operations.Add(BuildOperation(operation));
            }

            return new ModuleSyntax(operations, module.EndOfFileToken);
        }

        private OperationSyntax BuildOperation(OperationSyntax operation)
        {
            if (!operation.TryGetGenericBody(out var genericBody))
            {
                return operation;
            }

            var regions = new List<RegionSyntax>(genericBody!.Regions.Count);
            foreach (var region in genericBody.Regions)
            {
                regions.Add(BuildRegion(region));
            }

            return new OperationSyntax(
                operation.ResultTokens,
                operation.ResultCommaTokens,
                operation.EqualsToken,
                operation.NameToken,
                new GenericOperationBodySyntax(
                    genericBody.OperandList,
                    genericBody.SuccessorList,
                    regions,
                    genericBody.Attributes,
                    genericBody.TypeSignatureColonToken,
                    genericBody.TypeSignatureSyntax));
        }

        private RegionSyntax BuildRegion(RegionSyntax region)
        {
            var blocks = new List<BlockSyntax>(region.Blocks.Count);
            foreach (var block in region.Blocks)
            {
                blocks.Add(BuildBlock(block));
            }

            return new RegionSyntax(region.OpenBraceToken, blocks, region.CloseBraceToken);
        }

        private BlockSyntax BuildBlock(BlockSyntax block)
        {
            var operations = new List<OperationSyntax>(block.Operations.Count);
            foreach (var operation in block.Operations)
            {
                operations.Add(BuildOperation(operation));
            }

            return new BlockSyntax(block.LabelToken, block.Arguments, block.ColonToken, operations);
        }
    }
}
