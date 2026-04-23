namespace MLIR.Generators.Emitters;

using System.Text;
using MLIR.Generators.Emitters.AssemblyFormat;
using MLIR.Generators.Emitters.Common;
using MLIR.ODS.Model;

internal static class TypeEmitter
{
    public static void Emit(StringBuilder builder, TypeModel type, IReadOnlyList<string> markerInterfaces)
    {
        var className = DialectGeneratorNaming.GetTypeClassName(type);

        if (type.AssemblyFormat != null)
        {
            TypeAssemblyFormatEmitter.EmitSyntaxClass(builder, type, className);
            builder.AppendLine();
            builder.AppendLine();
            EmitterHelpers.AppendXmlDocComment(builder, type.Summary, type.Description);
            EmitParametrisedTypeClass(builder, type, className, markerInterfaces);
            builder.AppendLine();
            TypeAssemblyFormatEmitter.EmitAssemblyFormatClass(builder, type, className);
        }
        else
        {
            EmitterHelpers.AppendXmlDocComment(builder, type.Summary, type.Description);
            EmitParametrisedTypeClass(builder, type, className, markerInterfaces);
        }
    }

    private static void EmitParametrisedTypeClass(StringBuilder builder, TypeModel type, string className, IReadOnlyList<string> markerInterfaces)
    {
        var parameters = type.Parameters;
        var hasAssemblyFormat = type.AssemblyFormat != null;
        var formatClassName = hasAssemblyFormat ? className + "AssemblyFormat" : null;
        var assemblyFormatExpression = !string.IsNullOrEmpty(type.CsharpAssemblyFormat)
            ? type.CsharpAssemblyFormat
            : formatClassName != null
                ? "new " + formatClassName + "()"
                : null;

        var classDeclaration = "public sealed partial class " + className + " : TypeReference";
        if (markerInterfaces.Count > 0)
        {
            classDeclaration += ", " + string.Join(", ", markerInterfaces);
        }

        builder.AppendLine(classDeclaration);
        builder.AppendLine("{");
        EmitTypeDefinition(builder, type, assemblyFormatExpression);
        builder.AppendLine();

        EmitTypeConstructor(builder, className, parameters);
        builder.AppendLine();

        EmitTypeParameterProperties(builder, parameters);

        builder.AppendLine();
        builder.AppendLine("    public override string? Name => " + (string.IsNullOrEmpty(type.CsharpName) ? "TypeDefinition.Name" : type.CsharpName) + ";");
        builder.AppendLine();
        builder.AppendLine("    public override TypeDefinition? Definition => TypeDefinition;");
        builder.AppendLine();

        EmitTypeSemanticEquality(builder, className, parameters);
        builder.AppendLine();

        builder.AppendLine("}");
    }

    private static void EmitTypeDefinition(
        StringBuilder builder,
        TypeModel type,
        string? assemblyFormatExpression)
    {
        builder.AppendLine("    public static TypeDefinition TypeDefinition { get; } =");
        EmitterHelpers.AppendDefinitionConstructor(builder, "TypeDefinition", type.Name, assemblyFormatExpression);
    }

    private static void EmitTypeConstructor(StringBuilder builder, string className, IReadOnlyList<AttrOrTypeParameterModel> parameters)
    {
        builder.Append("    public " + className + "(");
        for (var i = 0; i < parameters.Count; i++)
        {
            if (i > 0)
            {
                builder.Append(", ");
            }

            var csharpType = TypeAssemblyFormatEmitter.GetResolvedCSharpType(parameters[i]);
            builder.Append(csharpType + " " + EmitterHelpers.LowerFirst(parameters[i].Name));
        }

        if (parameters.Count > 0)
        {
            builder.Append(", ");
        }

        builder.AppendLine("TypeSyntax? syntax = null)");
        builder.AppendLine("        : base(syntax)");
        builder.AppendLine("    {");
        foreach (var param in parameters)
        {
            var propertyName = DialectGeneratorNaming.ToPascalCase(param.Name);
            builder.AppendLine("        " + propertyName + " = " + EmitterHelpers.LowerFirst(param.Name) + ";");
        }

        builder.AppendLine("    }");
    }

