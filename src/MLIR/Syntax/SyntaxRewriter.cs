namespace MLIR.Syntax;

using System;
using System.Collections.Generic;

/// <summary>
/// A base class for CST rewriters that transform syntax nodes.
/// Provides default implementations that traverse the CST and reconstruct nodes only when their children change.
/// </summary>
/// <remarks>
/// Override specific <c>Visit</c> methods to intercept and transform nodes of interest.
/// The default implementations preserve the original nodes when nothing changes, avoiding unnecessary allocations.
/// To participate in default traversal, concrete <see cref="OperationBodySyntax"/> subclasses should override
/// <see cref="OperationBodySyntax.RewriteChildren"/>.
/// </remarks>
public class SyntaxRewriter
{
    /// <summary>
    /// Visits a module, rewriting its top-level operations.
    /// </summary>
    /// <param name="module">The module to visit.</param>
    /// <returns>A rewritten module, or the original if no changes were made.</returns>
    public virtual ModuleSyntax VisitModule(ModuleSyntax module)
    {
        var ops = RewriteList(module.Operations, VisitOperation);
        if (ReferenceEquals(ops, module.Operations))
            return module;
        return new ModuleSyntax(ops, module.EndOfFileToken);
    }

    /// <summary>
    /// Visits an operation, rewriting its body via <see cref="OperationBodySyntax.RewriteChildren"/>.
    /// </summary>
    /// <param name="operation">The operation to visit.</param>
    /// <returns>A rewritten operation, or the original if no changes were made.</returns>
    public virtual OperationSyntax VisitOperation(OperationSyntax operation)
    {
        var newBody = VisitOperationBody(operation.Body);
        if (ReferenceEquals(newBody, operation.Body))
            return operation;
        return new OperationSyntax(
            operation.ResultTokens,
            operation.ResultCommaTokens,
            operation.EqualsToken,
            operation.NameToken,
            newBody);
    }

    /// <summary>
    /// Visits an operation body, rewriting its children via <see cref="OperationBodySyntax.RewriteChildren"/>.
    /// </summary>
    /// <param name="body">The operation body to visit.</param>
    /// <returns>A rewritten body, or the original if no changes were made.</returns>
    public virtual OperationBodySyntax VisitOperationBody(OperationBodySyntax body)
    {
        return body.RewriteChildren(this);
    }

    /// <summary>
    /// Visits a region, rewriting its blocks.
    /// </summary>
    /// <param name="region">The region to visit.</param>
    /// <returns>A rewritten region, or the original if no changes were made.</returns>
    public virtual RegionSyntax VisitRegion(RegionSyntax region)
    {
        var blocks = RewriteList(region.Blocks, VisitBlock);
        if (ReferenceEquals(blocks, region.Blocks))
            return region;
        return new RegionSyntax(region.OpenBraceToken, blocks, region.CloseBraceToken);
    }

    /// <summary>
    /// Visits a block, rewriting its arguments and operations.
    /// </summary>
    /// <param name="block">The block to visit.</param>
    /// <returns>A rewritten block, or the original if no changes were made.</returns>
    public virtual BlockSyntax VisitBlock(BlockSyntax block)
    {
        var args = RewriteDelimitedList(block.Arguments, VisitBlockArgument);
        var ops = RewriteList(block.Operations, VisitOperation);
        if (ReferenceEquals(args, block.Arguments) && ReferenceEquals(ops, block.Operations))
            return block;
        return new BlockSyntax(block.LabelToken, args, block.ColonToken, ops);
    }

    /// <summary>
    /// Visits a block argument, rewriting its type syntax.
    /// </summary>
    /// <param name="argument">The block argument to visit.</param>
    /// <returns>A rewritten block argument, or the original if no changes were made.</returns>
    public virtual BlockArgumentSyntax VisitBlockArgument(BlockArgumentSyntax argument)
    {
        var newType = VisitTypeSyntax(argument.TypeSyntax);
        if (ReferenceEquals(newType, argument.TypeSyntax))
            return argument;
        return new BlockArgumentSyntax(argument.NameToken, argument.ColonToken, newType);
    }

