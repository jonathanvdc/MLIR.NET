namespace MLIR;

using MLIR.Dialects;
using MLIR.Semantics;
using MLIR.Syntax;
using MLIR.Text;

/// <summary>
/// Represents a parsed MLIR document and provides entry points for text conversion.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="Document"/> class.
/// </remarks>
/// <param name="module">The root module syntax.</param>
public sealed class Document(ModuleSyntax module)
{
    /// <summary>
    /// Gets the root module syntax.
    /// </summary>
    public ModuleSyntax Module { get; } = module;

    /// <summary>
    /// Binds the document's concrete syntax tree to a semantic module.
    /// </summary>
    /// <param name="dialectRegistry">The dialect registry used to resolve known operations.</param>
    /// <returns>The semantic module.</returns>
    public Semantics.Module Bind(DialectRegistry? dialectRegistry = null)
    {
        return Binder.BindModule(Module, dialectRegistry);
    }

    /// <summary>
    /// Parses MLIR text into a document.
    /// </summary>
    /// <param name="source">The source text to parse.</param>
    /// <returns>A parsed MLIR document.</returns>
    public static Document Parse(string source)
    {
        return new Document(Parser.ParseModule(source));
    }

    /// <summary>
    /// Tries to parse MLIR text into a document.
    /// </summary>
    public static bool TryParse(string source, out Document? document, out Diagnostic? diagnostic)
    {
        document = null;
        if (!Parser.TryParseModule(source, out var module, out diagnostic))
        {
            return false;
        }

        document = new Document(module!);
        return true;
    }

    /// <summary>
    /// Parses a document from MLIR source text, using registered dialects to recognize custom assembly formats.
    /// </summary>
    /// <param name="source">The MLIR source text.</param>
    /// <param name="dialectRegistry">The dialect registry used to recognize custom assembly formats.</param>
    /// <returns>The parsed document.</returns>
    public static Document Parse(string source, DialectRegistry? dialectRegistry)
    {
        return new Document(Parser.ParseModule(source, dialectRegistry));
    }

    /// <summary>
    /// Tries to parse a document from MLIR source text, using registered dialects to recognize custom assembly formats.
    /// </summary>
    public static bool TryParse(string source, DialectRegistry? dialectRegistry, out Document? document, out Diagnostic? diagnostic)
    {
        document = null;
        if (!Parser.TryParseModule(source, dialectRegistry, out var module, out diagnostic))
        {
            return false;
        }

        document = new Document(module!);
        return true;
    }

    /// <summary>
    /// Serializes the document back to MLIR text.
    /// </summary>
    /// <returns>The printed MLIR text.</returns>
    public string ToText()
    {
        return Printer.Print(Module);
    }
}
