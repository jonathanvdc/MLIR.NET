using MLIR.Semantics;

namespace MLIR.Syntax;

using MLIR.Text;

/// <summary>
/// Base class for all MLIR concrete syntax tree nodes.
/// Provides a uniform <see cref="WriteTo(Text.SyntaxWriter)"/> entry point used by
/// <see cref="ToString"/> so that every concrete node type produces a trimmed textual
/// representation without requiring callers to manage formatting parameters.
/// </summary>
public abstract class SyntaxNode : IHasSourceLocation
{
    /// <summary>
    /// Gets the source location of this syntax node, if known.
    /// </summary>
    public abstract SourceLocation Location { get; }

    /// <summary>
    /// Writes this syntax node to the supplied writer using default formatting.
    /// </summary>
    /// <param name="writer">The syntax writer to write to.</param>
    public abstract void WriteTo(Text.SyntaxWriter writer);

    /// <summary>
    /// Rewrites this syntax node using the supplied rewriter.
    /// </summary>
    /// <param name="rewriter">The rewriter to apply.</param>
    /// <returns>A rewritten syntax node of the same logical shape.</returns>
    public abstract SyntaxNode Rewrite(SyntaxRewriter rewriter);

    /// <inheritdoc/>
    public override string ToString()
    {
        var writer = new Text.SyntaxWriter();
        WriteTo(writer);
        return writer.ToString().Trim();
    }
}
