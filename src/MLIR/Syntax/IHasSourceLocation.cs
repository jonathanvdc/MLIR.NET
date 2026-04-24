using MLIR.Semantics;

namespace MLIR.Syntax;

using MLIR.Text;

/// <summary>
/// Interface for syntax nodes that have a well-defined source location.
/// This is used to provide a common API for retrieving source locations from syntax nodes.
/// </summary>
/// <remarks>
/// <see cref="IHasSourceLocation"/> is implemented by syntax node types, tokens, separated lists, and other constructs that may not directly inherit from <see cref="SyntaxNode"/> but still want to expose location information. This allows for
/// a uniform way to retrieve source locations from various syntax constructs without requiring them to be full-fledged syntax nodes.
/// </remarks>
public interface IHasSourceLocation
{
    /// <summary>
    /// Gets the source location of this syntax node. If the node does not have a well-defined source location, returns <c>SourceLocation.Unknown</c>.
    /// </summary>
    SourceLocation Location { get; }
}
