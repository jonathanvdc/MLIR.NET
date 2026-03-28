namespace MLIR.Text;

using System.Text;
using MLIR.Semantics;

/// <summary>
/// Prints semantic MLIR modules, optionally using dialect-specific custom assembly formats.
/// </summary>
public sealed class MlirSemanticPrinter
{
    /// <summary>
    /// Converts a semantic module to MLIR text.
    /// </summary>
    /// <param name="module">The semantic module to print.</param>
    /// <returns>The printed MLIR text.</returns>
    public static string Print(Module module)
    {
        var printer = new MlirSemanticPrinter();
        var builder = new StringBuilder();
        MlirStructuralPrinter.AppendModule(builder, module.Operations, module.Syntax.EndOfFileToken, printer.AppendOperation);
        return builder.ToString();
    }

    internal void AppendOperation(StringBuilder builder, Operation operation, int indentLevel, string defaultLeadingTrivia)
    {
        if (operation.Definition?.AssemblyFormat != null)
        {
            operation.Definition.AssemblyFormat.Print(
                operation,
                new OperationPrintingContext(this, builder, indentLevel, defaultLeadingTrivia));
            return;
        }

        AppendGenericOperation(builder, operation, indentLevel, defaultLeadingTrivia);
    }

    internal void AppendGenericOperation(StringBuilder builder, Operation operation, int indentLevel, string defaultLeadingTrivia)
    {
        MlirPrinter.AppendOperation(
            builder,
            operation.Syntax,
            indentLevel,
            defaultLeadingTrivia,
            (innerBuilder, _, regionIndex, innerIndentLevel) =>
            {
                AppendRegion(innerBuilder, operation.Regions[regionIndex], innerIndentLevel);
            });
    }

    internal void AppendGenericOperation(StringBuilder builder, Syntax.OperationSyntax operation, int indentLevel, string defaultLeadingTrivia)
    {
        MlirPrinter.AppendOperation(builder, operation, indentLevel, defaultLeadingTrivia);
    }

    internal void AppendRegion(StringBuilder builder, Region region, int indentLevel)
    {
        MlirStructuralPrinter.AppendRegion(builder, region.Syntax, region.Blocks, indentLevel, static block => block.Syntax, static block => block.Operations, AppendOperation);
    }
}
