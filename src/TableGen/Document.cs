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
    /// Parses a TableGen document from source text with optional source-file context for diagnostics.
    /// </summary>
    /// <param name="source">The source text to parse.</param>
    /// <param name="sourceFile">The logical source file used in diagnostics, if known.</param>
    /// <returns>The parsed document.</returns>
    public static Document Parse(string source, TableGenSourceFile? sourceFile)
    {
        return new Document(Parser.ParseDocument(source, sourceFile?.LogicalPath));
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
        var defines = new HashSet<string>(StringComparer.Ordinal);
        var declarations = new List<TopLevelSyntax>();
        ExpandIncludes(source, sourceFile, resolver, defines, declarations);
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
        HashSet<string> defines,
        List<TopLevelSyntax> output)
    {
        var preprocessed = TableGenPreprocessor.Process(source, defines);
        var syntax = Parser.ParseDocument(preprocessed, sourceFile?.LogicalPath);
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

                var includedFile = new TableGenSourceFile(resolved.LogicalPath);
                ExpandIncludes(resolved.SourceText, includedFile, resolver, defines, output);
            }
            else
            {
                output.Add(declaration);
            }
        }
    }
}
