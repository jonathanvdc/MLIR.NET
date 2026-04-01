namespace TableGen;

using System;
using System.Collections.Generic;
using TableGen.Evaluation;
using TableGen.Syntax;
using TableGen.Text;

/// <summary>
/// Represents a parsed TableGen document.
/// </summary>
public sealed class Document(DocumentSyntax syntax)
{
    /// <summary>
    /// Gets the parsed syntax tree.
    /// </summary>
    public DocumentSyntax Syntax { get; } = syntax;

    /// <summary>
    /// Parses a TableGen document from source text.
    /// Include directives present in the source are represented as
    /// <see cref="IncludeDirectiveSyntax"/> nodes but are not expanded.
    /// Use <see cref="Load"/> to parse a document with include expansion.
    /// </summary>
    /// <param name="source">The source text to parse.</param>
    /// <returns>The parsed document.</returns>
    public static Document Parse(string source)
    {
        return new Document(Parser.ParseDocument(source));
    }

    /// <summary>
    /// Loads a TableGen document from source text, recursively expanding all include directives
    /// using the provided <paramref name="resolver"/>.
    /// </summary>
    /// <param name="source">The root source text to parse.</param>
    /// <param name="resolver">
    /// The include resolver used to look up the source text for each include directive.
    /// </param>
    /// <param name="sourceFile">
    /// The source file context for the root document (used for relative include resolution
    /// and diagnostics), or <see langword="null"/> if not applicable.
    /// </param>
    /// <returns>
    /// A document whose declarations contain all top-level items from the root source and all
    /// transitively included files, with include directives replaced by the included content.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when an include directive cannot be resolved by <paramref name="resolver"/>.
    /// </exception>
    public static Document Load(
        string source,
        TableGenIncludeResolver resolver,
        TableGenSourceFile? sourceFile = null)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var declarations = new List<TopLevelSyntax>();
        ExpandIncludes(source, sourceFile, resolver, seen, declarations);
        return new Document(new DocumentSyntax(declarations));
    }

    /// <summary>
    /// Evaluates the document into expanded records.
    /// </summary>
    /// <returns>The interpreted document.</returns>
    public InterpretedDocument Evaluate()
    {
        return Interpreter.Evaluate(Syntax);
    }

    private static void ExpandIncludes(
        string source,
        TableGenSourceFile? sourceFile,
        TableGenIncludeResolver resolver,
        HashSet<string> seen,
        List<TopLevelSyntax> output)
    {
        var syntax = Parser.ParseDocument(source);
        foreach (var declaration in syntax.Declarations)
        {
            if (declaration is IncludeDirectiveSyntax include)
            {
                if (!resolver.TryResolveInclude(include.Path, sourceFile, out var resolved))
                {
                    var location = sourceFile != null
                        ? $" from '{sourceFile.LogicalPath}'"
                        : string.Empty;
                    throw new InvalidOperationException(
                        $"Could not resolve include '{include.Path}'{location}.");
                }

                // Guard against double inclusion using the resolved logical path.
                if (seen.Add(resolved.LogicalPath))
                {
                    var includedFile = new TableGenSourceFile(resolved.LogicalPath);
                    ExpandIncludes(resolved.SourceText, includedFile, resolver, seen, output);
                }
            }
            else
            {
                output.Add(declaration);
            }
        }
    }
}
