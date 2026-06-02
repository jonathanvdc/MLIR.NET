namespace MLIR.Transforms;

using System;
using System.Linq;
using MLIR.Dialects.Builtin;
using MLIR.Semantics;
using MLIR.Syntax;
using MLIR.Syntax.Types.Collections;

/// <summary>
/// Provides dialect assembly rewriters controlled access to semantic-to-syntax transforms.
/// </summary>
public sealed class ConcreteSyntaxBuilderContext
{
    private readonly ConcreteSyntaxBuilder.Builder builder;

    internal ConcreteSyntaxBuilderContext(ConcreteSyntaxBuilder.Builder builder)
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
    public OperationSyntax RewriteOperation(Operation operation, OperationBodySyntax body, Token? nameToken = null)
    {
        return builder.RewriteOperation(operation, body, nameToken);
    }

    /// <summary>
    /// Builds an <see cref="AttributeValueSyntax"/> for the supplied semantic attribute value,
    /// reusing its original syntax when available.
    /// </summary>
    public AttributeValueSyntax BuildAttributeValueSyntax(AttributeValue value)
    {
        return builder.BuildAttributeValue(value);
    }

    /// <summary>
    /// Builds a <see cref="NamedAttributeSyntax"/> for the supplied semantic named attribute.
    /// </summary>
    public NamedAttributeSyntax BuildNamedAttributeSyntax(NamedAttribute attribute)
    {
        return builder.BuildNamedAttribute(attribute);
    }

    /// <summary>
    /// Normalizes a token before it is reused in rebuilt syntax.
    /// </summary>
    /// <remarks>
    /// When rebuilding existing syntax, this strips trivia so source indentation and line breaks
    /// do not leak into synthesized assembly. PreserveExistingSyntax keeps the token unchanged.
    /// </remarks>
    public Token NormalizeToken(Token token)
    {
        return builder.NormalizeToken(token);
    }

    /// <summary>
    /// Builds a <see cref="TypeSyntax"/> for the supplied semantic type reference,
    /// reusing its original syntax when available.
    /// </summary>
    public TypeSyntax BuildTypeSyntax(TypeReference type)
    {
        return builder.BuildTypeReference(type);
    }

    /// <summary>
    /// Builds a bare comma-separated type list for variadic <c>type($operand)</c>
    /// directives. Function type signatures contribute their input types; any other
    /// type reference is treated as a single-item list for compatibility with existing
    /// single-operand assembly formats.
    /// </summary>
    public SeparatedSyntaxList<TypeSyntax> BuildTypeListSyntax(TypeReference? type)
    {
        if (type is null)
        {
            return SeparatedSyntaxList<TypeSyntax>.Empty;
        }

        var typeSyntax = builder.BuildTypeReference(type);
        if (typeSyntax is FunctionTypeSyntax functionType)
        {
            return new SeparatedSyntaxList<TypeSyntax>(
                functionType.InputTypes.Items,
                functionType.InputTypes.SeparatorTokens);
        }

        if (type is FunctionType functionTypeReference)
        {
            var items = functionTypeReference.Inputs.Select(builder.BuildTypeReference).ToArray();
            return new SeparatedSyntaxList<TypeSyntax>(
                items,
                Enumerable.Range(0, Math.Max(0, items.Length - 1)).Select(static _ => TokenFactory.Comma()).ToArray());
        }

        return new SeparatedSyntaxList<TypeSyntax>([typeSyntax], []);
    }

    /// <summary>
    /// Builds a delimited attribute-dictionary syntax list from the supplied collection.
    /// An empty collection produces a list with no open token (absent attribute dictionary).
    /// </summary>
    public DelimitedSyntaxList<NamedAttributeSyntax> BuildAttrDict(NamedAttributeCollection attributes)
    {
        return builder.BuildAttrDict(attributes);
    }

    /// <summary>
    /// Builds an optional keyword-prefixed attribute dictionary for an
    /// <c>attr-dict-with-keyword</c> declarative assembly-format directive.
    /// </summary>
    public KeywordedAttributeDictionarySyntax BuildKeywordedAttrDict(NamedAttributeCollection attributes)
    {
        var attrDict = builder.BuildAttrDict(attributes);
        return new KeywordedAttributeDictionarySyntax(
            attrDict.OpenToken.HasValue ? TokenFactory.Identifier("attributes") : null,
            attrDict);
    }
}
