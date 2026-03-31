namespace MLIR.Generators.Emitters;

using System.Collections.Generic;
using System.Text;
using MLIR.ODS.Model;

internal static class AssemblyFormatEmitter
{
    public static void Emit(StringBuilder builder, OperationModel operation)
    {
        var className = DialectGeneratorNaming.GetOperationClassName(operation);
        builder.AppendLine("public sealed class " + className + "AssemblyFormat : IOperationAssemblyFormat");
        builder.AppendLine("{");
        builder.AppendLine("    public bool TryParse(SyntaxToken nameToken, IReadOnlyList<SyntaxToken> resultTokens, IReadOnlyList<SyntaxToken> resultCommaTokens, SyntaxToken? equalsToken, OperationParsingContext context, out OperationBodySyntax? body)");
        builder.AppendLine("    {");
        builder.AppendLine("        body = null;");
        builder.AppendLine("        return false;");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    public Operation Bind(OperationSyntax syntax, OperationDefinition definition, Binder binder)");
        builder.AppendLine("    {");
        builder.AppendLine("        var body = (" + className + "BodySyntax)syntax.Body;");
        builder.AppendLine("        binder.Report(new AssemblyDiagnostic(syntax.Location, $\"Custom assembly bodies are not yet supported for '{definition.Name}'.\"));");
        builder.AppendLine("        return new UnknownOperation(");
        builder.AppendLine("            syntax,");
        builder.AppendLine("            definition.Name,");
        builder.AppendLine("            definition,");
        builder.AppendLine("            new List<Region>(),");
        builder.AppendLine("            new List<NamedAttribute>(),");
        builder.AppendLine("            null,");
        builder.AppendLine("            new List<ValueReference>(),");
        builder.AppendLine("            new List<ValueReference>(),");
        builder.AppendLine("            new List<BlockReference>());");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    public OperationSyntax BuildCustomAssemblySyntax(Operation operation, ConcreteSyntaxBuilderContext context)");
        builder.AppendLine("    {");
        builder.AppendLine("        return context.RewriteOperation(operation, context.TransformGenericBody(operation));");
        builder.AppendLine("    }");
        builder.AppendLine("}");
    }
}