    private static void EmitTypeParameterProperties(StringBuilder builder, IReadOnlyList<AttrOrTypeParameterModel> parameters)
    {
        foreach (var param in parameters)
        {
            var csharpType = TypeAssemblyFormatEmitter.GetResolvedCSharpType(param);
            var propertyName = DialectGeneratorNaming.ToPascalCase(param.Name);
            builder.AppendLine("    public " + csharpType + " " + propertyName + " { get; }");
        }
    }

    private static void EmitTypeSemanticEquality(StringBuilder builder, string className, IReadOnlyList<AttrOrTypeParameterModel> parameters)
    {
        builder.AppendLine("    protected override Type SemanticFamily => typeof(" + className + ");");
        builder.AppendLine();
        builder.AppendLine("    protected override bool SemanticEqualsValue(TypeReference other)");
        builder.AppendLine("    {");
        builder.AppendLine("        var typedOther = (" + className + ")other;");
        builder.Append("        return ");
        for (var i = 0; i < parameters.Count; i++)
        {
            if (i > 0)
            {
                builder.AppendLine(" &&");
                builder.Append("               ");
            }

            var propertyName = DialectGeneratorNaming.ToPascalCase(parameters[i].Name);
            var csharpType = TypeAssemblyFormatEmitter.GetResolvedCSharpType(parameters[i]);
            builder.Append(GetSemanticEqualsExpression(csharpType, propertyName, "typedOther." + propertyName));
        }

        if (parameters.Count == 0)
        {
            builder.Append("true");
        }

        builder.AppendLine(";");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    protected override int GetSemanticHashCodeValue()");
        builder.AppendLine("    {");
        builder.AppendLine("        unchecked");
        builder.AppendLine("        {");
        builder.AppendLine("            var hash = 17;");
        foreach (var param in parameters)
        {
            var propertyName = DialectGeneratorNaming.ToPascalCase(param.Name);
            var csharpType = TypeAssemblyFormatEmitter.GetResolvedCSharpType(param);
            EmitHashContribution(builder, csharpType, propertyName);
        }

        builder.AppendLine("            return hash;");
        builder.AppendLine("        }");
        builder.AppendLine("    }");
    }

    private static string GetSemanticEqualsExpression(string csharpType, string leftExpression, string rightExpression)
    {
        return IsTypeReferenceList(csharpType)
            ? "global::System.Linq.Enumerable.SequenceEqual(" + leftExpression + ", " + rightExpression + ")"
            : "global::System.Collections.Generic.EqualityComparer<" + csharpType + ">.Default.Equals(" + leftExpression + ", " + rightExpression + ")";
    }

    private static void EmitHashContribution(StringBuilder builder, string csharpType, string propertyName)
    {
        if (!IsTypeReferenceList(csharpType))
        {
            builder.AppendLine("            hash = (hash * 31) + global::System.Collections.Generic.EqualityComparer<" + csharpType + ">.Default.GetHashCode(" + propertyName + ");");
            return;
        }

        builder.AppendLine("            foreach (var item in " + propertyName + ")");
        builder.AppendLine("            {");
        builder.AppendLine("                hash = (hash * 31) + global::System.Collections.Generic.EqualityComparer<global::MLIR.Semantics.TypeReference>.Default.GetHashCode(item);");
        builder.AppendLine("            }");
    }

    private static bool IsTypeReferenceList(string csharpType)
    {
        return string.Equals(
            csharpType,
            "global::System.Collections.Generic.IReadOnlyList<global::MLIR.Semantics.TypeReference>",
            System.StringComparison.Ordinal);
    }

}
