namespace MLIR.Generators.Emitters.AssemblyFormat;

using System.Text;
using MLIR.Generators.Emitters.Common;
using MLIR.ODS.Model;

/// <summary>
/// Generates the structured <c>TypeSyntax</c> subclass and the <c>ITypeAssemblyFormat</c>
/// implementation for a <c>TypeDef</c> with a declarative <c>assemblyFormat</c> string.
/// </summary>
internal static class TypeAssemblyFormatEmitter
{
    public static void EmitSyntaxClass(StringBuilder builder, TypeModel type, string className)
    {
        var format = type.AssemblyFormat!;
        var syntaxClassName = className + "Syntax";
        var lowered = AssemblyFormatLowerer.LowerType(type, format);
        new TypeSyntaxClassEmitter(type, syntaxClassName, lowered.Fields).Emit(builder);
    }

    public static void EmitAssemblyFormatClass(StringBuilder builder, TypeModel type, string className)
    {
        var format = type.AssemblyFormat!;
        var lowered = AssemblyFormatLowerer.LowerType(type, format);
        var fields = lowered.Fields;
        var syntaxClassName = className + "Syntax";
        var formatClassName = className + "AssemblyFormat";
        var bindBuildEmitter = new TypeBindBuildEmitter(type, fields, className, syntaxClassName);

        builder.AppendLine("internal sealed class " + formatClassName + " : BodyOnlyTypeAssemblyFormat");
        builder.AppendLine("{");
        builder.AppendLine("    public " + formatClassName + "()");
        builder.AppendLine("        : base(" + EmitterHelpers.ToCSharpStringLiteral(type.Name ?? string.Empty) + ")");
        builder.AppendLine("    {");
        builder.AppendLine("    }");
        builder.AppendLine();
        if (!lowered.IsSupported)
        {
            builder.AppendLine("    // This declarative format currently includes unsupported constructs for type/attr lowering.");
            builder.AppendLine("    // The generated format class is still emitted for API completeness, but parsing will fail fast.");
            builder.AppendLine();
        }
        builder.AppendLine("    protected override ParseResult<TypeSyntax> TryParseBody(ParsingContext context, DialectTypePrefix prefix)");
        builder.AppendLine("    {");
        if (!lowered.IsSupported)
        {
            builder.AppendLine("        return ParseResult<TypeSyntax>.Failure(new AssemblyDiagnostic(prefix.Location, \"Unsupported declarative assembly format construct for type body.\"));");
        }
        else
        {
            new TypeTryParseBodyEmitter(lowered, syntaxClassName).Emit(builder);
        }

        builder.AppendLine("    }");
        builder.AppendLine();
        bindBuildEmitter.EmitBindValueMethod(builder);
        builder.AppendLine();
        builder.AppendLine("    public override TypeReference Bind(TypeSyntax syntax, Binder binder)");
        builder.AppendLine("    {");
        builder.AppendLine("        return BindValue(syntax, binder);");
        builder.AppendLine("    }");
        builder.AppendLine();
        bindBuildEmitter.EmitBuildCustomAssemblySyntaxMethod(builder);
        builder.AppendLine("}");
    }
}
