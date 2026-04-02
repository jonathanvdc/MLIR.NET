namespace MLIR.Semantics;

using System;
using System.Collections.Generic;

/// <summary>
/// A base class for AST rewriters that transform semantic nodes.
/// Provides default implementations that traverse the AST and reconstruct nodes only when their children change.
/// </summary>
/// <remarks>
/// Override specific <c>Visit</c> methods to intercept and transform nodes of interest.
/// The default implementations preserve the original nodes when nothing changes, avoiding unnecessary allocations.
/// To participate in default traversal, concrete <see cref="Operation"/> subclasses should override
/// <see cref="Operation.RewriteChildren"/>.
/// </remarks>
public class SemanticRewriter
{
    /// <summary>
    /// Visits a module, rewriting its top-level operations.
    /// </summary>
    /// <param name="module">The module to visit.</param>
    /// <returns>A rewritten module, or the original if no changes were made.</returns>
    public virtual Module VisitModule(Module module)
    {
        var ops = RewriteList(module.Operations, VisitOperation);
        if (ReferenceEquals(ops, module.Operations))
            return module;
        return new Module(module.Syntax, ops, module.AssemblyDiagnostics);
    }

    /// <summary>
    /// Visits an operation, rewriting its children via <see cref="Operation.RewriteChildren"/>.
    /// </summary>
    /// <param name="operation">The operation to visit.</param>
    /// <returns>A rewritten operation, or the original if no changes were made.</returns>
    public virtual Operation VisitOperation(Operation operation)
    {
        return operation.RewriteChildren(this);
    }

    /// <summary>
    /// Visits a region, rewriting its blocks.
    /// </summary>
    /// <param name="region">The region to visit.</param>
    /// <returns>A rewritten region, or the original if no changes were made.</returns>
    public virtual Region VisitRegion(Region region)
    {
        var blocks = RewriteList(region.Blocks, VisitBlock);
        if (ReferenceEquals(blocks, region.Blocks))
            return region;
        return new Region(region.Syntax, blocks);
    }

    /// <summary>
    /// Visits a block, rewriting its arguments and operations.
    /// </summary>
    /// <param name="block">The block to visit.</param>
    /// <returns>A rewritten block, or the original if no changes were made.</returns>
    public virtual Block VisitBlock(Block block)
    {
        var args = RewriteList(block.Arguments, VisitBlockArgument);
        var ops = RewriteList(block.Operations, VisitOperation);
        if (ReferenceEquals(args, block.Arguments) && ReferenceEquals(ops, block.Operations))
            return block;
        return block.Syntax != null
            ? new Block(block.Syntax, args, ops)
            : new Block(block.LabelReference, args, ops);
    }

    /// <summary>
    /// Visits a block argument, rewriting its type reference.
    /// </summary>
    /// <param name="argument">The block argument to visit.</param>
    /// <returns>A rewritten block argument, or the original if no changes were made.</returns>
    public virtual BlockArgument VisitBlockArgument(BlockArgument argument)
    {
        var typeRef = VisitTypeReference(argument.TypeReference);
        if (ReferenceEquals(typeRef, argument.TypeReference))
            return argument;
        return new BlockArgument(argument.Syntax, typeRef);
    }

    /// <summary>
    /// Visits a type reference. The default implementation returns the type reference unchanged.
    /// Override this to transform or replace type references.
    /// </summary>
    /// <param name="typeReference">The type reference to visit.</param>
    /// <returns>A rewritten type reference, or the original if no changes were made.</returns>
    public virtual TypeReference VisitTypeReference(TypeReference typeReference)
    {
        return typeReference;
    }

    /// <summary>
    /// Visits an attribute value. The default implementation returns the attribute value unchanged.
    /// Override this to transform or replace attribute values.
    /// </summary>
    /// <param name="attributeValue">The attribute value to visit.</param>
    /// <returns>A rewritten attribute value, or the original if no changes were made.</returns>
    public virtual AttributeValue VisitAttributeValue(AttributeValue attributeValue)
    {
        return attributeValue;
    }

    /// <summary>
    /// Visits a named attribute, rewriting its value.
    /// </summary>
    /// <param name="attribute">The named attribute to visit.</param>
    /// <returns>A rewritten named attribute, or the original if no changes were made.</returns>
    public virtual NamedAttribute VisitNamedAttribute(NamedAttribute attribute)
    {
        var value = VisitAttributeValue(attribute.Value);
        if (ReferenceEquals(value, attribute.Value))
            return attribute;
        return new NamedAttribute(attribute.Name, value);
    }

    /// <summary>
    /// Visits a named attribute collection, rewriting each attribute.
    /// </summary>
    /// <param name="collection">The attribute collection to visit.</param>
    /// <returns>A rewritten collection, or the original if no changes were made.</returns>
    public virtual NamedAttributeCollection VisitNamedAttributeCollection(NamedAttributeCollection collection)
    {
        NamedAttribute[]? newItems = null;
        for (int i = 0; i < collection.Count; i++)
        {
            var original = collection[i];
            var rewritten = VisitNamedAttribute(original);
            if (newItems != null)
            {
                newItems[i] = rewritten;
            }
            else if (!ReferenceEquals(original, rewritten))
            {
                newItems = new NamedAttribute[collection.Count];
                for (int j = 0; j < i; j++)
                    newItems[j] = collection[j];
                newItems[i] = rewritten;
            }
        }
        return newItems != null ? new NamedAttributeCollection(newItems) : collection;
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
}
