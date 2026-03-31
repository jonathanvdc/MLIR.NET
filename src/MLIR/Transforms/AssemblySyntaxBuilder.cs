namespace MLIR.Transforms;

using System;
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
            if (operation.Syntax != null)
            {
                return new OperationSyntax(
                    operation.Syntax.ResultTokens,
                    operation.Syntax.ResultCommaTokens,
                    operation.Syntax.EqualsToken,
                    nameToken ?? operation.Syntax.NameToken,
                    body);
            }

            // Synthesize tokens for a synthetic operation with no source syntax.
            var results = operation.Results;
            var resultTokens = new List<SyntaxToken>(results.Count);
            foreach (var result in results)
            {
                resultTokens.Add(new SyntaxToken(result));
            }

            var resultCommaTokens = new List<SyntaxToken>(Math.Max(0, results.Count - 1));
            for (var i = 1; i < results.Count; i++)
            {
                resultCommaTokens.Add(new SyntaxToken(","));
            }

            var equalsToken = results.Count > 0 ? (SyntaxToken?)new SyntaxToken("=") : null;
            return new OperationSyntax(
                resultTokens,
                resultCommaTokens,
                equalsToken,
                nameToken ?? new SyntaxToken(operation.Name),
                body);
        }

        public GenericOperationBodySyntax BuildGenericBody(Operation operation)
        {
            var genericBody = GetGenericBody(operation);
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

        private GenericOperationBodySyntax GetGenericBody(Operation operation)
        {
            if (operation.Syntax?.Body is GenericOperationBodySyntax genericBody)
            {
                return genericBody;
            }

            // TODO: preserve tokens
            return (GenericOperationBodySyntax)Factory.Op(
                operation.Name,
                operation.Results,
                operation.Operands,
                operation.Successors,
                operation.Regions.Select(BuildRegion).ToList(),
                operation.Attributes.Select(BuildNamedAttribute).ToList(),
                operation.TypeSignatureReference != null ? operation.TypeSignatureReference.Syntax : null
            ).Body;
        }

        public NamedAttributeSyntax BuildNamedAttribute(NamedAttribute attribute)
        {
            if (attribute.Syntax != null)
            {
                return attribute.Syntax;
            }

            // Synthesize an attribute syntax for a synthetic attribute with no source syntax.
            return new NamedAttributeSyntax(
                new SyntaxToken(attribute.Name),
                new SyntaxToken("="),
                BuildAttributeValue(attribute.Value));
        }

        public AttributeValueSyntax BuildAttributeValue(AttributeValue attributeValue)
        {
            if (attributeValue.Syntax != null)
            {
                return attributeValue.Syntax;
            }

            if (attributeValue is UnknownAttributeValue unknownAttributeValue)
            {
                // For unknown attribute values, we want to preserve the original syntax if possible, even if it was not recognized as a valid attribute value.
                return unknownAttributeValue.Syntax!;
            }

            throw new InvalidOperationException($"Cannot build syntax for unrecognized attribute value of type {attributeValue.GetType().FullName}.");
        }

        public RegionSyntax BuildRegion(Region region)
        {
            var blocks = new List<BlockSyntax>(region.Blocks.Count);
            foreach (var block in region.Blocks)
            {
                blocks.Add(BuildBlock(block));
            }

            return new RegionSyntax(
                region.Syntax?.OpenBraceToken ?? new SyntaxToken("{"),
                blocks,
                region.Syntax?.CloseBraceToken ?? new SyntaxToken("}"));
        }

        public BlockSyntax BuildBlock(Block block)
        {
            var operations = new List<OperationSyntax>(block.Operations.Count);
            foreach (var operation in block.Operations)
            {
                operations.Add(BuildOperation(operation));
            }

            if (block.Syntax != null)
            {
                return new BlockSyntax(
                    block.Syntax.LabelToken,
                    block.Syntax.Arguments,
                    block.Syntax.ColonToken,
                    operations);
            }

            // Synthesize a block syntax for a synthetic block with no source syntax.
            // Use "^entry" as the fallback label: the parser uses this synthetic label for implicit
            // entry blocks, and BlockSyntax.WriteTo omits it during printing when there are no arguments.
            return new BlockSyntax(block.Label, [], operations);
        }
    }
}
