namespace MLIR.Transforms;

using System;
using System.Collections.Generic;
using System.Linq;
using MLIR.Construction;
using MLIR.Semantics;
using MLIR.Syntax;
using MLIR.Syntax.Types.Collections;

/// <summary>
/// Builds concrete syntax trees from semantic MLIR modules, synthesizing missing nodes when needed and honoring
/// custom assembly rewrites when requested.
/// </summary>
public static class ConcreteSyntaxBuilder
{
    /// <summary>
    /// Builds a concrete syntax tree for the supplied semantic module according to the provided options.
    /// </summary>
    public static ModuleSyntax BuildModule(Module module, ConcreteSyntaxBuilderOptions? options = null)
    {
        var builder = new Builder(options ?? new ConcreteSyntaxBuilderOptions());
        return builder.BuildModule(module);
    }

    /// <summary>
    /// Configures how <see cref="ConcreteSyntaxBuilder"/> prefers custom assembly formats and handles existing syntax.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="ConcreteSyntaxBuilderOptions"/> class with the specified preferences.
    /// </remarks>
    /// <param name="operationSyntaxPreference">The preferred format for emitting operations.</param>
    /// <param name="existingSyntaxHandling">How existing syntax should be handled.</param>
    public sealed class ConcreteSyntaxBuilderOptions(
        OperationSyntaxPreference operationSyntaxPreference = OperationSyntaxPreference.PreferCustomAssembly,
        ExistingSyntaxHandling existingSyntaxHandling = ExistingSyntaxHandling.PreserveExistingSyntax)
    {

        /// <summary>
        /// Gets or sets the preferred format for emitting operations.
        /// </summary>
        public OperationSyntaxPreference OperationSyntaxPreference { get; } = operationSyntaxPreference;

        /// <summary>
        /// Gets or sets whether existing CST nodes should be preserved or rebuilt to match the configured preferences.
        /// </summary>
        public ExistingSyntaxHandling ExistingSyntaxHandling { get; } = existingSyntaxHandling;
    }

    /// <summary>
    /// Determines whether to favor dialect-specific assembly rewrites or always emit the generic format when building syntax.
    /// </summary>
    public enum OperationSyntaxPreference
    {
        /// <summary>
        /// Use custom assembly rewrites when available; fall back to generic syntax otherwise.
        /// </summary>
        PreferCustomAssembly,

        /// <summary>
        /// Always emit generic operation syntax, ignoring custom assembly rewrites.
        /// </summary>
        PreferGeneric
    }

    /// <summary>
    /// Controls how existing concrete syntax trees attached to semantic nodes are handled.
    /// </summary>
    public enum ExistingSyntaxHandling
    {
        /// <summary>
        /// Keep existing CST nodes unchanged when they already exist on semantic nodes.
        /// </summary>
        PreserveExistingSyntax,

        /// <summary>
        /// Rebuild CST nodes so they match the current preferences even if syntax was previously attached.
        /// </summary>
        ReplaceExistingSyntax
    }

    internal sealed class Builder
    {
        private readonly ConcreteSyntaxBuilderOptions options;

        internal Builder(ConcreteSyntaxBuilderOptions options)
        {
            this.options = options;
        }

        public ModuleSyntax BuildModule(Module module)
        {
            var operations = new List<OperationSyntax>(module.Operations.Count);
            foreach (var operation in module.Operations)
            {
                operations.Add(BuildOperation(operation));
            }

            return RewriteIfNeeded(new ModuleSyntax(operations, module.Syntax.EndOfFileToken));
        }

        public OperationSyntax BuildOperation(Operation operation)
        {
            if (operation.Syntax != null && options.ExistingSyntaxHandling == ExistingSyntaxHandling.PreserveExistingSyntax)
            {
                return operation.Syntax;
            }

            var assemblyFormat = operation.Definition?.AssemblyFormat;
            if (options.OperationSyntaxPreference == OperationSyntaxPreference.PreferCustomAssembly && assemblyFormat != null)
            {
                return assemblyFormat.BuildCustomAssemblySyntax(operation, new ConcreteSyntaxBuilderContext(this));
            }

            var body = BuildGenericBody(operation);
            var shouldPreserveOuterTokens = options.ExistingSyntaxHandling == ExistingSyntaxHandling.PreserveExistingSyntax
                || (assemblyFormat == null && operation.Syntax != null);
            return RewriteIfNeeded(RewriteOperation(
                operation,
                body,
                preserveOuterTokens: shouldPreserveOuterTokens));
        }

