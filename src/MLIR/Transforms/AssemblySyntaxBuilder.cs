namespace MLIR.Transforms;

using System.Collections.Generic;
using MLIR.Construction;
using MLIR.Semantics;
using MLIR.Syntax;

/// <summary>
/// Builds concrete syntax trees from semantic MLIR modules, optionally rewriting operations into custom assembly forms.
/// </summary>
public static class AssemblySyntaxBuilder
{
    /// <summary>
    /// Builds a concrete syntax tree for the supplied semantic module.
    /// </summary>
    public static ModuleSyntax BuildModule(Module module)
    {
        var builder = new Builder();
        return builder.BuildModule(module);
    }

    internal sealed class Builder
    {
        public ModuleSyntax BuildModule(Module module)
        {
            var operations = new List<OperationSyntax>(module.Operations.Count);
            foreach (var operation in module.Operations)
            {
                operations.Add(BuildOperation(operation));
            }

            return new ModuleSyntax(operations, module.Syntax.EndOfFileToken);
        }

        public OperationSyntax BuildOperation(Operation operation)
        {
            return operation.Definition?.AssemblyFormat?.Rewrite(operation, new OperationSyntaxTransformContext(this))
                ?? RewriteOperation(operation, BuildGenericBody(operation));
        }

        public OperationSyntax WithBody(Operation operation, OperationBodySyntax body)
        {
            return RewriteOperation(operation, body);
        }

        public OperationSyntax RewriteOperation(Operation operation, OperationBodySyntax body, SyntaxToken? nameToken = null)
        {
            return new OperationSyntax(
                operation.Syntax.ResultTokens,
                operation.Syntax.ResultCommaTokens,
                operation.Syntax.EqualsToken,
                nameToken ?? operation.Syntax.NameToken,
                body);
        }

        public GenericOperationBodySyntax BuildGenericBody(Operation operation)
        {
            var genericBody = operation.GetGenericBody();
            var regions = new List<RegionSyntax>(operation.Regions.Count);
            foreach (var region in operation.Regions)
            {
                regions.Add(BuildRegion(region));
            }

            return new GenericOperationBodySyntax(
                genericBody.OperandList,
                genericBody.SuccessorList,
                regions,
                genericBody.Attributes,
                genericBody.TypeSignatureColonToken,
                genericBody.TypeSignatureSyntax);
        }

        public RegionSyntax BuildRegion(Region region)
        {
            var blocks = new List<BlockSyntax>(region.Blocks.Count);
            foreach (var block in region.Blocks)
            {
                blocks.Add(BuildBlock(block));
            }

            return new RegionSyntax(region.Syntax.OpenBraceToken, blocks, region.Syntax.CloseBraceToken);
        }

        public BlockSyntax BuildBlock(Block block)
        {
            var operations = new List<OperationSyntax>(block.Operations.Count);
            foreach (var operation in block.Operations)
            {
                operations.Add(BuildOperation(operation));
            }

            return new BlockSyntax(
                block.Syntax.LabelToken,
                block.Syntax.Arguments,
                block.Syntax.ColonToken,
                operations);
        }
    }
}
