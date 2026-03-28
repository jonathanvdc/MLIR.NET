namespace MLIR.Text;

using MLIR.Semantics;
using MLIR.Syntax;
using MLIR.Transforms;

/// <summary>
/// Prints MLIR syntax and semantic modules back to text.
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

    /// <summary>
    /// Converts a semantic module to MLIR text, using custom assembly formats when available.
    /// </summary>
    /// <param name="module">The semantic module to print.</param>
    /// <returns>The printed MLIR text.</returns>
    public static string Print(Module module)
    {
        return Print(AssemblySyntaxBuilder.BuildModule(module));
    }
}
