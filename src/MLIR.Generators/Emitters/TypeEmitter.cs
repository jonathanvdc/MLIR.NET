namespace MLIR.Generators.Emitters;

using System.Text;
using MLIR.Generators.Emitters.AssemblyFormat;
using MLIR.Generators.Emitters.Common;
using MLIR.ODS.Model;

internal static class TypeEmitter
{
    public static void Emit(StringBuilder builder, TypeModel type, IReadOnlyList<ResolvedTypeInterfaceModel> interfaces)
    {
        var className = DialectGeneratorNaming.GetTypeClassName(type);

        if (type.AssemblyFormat != null)
        {
            TypeAssemblyFormatEmitter.EmitSyntaxClass(builder, type, className);
            builder.AppendLine();
            builder.AppendLine();
            EmitterHelpers.AppendXmlDocComment(builder, type.Summary, type.Description);
            EmitParametrisedTypeClass(builder, type, className, interfaces);
            builder.AppendLine();
            TypeAssemblyFormatEmitter.EmitAssemblyFormatClass(builder, type, className);
        }
        else
        {
            EmitterHelpers.AppendXmlDocComment(builder, type.Summary, type.Description);
            EmitParametrisedTypeClass(builder, type, className, interfaces);
        }
    }

    private static void EmitParametrisedTypeClass(StringBuilder builder, TypeModel type, string className, IReadOnlyList<ResolvedTypeInterfaceModel> interfaces)
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
        if (interfaces.Count > 0)
        {
            classDeclaration += ", " + string.Join(", ", interfaces.Select(static resolved => resolved.QualifiedName));
        }

        builder.AppendLine(classDeclaration);
        builder.AppendLine("{");
        EmitTypeDefinition(builder, type, assemblyFormatExpression);
        builder.AppendLine();

        EmitTypeConstructor(builder, className, parameters);
        builder.AppendLine();

        EmitTypeParameterProperties(builder, parameters, interfaces);
        if (interfaces.Any(static resolved => resolved.InterfaceModel.CsharpMembers.Count > 0))
        {
            builder.AppendLine();
            TypeInterfaceImplementationEmitter.Emit(builder, type, interfaces);
        }

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

            var csharpType = AssemblyFormatLowerer.GetResolvedCSharpType(parameters[i]);
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

    private static void EmitTypeParameterProperties(
        StringBuilder builder,
        IReadOnlyList<AttrOrTypeParameterModel> parameters,
        IReadOnlyList<ResolvedTypeInterfaceModel> interfaces)
    {
        foreach (var param in parameters)
        {
            var csharpType = AssemblyFormatLowerer.GetResolvedCSharpType(param);
            var propertyName = DialectGeneratorNaming.ToPascalCase(param.Name);
            if (TryFindMappedInterfaceMember(interfaces, propertyName, csharpType, out var mappedMember, out var upstreamMethod))
            {
                EmitterHelpers.AppendXmlDocComment(
                    builder,
                    mappedMember!.CsharpSummary,
                    mappedMember.CsharpDescription ?? upstreamMethod?.Description,
                    "    ");
            }

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
            var csharpType = AssemblyFormatLowerer.GetResolvedCSharpType(parameters[i]);
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
            var csharpType = AssemblyFormatLowerer.GetResolvedCSharpType(param);
            EmitHashContribution(builder, csharpType, propertyName);
        }

        builder.AppendLine("            return hash;");
        builder.AppendLine("        }");
        builder.AppendLine("    }");
    }

    private static string GetSemanticEqualsExpression(string csharpType, string leftExpression, string rightExpression)
    {
        return TryGetReadOnlyListElementType(csharpType) != null
            ? "global::System.Linq.Enumerable.SequenceEqual(" + leftExpression + ", " + rightExpression + ")"
            : "global::System.Collections.Generic.EqualityComparer<" + csharpType + ">.Default.Equals(" + leftExpression + ", " + rightExpression + ")";
    }

    private static void EmitHashContribution(StringBuilder builder, string csharpType, string propertyName)
    {
        var readOnlyListElementType = TryGetReadOnlyListElementType(csharpType);
        if (readOnlyListElementType == null)
        {
            builder.AppendLine("            hash = (hash * 31) + global::System.Collections.Generic.EqualityComparer<" + csharpType + ">.Default.GetHashCode(" + propertyName + ");");
            return;
        }

        builder.AppendLine("            foreach (var item in " + propertyName + ")");
        builder.AppendLine("            {");
        builder.AppendLine("                hash = (hash * 31) + global::System.Collections.Generic.EqualityComparer<" + readOnlyListElementType + ">.Default.GetHashCode(item);");
        builder.AppendLine("            }");
    }

    private static string? TryGetReadOnlyListElementType(string csharpType)
    {
        const string prefix = "global::System.Collections.Generic.IReadOnlyList<";
        return csharpType.StartsWith(prefix, System.StringComparison.Ordinal) && csharpType.EndsWith(">", System.StringComparison.Ordinal)
            ? csharpType.Substring(prefix.Length, csharpType.Length - prefix.Length - 1)
            : null;
    }

    private static bool TryFindMappedInterfaceMember(
        IReadOnlyList<ResolvedTypeInterfaceModel> interfaces,
        string propertyName,
        string csharpType,
        out InterfaceCSharpMemberModel? mappedMember,
        out InterfaceMethodModel? upstreamMethod)
    {
        foreach (var resolvedInterface in interfaces)
        {
            foreach (var member in resolvedInterface.InterfaceModel.CsharpMembers)
            {
                if (!string.Equals(member.CsharpName, propertyName, System.StringComparison.Ordinal)
                    || !string.Equals(member.CsharpType, csharpType, System.StringComparison.Ordinal))
                {
                    continue;
                }

                mappedMember = member;
                upstreamMethod = resolvedInterface.InterfaceModel.Methods.FirstOrDefault(
                    method => string.Equals(method.Name, member.UpstreamName, System.StringComparison.Ordinal));
                return true;
            }
        }

        mappedMember = null;
        upstreamMethod = null;
        return false;
    }

}
