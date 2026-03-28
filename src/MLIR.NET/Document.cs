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
    /// Serializes the document back to MLIR text.
    /// </summary>
    /// <returns>The printed MLIR text.</returns>
    public string ToText()
    {
        return Printer.Print(Module);
    }
}
