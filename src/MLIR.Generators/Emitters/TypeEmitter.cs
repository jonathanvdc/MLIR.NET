namespace MLIR.Generators.Emitters;

using System.Text;
using MLIR.ODS.Model;

internal static class TypeEmitter
{
    public static void Emit(StringBuilder builder, TypeModel type)
    {
        var className = DialectGeneratorNaming.GetTypeClassName(type);
        builder.AppendLine("public sealed class " + className + " : TypeReference");
        builder.AppendLine("{");
        builder.AppendLine("    public static TypeDefinition TypeDefinition { get; } =");
        builder.AppendLine("        new TypeDefinition(" + EmitterHelpers.ToCSharpStringLiteral(type.Name) + ", factory: static context => new " + className + "(context));");
        builder.AppendLine();
        builder.AppendLine("    public " + className + "(TypeReferenceConstructionContext context)");
        builder.AppendLine("        : base(context.Syntax, context.Name, context.Definition, context.Location)");
        builder.AppendLine("    {");
        builder.AppendLine("    }");
        builder.AppendLine("}");
    }
}
