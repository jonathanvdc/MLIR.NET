namespace MLIR.Construction;

using System.Collections.Generic;
using MLIR.Syntax;

/// <summary>
/// Provides ergonomic helpers for constructing MLIR concrete syntax trees programmatically.
/// </summary>
public static class MlirFactory
{
    /// <summary>
    /// Creates a module from the supplied top-level operations.
    /// </summary>
    /// <param name="operations">The top-level operations.</param>
    /// <returns>A module syntax node.</returns>
    public static ModuleSyntax Module(params OperationSyntax[] operations)
    {
        return new ModuleSyntax(operations);
    }

    /// <summary>
    /// Creates an operation using the ergonomic string-based API.
    /// </summary>
    /// <param name="name">The operation name without automatic modification.</param>
    /// <param name="results">The SSA results.</param>
    /// <param name="operands">The SSA operands.</param>
    /// <param name="successors">The successor block labels.</param>
    /// <param name="regions">The nested regions.</param>
    /// <param name="attributes">The named attributes.</param>
    /// <param name="type">The trailing type signature, if any.</param>
    /// <returns>An operation syntax node.</returns>
    public static OperationSyntax Op(
        string name,
        IReadOnlyList<string>? results = null,
        IReadOnlyList<string>? operands = null,
        IReadOnlyList<string>? successors = null,
        IReadOnlyList<RegionSyntax>? regions = null,
        IReadOnlyList<NamedAttributeSyntax>? attributes = null,
        string? type = null)
    {
        return new OperationSyntax(
            results ?? EmptyStrings,
            QuoteIfNeeded(name),
            operands ?? EmptyStrings,
            successors ?? EmptyStrings,
            regions ?? EmptyRegions,
            attributes ?? EmptyAttributes,
            type != null ? new RawSyntaxText(type) : null);
    }

    /// <summary>
    /// Creates a region from the supplied blocks.
    /// </summary>
    /// <param name="blocks">The blocks in the region.</param>
    /// <returns>A region syntax node.</returns>
    public static RegionSyntax Region(params BlockSyntax[] blocks)
    {
        return new RegionSyntax(blocks);
    }

    /// <summary>
    /// Creates a block from the supplied parts.
    /// </summary>
    /// <param name="label">The block label.</param>
    /// <param name="args">The block arguments.</param>
    /// <param name="ops">The block operations.</param>
    /// <returns>A block syntax node.</returns>
    public static BlockSyntax Block(
        string label,
        IReadOnlyList<BlockArgumentSyntax>? args = null,
        IReadOnlyList<OperationSyntax>? ops = null)
    {
        return new BlockSyntax(label, args ?? EmptyArguments, ops ?? EmptyOperations);
    }

    /// <summary>
    /// Creates a block argument.
    /// </summary>
    /// <param name="name">The SSA argument name.</param>
    /// <param name="type">The argument type.</param>
    /// <returns>A block argument syntax node.</returns>
    public static BlockArgumentSyntax Arg(string name, string type)
    {
        return new BlockArgumentSyntax(name, new RawSyntaxText(type));
    }

    /// <summary>
    /// Creates a named attribute.
    /// </summary>
    /// <param name="name">The attribute name.</param>
    /// <param name="value">The raw attribute value text.</param>
    /// <returns>A named attribute syntax node.</returns>
    public static NamedAttributeSyntax Attr(string name, string value)
    {
        return new NamedAttributeSyntax(name, new RawSyntaxText(value));
    }

    /// <summary>
    /// Creates a raw syntax fragment.
    /// </summary>
    /// <param name="text">The raw syntax text.</param>
    /// <returns>A raw syntax text node.</returns>
    public static RawSyntaxText Raw(string text)
    {
        return new RawSyntaxText(text);
    }

    private static string QuoteIfNeeded(string name)
    {
        return name.Length > 0 && name[0] == '"' ? name : "\"" + name + "\"";
    }

    private static readonly IReadOnlyList<string> EmptyStrings = new string[0];
    private static readonly IReadOnlyList<RegionSyntax> EmptyRegions = new RegionSyntax[0];
    private static readonly IReadOnlyList<NamedAttributeSyntax> EmptyAttributes = new NamedAttributeSyntax[0];
    private static readonly IReadOnlyList<BlockArgumentSyntax> EmptyArguments = new BlockArgumentSyntax[0];
    private static readonly IReadOnlyList<OperationSyntax> EmptyOperations = new OperationSyntax[0];
}
