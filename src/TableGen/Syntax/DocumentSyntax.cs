namespace TableGen.Syntax;

using System.Collections.Generic;

/// <summary>
/// Represents a parsed TableGen source file.
/// </summary>
public sealed class DocumentSyntax(IReadOnlyList<TopLevelSyntax> declarations)
{
    /// <summary>
    /// Gets the top-level declarations in the document.
    /// </summary>
    public IReadOnlyList<TopLevelSyntax> Declarations { get; } = declarations;
}