    /// <summary>
    /// Visits a named attribute, rewriting its value syntax.
    /// </summary>
    /// <param name="attribute">The named attribute to visit.</param>
    /// <returns>A rewritten named attribute, or the original if no changes were made.</returns>
    public virtual NamedAttributeSyntax VisitNamedAttribute(NamedAttributeSyntax attribute)
    {
        var newValue = VisitAttributeValue(attribute.ValueSyntax);
        if (ReferenceEquals(newValue, attribute.ValueSyntax))
            return attribute;
        return new NamedAttributeSyntax(attribute.NameToken, attribute.EqualsToken, newValue);
    }

    /// <summary>
    /// Visits an attribute value syntax node. The default implementation returns the node unchanged.
    /// Override this to transform or replace attribute value syntax.
    /// </summary>
    /// <param name="attributeValue">The attribute value syntax to visit.</param>
    /// <returns>A rewritten attribute value syntax, or the original if no changes were made.</returns>
    public virtual AttributeValueSyntax VisitAttributeValue(AttributeValueSyntax attributeValue)
    {
        return attributeValue;
    }

    /// <summary>
    /// Visits a type syntax node. The default implementation returns the node unchanged.
    /// Override this to transform or replace type syntax.
    /// </summary>
    /// <param name="typeSyntax">The type syntax to visit.</param>
    /// <returns>A rewritten type syntax, or the original if no changes were made.</returns>
    public virtual TypeSyntax VisitTypeSyntax(TypeSyntax typeSyntax)
    {
        return typeSyntax;
    }

    /// <summary>
    /// Visits each region in a list, rewriting changed regions and preserving unchanged ones.
    /// </summary>
    /// <param name="regions">The list of regions to visit.</param>
    /// <returns>A new list if any region changed, or the original list otherwise.</returns>
    public virtual IReadOnlyList<RegionSyntax> VisitRegionList(IReadOnlyList<RegionSyntax> regions)
    {
        return RewriteList(regions, VisitRegion);
    }

    /// <summary>
    /// Visits each named attribute in a delimited list, rewriting changed attributes and preserving unchanged ones.
    /// </summary>
    /// <param name="list">The delimited named attribute list to visit.</param>
    /// <returns>A new list if any attribute changed, or the original list otherwise.</returns>
    public virtual DelimitedSyntaxList<NamedAttributeSyntax> VisitNamedAttributeList(DelimitedSyntaxList<NamedAttributeSyntax> list)
    {
        return RewriteDelimitedList(list, VisitNamedAttribute);
    }

    /// <summary>
    /// Rewrites a read-only list by applying a visit function to each element.
    /// Returns the original list instance if no elements were changed.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="list">The list to rewrite.</param>
    /// <param name="visit">The function to apply to each element.</param>
    /// <returns>A new list if any element changed, or the original list if nothing changed.</returns>
    protected static IReadOnlyList<T> RewriteList<T>(IReadOnlyList<T> list, Func<T, T> visit)
        where T : class
    {
        List<T>? newList = null;
        for (int i = 0; i < list.Count; i++)
        {
            var original = list[i];
            var rewritten = visit(original);
            if (newList != null)
            {
                newList.Add(rewritten);
            }
            else if (!ReferenceEquals(original, rewritten))
            {
                newList = new List<T>(list.Count);
                for (int j = 0; j < i; j++)
                    newList.Add(list[j]);
                newList.Add(rewritten);
            }
        }

        return newList ?? list;
    }

    /// <summary>
    /// Rewrites a delimited list by applying a visit function to each item.
    /// Returns the original list instance if no items were changed.
    /// Delimiter tokens (open, separators, close) are always preserved as-is.
    /// </summary>
    /// <typeparam name="T">The item type.</typeparam>
    /// <param name="list">The delimited list to rewrite.</param>
    /// <param name="visit">The function to apply to each item.</param>
    /// <returns>A new list if any item changed, or the original list if nothing changed.</returns>
    protected static DelimitedSyntaxList<T> RewriteDelimitedList<T>(DelimitedSyntaxList<T> list, Func<T, T> visit)
        where T : class
    {
        var newItems = RewriteList(list.Items, visit);
        if (ReferenceEquals(newItems, list.Items))
            return list;
        return new DelimitedSyntaxList<T>(list.OpenToken, newItems, list.SeparatorTokens, list.CloseToken);
    }
}
