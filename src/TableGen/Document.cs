namespace TableGen;

using System;
using System.Collections.Generic;
using MLIR.Text;
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
    /// Use <see cref="Load(string, IncludeResolver)"/> to parse a document with include expansion.
    /// </summary>
    /// <param name="source">The source text to parse.</param>
    /// <returns>The parsed document.</returns>
    public static Document Parse(string source)
    {
        return Parse(new SourceDocument(source));
    }

    /// <summary>
    /// Parses a TableGen document from source text with source-document context for diagnostics.
    /// </summary>
    /// <param name="sourceDocument">The source document to parse.</param>
    /// <returns>The parsed document.</returns>
    public static Document Parse(SourceDocument sourceDocument)
    {
        return new Document(Parser.ParseDocument(sourceDocument));
    }

    /// <summary>
    /// Loads a TableGen document from source text, recursively expanding all include directives
    /// using the provided <paramref name="resolver"/>.
    /// </summary>
    /// <param name="source">The root source text to parse.</param>
    /// <param name="resolver">The include resolver used to look up the source text for each include directive.</param>
    /// <returns>
    /// A document whose declarations contain all top-level items from the root source and all
    /// transitively included files, with include directives replaced by the included content.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when an include directive cannot be resolved by <paramref name="resolver"/>.
    /// </exception>
    public static Document Load(string source, IncludeResolver resolver)
    {
        return Load(new SourceDocument(source), resolver);
    }

    /// <summary>
    /// Loads a TableGen document from a source document, recursively expanding all include directives
    /// using the provided <paramref name="resolver"/>.
    /// </summary>
    /// <param name="sourceDocument">The root source document to parse.</param>
    /// <param name="resolver">The include resolver used to look up the source text for each include directive.</param>
    /// <returns>
    /// A document whose declarations contain all top-level items from the root source and all
    /// transitively included files, with include directives replaced by the included content.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when an include directive cannot be resolved by <paramref name="resolver"/>.
    /// </exception>
    public static Document Load(SourceDocument sourceDocument, IncludeResolver resolver)
    {
        var defines = new HashSet<string>(StringComparer.Ordinal);
        var declarations = new List<TopLevelSyntax>();
        ExpandIncludes(sourceDocument, resolver, defines, declarations);
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

    /// <summary>
    /// Recursively preprocesses, parses, and expands include directives into a flat declaration list.
    /// </summary>
    /// <param name="sourceDocument">The source document currently being expanded.</param>
    /// <param name="resolver">The include resolver used for nested includes.</param>
    /// <param name="defines">The shared preprocessor symbol set.</param>
    /// <param name="output">The accumulated flattened declaration list.</param>
    private static void ExpandIncludes(
        SourceDocument sourceDocument,
        IncludeResolver resolver,
        HashSet<string> defines,
        List<TopLevelSyntax> output)
    {
        var preprocessed = Preprocessor.Process(sourceDocument.Text, defines);
        var syntax = Parser.ParseDocument(new SourceDocument(preprocessed, sourceDocument.FileName));
        foreach (var declaration in syntax.Declarations)
        {
            if (declaration is IncludeDirectiveSyntax include)
            {
                if (!resolver.TryResolveInclude(include.Path, sourceDocument, out var resolved))
                {
                    var location = sourceDocument.FileName != null
                        ? $" from '{sourceDocument.FileName}'"
                        : string.Empty;
                    throw new InvalidOperationException(
                        $"Could not resolve include '{include.Path}'{location}.");
                }

                // Includes are expanded eagerly so later stages can operate on one flat declaration stream.
                ExpandIncludes(resolved, resolver, defines, output);
            }
            else
            {
                output.Add(declaration);
            }
        }
    }
}
