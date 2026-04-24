namespace TableGen.Syntax;

using MLIR.Text;

/// <summary>
/// Represents a TableGen include directive, e.g. <c>include "path/to/file.td"</c>.
/// </summary>
public sealed class IncludeDirectiveSyntax(string path, SourceLocation location) : TopLevelSyntax
{
    /// <summary>
    /// Gets the include path as written in the source.
    /// </summary>
    public string Path { get; } = path;

    /// <summary>
    /// Gets the source location of the include path.
    /// </summary>
    public SourceLocation Location { get; } = location;
}
