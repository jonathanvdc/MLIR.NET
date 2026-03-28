namespace MLIR;

using MLIR.Syntax;
using MLIR.Text;

/// <summary>
/// Represents a parsed MLIR document and provides entry points for text conversion.
/// </summary>
public sealed class MlirDocument
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MlirDocument"/> class.
    /// </summary>
    /// <param name="module">The root module syntax.</param>
    public MlirDocument(ModuleSyntax module)
    {
        Module = module;
    }

    /// <summary>
    /// Gets the root module syntax.
    /// </summary>
    public ModuleSyntax Module { get; }

    /// <summary>
    /// Parses MLIR text into a document.
    /// </summary>
    /// <param name="source">The source text to parse.</param>
    /// <returns>A parsed MLIR document.</returns>
    public static MlirDocument Parse(string source)
    {
        return new MlirDocument(MlirParser.ParseModule(source));
    }

    /// <summary>
    /// Serializes the document back to MLIR text.
    /// </summary>
    /// <returns>The printed MLIR text.</returns>
    public string ToText()
    {
        return MlirPrinter.Print(Module);
    }
}