        public OperationSyntax WithBody(Operation operation, OperationBodySyntax body)
        {
            return RewriteIfNeeded(RewriteOperation(operation, body));
        }

        public OperationSyntax RewriteOperation(
            Operation operation,
            OperationBodySyntax body,
            SyntaxToken? nameToken = null,
            bool preserveOuterTokens = true)
        {
            if (preserveOuterTokens && operation.Syntax != null)
            {
                return new OperationSyntax(
                    operation.Syntax.ResultList,
                    operation.Syntax.EqualsToken,
                    nameToken ?? operation.Syntax.NameToken,
                    body);
            }

            var results = operation.Results;
            var resultItems = new List<SyntaxToken>(results.Count);
            foreach (var result in results)
            {
                resultItems.Add(SyntaxTokenFactory.SsaName(result.Name));
            }

            var resultSeparators = new List<SyntaxToken>(Math.Max(0, results.Count - 1));
            for (var i = 1; i < results.Count; i++)
            {
                resultSeparators.Add(SyntaxTokenFactory.Comma());
            }

            var equalsToken = results.Count > 0 ? (SyntaxToken?)SyntaxTokenFactory.Equal() : null;
            return new OperationSyntax(
                new SeparatedSyntaxList<SyntaxToken>(resultItems, resultSeparators),
                equalsToken,
                nameToken ?? SyntaxTokenFactory.StringLiteral(QuoteIfNeeded(operation.Name)),
                body);
        }

        public SyntaxToken NormalizeToken(SyntaxToken token)
        {
            return options.ExistingSyntaxHandling == ExistingSyntaxHandling.ReplaceExistingSyntax
                ? new SyntaxToken(token.TokenKind, token.Text)
                : token;
        }

        public GenericOperationBodySyntax BuildGenericBody(Operation operation)
        {
            var genericBody = GetGenericBody(operation);
            var regions = new List<RegionSyntax>(operation.Regions.Count);
            foreach (var region in operation.Regions)
            {
                regions.Add(BuildRegion(region));
            }

            var attributes = options.ExistingSyntaxHandling == ExistingSyntaxHandling.PreserveExistingSyntax
                ? genericBody.Attributes
                : BuildAttrDict(operation.Attributes);
            var typeSignatureSyntax = operation.TypeSignatureReference != null
                ? BuildTypeReference(operation.TypeSignatureReference)
                : null;

            return RewriteIfNeeded(new GenericOperationBodySyntax(
                genericBody.OperandList,
                genericBody.SuccessorList,
                regions,
                attributes,
                genericBody.TypeSignatureColonToken,
                typeSignatureSyntax));
        }

        private GenericOperationBodySyntax GetGenericBody(Operation operation)
        {
            if (operation.Syntax?.Body is GenericOperationBodySyntax genericBody)
            {
                return genericBody;
            }

            return (GenericOperationBodySyntax)Factory.Op(
                operation.Name,
                operation.Results.Select(static result => result.Name).ToList(),
                operation.NonNullOperandValues.Select(static operand => operand.Name).ToList(),
                operation.Successors.Select(static successor => successor.Label).ToList(),
                operation.Regions.Select(BuildRegion).ToList(),
                operation.Attributes.Select(BuildNamedAttribute).ToList(),
                operation.TypeSignatureReference != null ? BuildTypeReference(operation.TypeSignatureReference) : null)
                .Body;
        }

        public NamedAttributeSyntax BuildNamedAttribute(NamedAttribute attribute)
        {
            if (attribute.Syntax != null)
            {
                return RewriteIfNeeded(attribute.Syntax);
            }

            return new NamedAttributeSyntax(
                SyntaxTokenFactory.Identifier(attribute.Name),
                SyntaxTokenFactory.Equal(),
                BuildAttributeValue(attribute.Value));
        }

        public AttributeValueSyntax BuildAttributeValue(AttributeValue attributeValue)
        {
            if (attributeValue.Definition?.AssemblyFormat != null &&
                (options.ExistingSyntaxHandling == ExistingSyntaxHandling.ReplaceExistingSyntax || attributeValue.Syntax == null))
            {
                return attributeValue.Definition.AssemblyFormat.BuildCustomAssemblySyntax(attributeValue, new ConcreteSyntaxBuilderContext(this));
            }

            if (attributeValue.Syntax != null)
            {
                return attributeValue.Syntax;
            }

            if (attributeValue is UnknownAttributeValue unknownAttributeValue)
            {
                return RewriteIfNeeded(unknownAttributeValue.Syntax!);
            }

            throw new InvalidOperationException($"Cannot build syntax for unrecognized attribute value of type {attributeValue.GetType().FullName}.");
        }

