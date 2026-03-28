namespace MLIR.Text;

using MLIR.Syntax;

/// <summary>
/// Prints generic MLIR syntax back to its textual representation.
/// </summary>
public sealed class Printer
{
    /// <summary>
    /// Converts a module syntax tree to MLIR text.
    /// </summary>
    /// <param name="module">The module to print.</param>
    /// <returns>The printed MLIR text.</returns>
    public static string Print(ModuleSyntax module)
    {
        var writer = new SyntaxWriter();
        writer.WriteModule(module);
        return writer.ToString();
    }
}
