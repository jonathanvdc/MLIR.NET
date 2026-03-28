namespace MLIR.Syntax;

using System.Collections.Generic;
using System;
using MLIR.Text;

/// <summary>
/// Represents a single block within an MLIR region.
/// </summary>
public sealed class BlockSyntax
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BlockSyntax"/> class.
    /// </summary>
    /// <param name="label">The block label, including the leading <c>^</c>.</param>
    /// <param name="arguments">The block arguments.</param>
    /// <param name="operations">The operations contained in the block.</param>
    public BlockSyntax(string label, IReadOnlyList<BlockArgumentSyntax> arguments, IReadOnlyList<OperationSyntax> operations)
        : this(
            new SyntaxToken(label),
            new DelimitedSyntaxList<BlockArgumentSyntax>(
                arguments.Count > 0 ? new SyntaxToken("(") : null,
                arguments,
                CreateDefaultCommaTokens(arguments.Count),
                arguments.Count > 0 ? new SyntaxToken(")") : null),
            new SyntaxToken(":"),
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

    internal void WriteTo(
        SyntaxWriter writer,
        int regionIndentLevel,
        Action<SyntaxWriter, OperationSyntax, int, string> writeOperation)
    {
        // Synthetic entry blocks are a parser implementation detail. Omit their labels when
        // printing unless the block carries arguments that require an explicit header.
        var blockHasExplicitLabel = Label != "^entry" || Arguments.Count > 0;
        var blockIndentLevel = regionIndentLevel + 1;

        if (blockHasExplicitLabel)
        {
            writer.WriteToken(LabelToken, "\n", blockIndentLevel);

            if (Arguments.OpenToken != null)
            {
                writer.WriteToken(Arguments.OpenToken.Value, string.Empty);
                for (var i = 0; i < Arguments.Count; i++)
                {
                    if (i > 0)
                    {
                        writer.WriteToken(Arguments.SeparatorTokens[i - 1], string.Empty);
                    }

                    Arguments[i].WriteTo(writer, i > 0 ? " " : string.Empty);
                }

                writer.WriteToken(Arguments.CloseToken!.Value, string.Empty);
            }

            writer.WriteToken(ColonToken, string.Empty);
        }

        var operationIndentLevel = blockHasExplicitLabel ? regionIndentLevel + 2 : regionIndentLevel + 1;
        foreach (var operation in Operations)
        {
            writeOperation(writer, operation, operationIndentLevel, "\n");
        }
    }

    private static IReadOnlyList<SyntaxToken> CreateDefaultCommaTokens(int count)
    {
        var separators = new List<SyntaxToken>();
        for (var i = 1; i < count; i++)
        {
            separators.Add(new SyntaxToken(","));
        }

        return separators;
    }
}