        public TypeSyntax BuildTypeReference(TypeReference typeReference)
        {
            if (typeReference.Definition?.AssemblyFormat != null &&
                (options.ExistingSyntaxHandling == ExistingSyntaxHandling.ReplaceExistingSyntax || typeReference.Syntax == null))
            {
                return RewriteIfNeeded(typeReference.Definition.AssemblyFormat.BuildCustomAssemblySyntax(typeReference, new ConcreteSyntaxBuilderContext(this)));
            }

            if (typeReference.Syntax != null)
            {
                return RewriteIfNeeded(typeReference.Syntax);
            }

            throw new InvalidOperationException($"Cannot build syntax for unrecognized type reference of type {typeReference.GetType().FullName}.");
        }

        /// <summary>
        /// Builds a delimited attribute-dictionary syntax list from the supplied collection.
        /// Attributes are rendered as <c>{ name = value, ... }</c>; an empty collection
        /// produces a list with no open token (representing an absent attribute dictionary).
        /// </summary>
        public DelimitedSyntaxList<NamedAttributeSyntax> BuildAttrDict(NamedAttributeCollection attributes)
        {
            if (attributes.Count == 0)
            {
                return new DelimitedSyntaxList<NamedAttributeSyntax>(null, [], [], null);
            }

            var items = new List<NamedAttributeSyntax>(attributes.Count);
            var separators = new List<SyntaxToken>(attributes.Count - 1);
            for (var i = 0; i < attributes.Count; i++)
            {
                if (i > 0)
                {
                    separators.Add(SyntaxTokenFactory.Comma());
                }

                items.Add(BuildNamedAttribute(attributes[i]));
            }

            return new DelimitedSyntaxList<NamedAttributeSyntax>(SyntaxTokenFactory.LBrace(), items, separators, SyntaxTokenFactory.RBrace());
        }

        public RegionSyntax BuildRegion(Region region)
        {
            var blocks = new List<BlockSyntax>(region.Blocks.Count);
            foreach (var block in region.Blocks)
            {
                blocks.Add(BuildBlock(block));
            }

            return RewriteIfNeeded(new RegionSyntax(
                region.Syntax?.OpenBraceToken ?? SyntaxTokenFactory.LBrace(),
                blocks,
                region.Syntax?.CloseBraceToken ?? SyntaxTokenFactory.RBrace()));
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
                if (options.ExistingSyntaxHandling == ExistingSyntaxHandling.PreserveExistingSyntax)
                {
                    return RewriteIfNeeded(new BlockSyntax(
                        block.Syntax.LabelToken,
                        block.Syntax.Arguments,
                        block.Syntax.ColonToken,
                        operations));
                }

                var arguments = new List<BlockArgumentSyntax>(block.Arguments.Count);
                foreach (var argument in block.Arguments)
                {
                    arguments.Add(new BlockArgumentSyntax(argument.Syntax.NameToken, argument.Syntax.ColonToken, BuildTypeReference(argument.Type)));
                }

                return RewriteIfNeeded(new BlockSyntax(
                    block.Syntax.LabelToken,
                    new DelimitedSyntaxList<BlockArgumentSyntax>(
                        block.Syntax.Arguments.OpenToken,
                        arguments,
                        block.Syntax.Arguments.SeparatorTokens,
                        block.Syntax.Arguments.CloseToken),
                    block.Syntax.ColonToken,
                    operations));
            }

            var syntheticArguments = new List<BlockArgumentSyntax>(block.Arguments.Count);
            foreach (var argument in block.Arguments)
            {
                syntheticArguments.Add(new BlockArgumentSyntax(SyntaxTokenFactory.SsaName(argument.Name), SyntaxTokenFactory.Colon(), BuildTypeReference(argument.Type)));
            }

            return RewriteIfNeeded(new BlockSyntax(block.Label, syntheticArguments, operations));
        }

        private static string QuoteIfNeeded(string name)
        {
            return name.Length > 0 && name[0] == '"' ? name : "\"" + name + "\"";
        }

        private TNode RewriteIfNeeded<TNode>(TNode node)
            where TNode : SyntaxNode
        {
            return options.ExistingSyntaxHandling == ExistingSyntaxHandling.ReplaceExistingSyntax
                ? TriviaStrippingSyntaxRewriter.Instance.Visit(node)
                : node;
        }
    }
}
