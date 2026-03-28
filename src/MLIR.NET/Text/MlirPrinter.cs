namespace MLIR.Text;

using System.Linq;
using System.Text;
using MLIR.Syntax;

/// <summary>
/// Prints generic MLIR syntax back to its textual representation.
/// </summary>
public sealed class MlirPrinter
{
    /// <summary>
    /// Converts a module syntax tree to MLIR text.
    /// </summary>
    /// <param name="module">The module to print.</param>
    /// <returns>The printed MLIR text.</returns>
    public static string Print(MlirModuleSyntax module)
    {
        var builder = new StringBuilder();
        for (var i = 0; i < module.Operations.Count; i++)
        {
            if (i > 0)
            {
                builder.AppendLine();
            }

            AppendOperation(builder, module.Operations[i], 0);
        }

        return builder.ToString();
    }

    private static void AppendOperation(StringBuilder builder, OperationSyntax operation, int indentLevel)
    {
        AppendIndent(builder, indentLevel);

        if (operation.Results.Count > 0)
        {
            builder.Append(string.Join(", ", operation.Results));
            builder.Append(" = ");
        }

        builder.Append(operation.Name);
        builder.Append('(');
        builder.Append(string.Join(", ", operation.Operands));
        builder.Append(')');

        if (operation.Successors.Count > 0)
        {
            builder.Append(" [");
            builder.Append(string.Join(", ", operation.Successors));
            builder.Append(']');
        }

        foreach (var region in operation.Regions)
        {
            builder.Append(' ');
            AppendRegion(builder, region, indentLevel);
        }

        if (operation.Attributes.Count > 0)
        {
            builder.Append(" {");
            builder.Append(string.Join(", ", operation.Attributes.Select(static attribute => $"{attribute.Name} = {attribute.Value.Text}")));
            builder.Append('}');
        }

        if (operation.TypeSignature != null)
        {
            builder.Append(" : ");
            builder.Append(operation.TypeSignature.Text);
        }
    }

    private static void AppendRegion(StringBuilder builder, RegionSyntax region, int indentLevel)
    {
        builder.AppendLine("{");

        foreach (var block in region.Blocks)
        {
            // Synthetic entry blocks are used to model unlabeled region bodies and should not
            // be emitted back as an explicit ^entry label.
            var blockHasExplicitLabel = block.Label != "^entry" || block.Arguments.Count > 0;
            if (blockHasExplicitLabel)
            {
                AppendIndent(builder, indentLevel + 1);
                builder.Append(block.Label);
                if (block.Arguments.Count > 0)
                {
                    builder.Append('(');
                    builder.Append(string.Join(", ", block.Arguments.Select(static argument => $"{argument.Name}: {argument.Type.Text}")));
                    builder.Append(')');
                }

                builder.AppendLine(":");
            }

            foreach (var operation in block.Operations)
            {
                AppendOperation(builder, operation, indentLevel + 2 - (blockHasExplicitLabel ? 0 : 1));
                builder.AppendLine();
            }
        }

        if (builder.Length > 0 && builder[builder.Length - 1] == '\n')
        {
            builder.Length--;
        }

        builder.AppendLine();
        AppendIndent(builder, indentLevel);
        builder.Append('}');
    }

    private static void AppendIndent(StringBuilder builder, int indentLevel)
    {
        builder.Append(' ', indentLevel * 2);
    }
}
