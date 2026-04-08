namespace MLIR.Syntax;

using System.Collections.Generic;
using MLIR.Semantics;
using MLIR.Text;

/// <summary>
/// Represents a single block within an MLIR region.
/// </summary>
public sealed class BlockSyntax : SyntaxNode
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BlockSyntax"/> class.
    /// </summary>
    /// <param name="label">The block label, including the leading <c>^</c>.</param>
    /// <param name="arguments">The block arguments.</param>
    /// <param name="operations">The operations contained in the block.</param>
    public BlockSyntax(string label, IReadOnlyList<BlockArgumentSyntax> arguments, IReadOnlyList<OperationSyntax> operations)
        : this(
            SyntaxTokenFactory.BlockLabel(label),
            new DelimitedSyntaxList<BlockArgumentSyntax>(
                arguments.Count > 0 ? SyntaxTokenFactory.LParen() : null,
                arguments,
                CreateDefaultCommaTokens(arguments.Count),
                arguments.Count > 0 ? SyntaxTokenFactory.RParen() : null),
            SyntaxTokenFactory.Colon(),
            operations)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BlockSyntax"/> class.
    /// </summary>
    /// <param name="labelToken">The block label token.</param>
    /// <param name="arguments">The delimited block argument list.</param>
    /// <param name="colonToken">The colon token after the block header.</param>
    /// <param name="operations">The operations contained in the block.</param>
    public BlockSyntax(
        SyntaxToken labelToken,
        DelimitedSyntaxList<BlockArgumentSyntax> arguments,
        SyntaxToken colonToken,
        IReadOnlyList<OperationSyntax> operations)
    {
        LabelToken = labelToken;
        Arguments = arguments;
        ColonToken = colonToken;
        Operations = operations;
    }

    /// <summary>
    /// Gets the block label token.
    /// </summary>
    public SyntaxToken LabelToken { get; }

    /// <summary>
    /// Gets the delimited block argument list.
    /// </summary>
    public DelimitedSyntaxList<BlockArgumentSyntax> Arguments { get; }

    /// <summary>
    /// Gets the colon token after the block header.
    /// </summary>
    public SyntaxToken ColonToken { get; }

    /// <summary>
    /// Gets the operations contained in the block.
    /// </summary>
    public IReadOnlyList<OperationSyntax> Operations { get; }

    /// <summary>
    /// Gets the block label, including the leading <c>^</c>.
    /// </summary>
    public string Label => LabelToken.Text;

    /// <summary>
    /// Gets the merged source location spanning from the block label through the last operation.
    /// Returns the label token location when there are no operations, and an unknown location
    /// when the label token itself has no source information.
    /// </summary>
    public override SourceLocation Location
    {
        get
        {
            var result = LabelToken.Location;
            foreach (var op in Operations)
            {
                result = SourceLocation.Merge(result, op.Location);
            }

            return result;
        }
    }

    /// <inheritdoc/>
    public override void WriteTo(SyntaxWriter writer)
    {
        // Synthetic entry blocks are a parser implementation detail. Omit their labels when
        // printing unless the block carries arguments that require an explicit header.
        var blockHasExplicitLabel = Label != "^entry" || Arguments.Count > 0;
        var regionIndentLevel = writer.IndentLevel;
        var blockIndentLevel = regionIndentLevel + 1;

        if (blockHasExplicitLabel)
        {
            writer.SuggestIndentedNewLine(blockIndentLevel);
            writer.WriteToken(LabelToken);
            writer.WriteDelimitedList(Arguments);
            writer.WriteToken(ColonToken);
        }

        var operationIndentLevel = blockHasExplicitLabel ? regionIndentLevel + 2 : regionIndentLevel + 1;
        foreach (var operation in Operations)
        {
            writer.IndentLevel = operationIndentLevel;
            writer.SuggestIndentedNewLine();
            operation.WriteTo(writer);
        }
    }

    private static IReadOnlyList<SyntaxToken> CreateDefaultCommaTokens(int count)
    {
        var separators = new List<SyntaxToken>();
        for (var i = 1; i < count; i++)
        {
            separators.Add(SyntaxTokenFactory.Comma());
        }

        return separators;
    }
}
