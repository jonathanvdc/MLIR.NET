namespace MLIR.Generators.Emitters;

using System.Text;
using MLIR.Generators.Emitters.Common;
using MLIR.ODS.Model;
using MLIR.Text;

internal static class TypeConstraintEmitter
{
    public static void Emit(StringBuilder builder, TypeConstraintModel typeConstraint)
    {
        switch (typeConstraint.Kind)
        {
            case TypeConstraintKind.ExactInteger:
                EmitExactIntegerConstraint(builder, typeConstraint);
                return;
            case TypeConstraintKind.ExactFloat:
                EmitExactFloatConstraint(builder, typeConstraint);
                return;
            case TypeConstraintKind.IndexType:
                EmitPrimitiveConstraint(builder, typeConstraint, "IndexTypeReference", "syntax, syntax?.Location ?? MLIR.Semantics.SourceLocation.Unknown");
                return;
            case TypeConstraintKind.NoneType:
                EmitPrimitiveConstraint(builder, typeConstraint, "NoneType", "syntax, syntax?.Location ?? MLIR.Semantics.SourceLocation.Unknown");
                return;
            case TypeConstraintKind.TupleType:
                EmitTupleConstraint(builder, typeConstraint);
                return;
            case TypeConstraintKind.FunctionType:
                EmitFunctionConstraint(builder, typeConstraint);
                return;
            case TypeConstraintKind.TensorType:
                EmitTensorConstraint(builder, typeConstraint);
                return;
            case TypeConstraintKind.VectorType:
                EmitVectorConstraint(builder, typeConstraint);
                return;
            case TypeConstraintKind.MemRefType:
                EmitMemRefConstraint(builder, typeConstraint);
                return;
            default:
                EmitPlainConstraint(builder, typeConstraint);
                return;
        }
    }

    private static void EmitExactIntegerConstraint(StringBuilder builder, TypeConstraintModel typeConstraint)
    {
        var className = DialectGeneratorNaming.GetTypeConstraintClassName(typeConstraint);
        builder.AppendLine("public sealed partial class " + className + " : global::MLIR.IntegerType");
        builder.AppendLine("{");
        builder.AppendLine("    public new static TypeDefinition TypeDefinition { get; } =");
        builder.AppendLine("        new TypeDefinition(" + EmitterHelpers.ToCSharpStringLiteral(typeConstraint.CanonicalTypeName!) + ", factory: static context => " + className + ".BindValue(context));");
        builder.AppendLine();
        builder.AppendLine("    public static " + className + " BindValue(TypeReferenceConstructionContext context)");
        builder.AppendLine("    {");
        builder.AppendLine("        return new " + className + "(GetWidth(context.Syntax), GetSignedness(context.Syntax), context.Syntax);");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    public " + className + "(int width, global::MLIR.Semantics.Types.Primitives.IntegerTypeSignedness signedness, TypeSyntax? syntax = null)");
        builder.AppendLine("        : base(width, signedness, syntax, syntax?.Location ?? MLIR.Semantics.SourceLocation.Unknown)");
        builder.AppendLine("    {");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    public override string? Name => TypeDefinition.Name;");
        builder.AppendLine("    public override TypeDefinition? Definition => TypeDefinition;");
        builder.AppendLine();
        builder.AppendLine("    private static global::MLIR.Semantics.Types.Primitives.IntegerTypeSignedness GetSignedness(TypeSyntax? syntax)");
        builder.AppendLine("    {");
        builder.AppendLine("        return syntax is BuiltinIntegerTypeSyntax integerSyntax");
        builder.AppendLine("            ? integerSyntax.Signedness");
        builder.AppendLine("            : " + GetIntegerSignednessLiteral(typeConstraint.CanonicalTypeName!) + ";");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    private static int GetWidth(TypeSyntax? syntax)");
        builder.AppendLine("    {");
        builder.AppendLine("        return syntax is BuiltinIntegerTypeSyntax integerSyntax");
        builder.AppendLine("            ? integerSyntax.Width");
        builder.AppendLine("            : " + GetIntegerWidthLiteral(typeConstraint.CanonicalTypeName!) + ";");
        builder.AppendLine("    }");
        builder.AppendLine("}");
    }

    private static void EmitExactFloatConstraint(StringBuilder builder, TypeConstraintModel typeConstraint)
    {
        var className = DialectGeneratorNaming.GetTypeConstraintClassName(typeConstraint);
        builder.AppendLine("public sealed partial class " + className + " : FloatTypeReference");
        builder.AppendLine("{");
        builder.AppendLine("    public new static TypeDefinition TypeDefinition { get; } =");
        builder.AppendLine("        new TypeDefinition(" + EmitterHelpers.ToCSharpStringLiteral(typeConstraint.CanonicalTypeName!) + ", factory: static context => " + className + ".BindValue(context));");
        builder.AppendLine();
        builder.AppendLine("    public static " + className + " BindValue(TypeReferenceConstructionContext context)");
        builder.AppendLine("    {");
        builder.AppendLine("        return new " + className + "(TypeDefinition.Name, context.Syntax);");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    public " + className + "(string name, TypeSyntax? syntax = null)");
        builder.AppendLine("        : base(name, syntax, syntax?.Location ?? MLIR.Semantics.SourceLocation.Unknown)");
        builder.AppendLine("    {");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    public override string? Name => TypeDefinition.Name;");
        builder.AppendLine("    public override TypeDefinition? Definition => TypeDefinition;");
        builder.AppendLine("}");
    }

    private static void EmitPrimitiveConstraint(StringBuilder builder, TypeConstraintModel typeConstraint, string baseTypeName, string baseArguments)
    {
        var className = DialectGeneratorNaming.GetTypeConstraintClassName(typeConstraint);
        var bindValueModifier = baseTypeName == "NoneType" ? "new static " : "static ";
        builder.AppendLine("public sealed partial class " + className + " : " + baseTypeName);
        builder.AppendLine("{");
        if (typeConstraint.CanonicalTypeName != null)
        {
            builder.AppendLine("    public new static TypeDefinition TypeDefinition { get; } =");
            builder.AppendLine("        new TypeDefinition(" + EmitterHelpers.ToCSharpStringLiteral(typeConstraint.CanonicalTypeName) + ", factory: static context => " + className + ".BindValue(context));");
            builder.AppendLine();
            builder.AppendLine("    public " + bindValueModifier + className + " BindValue(TypeReferenceConstructionContext context)");
            builder.AppendLine("    {");
            builder.AppendLine("        return new " + className + "(context.Syntax);");
            builder.AppendLine("    }");
            builder.AppendLine();
        }

        builder.AppendLine("    public " + className + "(TypeSyntax? syntax = null)");
        builder.AppendLine("        : base(" + baseArguments + ")");
        builder.AppendLine("    {");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    public override string? Name => " + (typeConstraint.CanonicalTypeName != null ? "TypeDefinition.Name" : EmitterHelpers.ToCSharpStringLiteral(typeConstraint.Name)) + ";");
        builder.AppendLine("    public override TypeDefinition? Definition => " + (typeConstraint.CanonicalTypeName != null ? "TypeDefinition" : "null") + ";");
        builder.AppendLine("}");
    }

    private static void EmitTupleConstraint(StringBuilder builder, TypeConstraintModel typeConstraint)
    {
        EmitCollectionConstraint(
            builder,
            typeConstraint,
            "TupleTypeReference",
            "TupleTypeSyntax",
            "tupleSyntax",
            constructorSignature: "TupleTypeSyntax syntax, global::System.Collections.Generic.IReadOnlyList<TypeReference> elements",
            constructorBaseArguments: "syntax, elements",
            bindExpression: "new " + DialectGeneratorNaming.GetTypeConstraintClassName(typeConstraint) + "(tupleSyntax, tupleSyntax.Elements.Select(binder.BindTypeReference).ToArray())");
    }

    private static void EmitFunctionConstraint(StringBuilder builder, TypeConstraintModel typeConstraint)
    {
        EmitCollectionConstraint(
            builder,
            typeConstraint,
            "FunctionTypeReference",
            "FunctionTypeSyntax",
            "functionSyntax",
            constructorSignature: "FunctionTypeSyntax syntax, global::System.Collections.Generic.IReadOnlyList<TypeReference> inputs, global::System.Collections.Generic.IReadOnlyList<TypeReference> results",
            constructorBaseArguments: "syntax, inputs, results",
            bindExpression: "new " + DialectGeneratorNaming.GetTypeConstraintClassName(typeConstraint) + "(functionSyntax, functionSyntax.InputTypes.Items.Select(binder.BindTypeReference).ToArray(), " + DialectGeneratorNaming.GetTypeConstraintClassName(typeConstraint) + ".GetResults(functionSyntax).Select(binder.BindTypeReference).ToArray())",
            extraMembers:
                "    internal static global::System.Collections.Generic.IReadOnlyList<TypeSyntax> GetResults(FunctionTypeSyntax syntax)\n" +
                "    {\n" +
                "        return syntax.HasDelimitedResults\n" +
                "            ? syntax.ResultTypes.Items\n" +
                "            : syntax.ResultType != null ? new[] { syntax.ResultType } : global::System.Array.Empty<TypeSyntax>();\n" +
                "    }\n");
    }

    private static void EmitTensorConstraint(StringBuilder builder, TypeConstraintModel typeConstraint)
    {
        EmitCollectionConstraint(
            builder,
            typeConstraint,
            "TensorTypeReference",
            "TensorTypeSyntax",
            "tensorSyntax",
            constructorSignature: "TensorTypeSyntax syntax, global::System.Collections.Generic.IReadOnlyList<long?> dimensions, TypeReference elementType, global::System.Collections.Generic.IReadOnlyList<RawSyntaxText> trailingParameters",
            constructorBaseArguments: "syntax, dimensions, elementType, trailingParameters",
            bindExpression: "new " + DialectGeneratorNaming.GetTypeConstraintClassName(typeConstraint) + "(tensorSyntax, " + DialectGeneratorNaming.GetTypeConstraintClassName(typeConstraint) + ".DecodeDimensions(tensorSyntax.Dimensions), binder.BindTypeReference(tensorSyntax.ElementType), tensorSyntax.TrailingParameters)",
            extraMembers: GetShapeDecodeMembers());
    }

    private static void EmitVectorConstraint(StringBuilder builder, TypeConstraintModel typeConstraint)
    {
        EmitCollectionConstraint(
            builder,
            typeConstraint,
            "VectorTypeReference",
            "VectorTypeSyntax",
            "vectorSyntax",
            constructorSignature: "VectorTypeSyntax syntax, global::System.Collections.Generic.IReadOnlyList<long?> dimensions, TypeReference elementType",
            constructorBaseArguments: "syntax, dimensions, elementType",
            bindExpression: "new " + DialectGeneratorNaming.GetTypeConstraintClassName(typeConstraint) + "(vectorSyntax, " + DialectGeneratorNaming.GetTypeConstraintClassName(typeConstraint) + ".DecodeDimensions(vectorSyntax.Dimensions), binder.BindTypeReference(vectorSyntax.ElementType))",
            extraMembers: GetShapeDecodeMembers());
    }

    private static void EmitMemRefConstraint(StringBuilder builder, TypeConstraintModel typeConstraint)
    {
        EmitCollectionConstraint(
            builder,
            typeConstraint,
            "MemRefTypeReference",
            "MemRefTypeSyntax",
            "memRefSyntax",
            constructorSignature: "MemRefTypeSyntax syntax, global::System.Collections.Generic.IReadOnlyList<long?> dimensions, TypeReference elementType, global::System.Collections.Generic.IReadOnlyList<RawSyntaxText> trailingParameters",
            constructorBaseArguments: "syntax, dimensions, elementType, trailingParameters",
            bindExpression: "new " + DialectGeneratorNaming.GetTypeConstraintClassName(typeConstraint) + "(memRefSyntax, " + DialectGeneratorNaming.GetTypeConstraintClassName(typeConstraint) + ".DecodeDimensions(memRefSyntax.Dimensions), binder.BindTypeReference(memRefSyntax.ElementType), memRefSyntax.TrailingParameters)",
            extraMembers: GetShapeDecodeMembers());
    }

    private static void EmitCollectionConstraint(
        StringBuilder builder,
        TypeConstraintModel typeConstraint,
        string baseTypeName,
        string syntaxTypeName,
        string syntaxVariableName,
        string constructorSignature,
        string constructorBaseArguments,
        string bindExpression,
        string? extraMembers = null)
    {
        var className = DialectGeneratorNaming.GetTypeConstraintClassName(typeConstraint);
        var assemblyFormatType = className + "AssemblyFormat";
        builder.AppendLine("public sealed partial class " + className + " : " + baseTypeName);
        builder.AppendLine("{");
        builder.AppendLine("    public new static TypeDefinition TypeDefinition { get; } =");
        builder.AppendLine("        new TypeDefinition(" + EmitterHelpers.ToCSharpStringLiteral(typeConstraint.CanonicalTypeName!) + ", new " + assemblyFormatType + "());");
        builder.AppendLine();
        builder.AppendLine("    public " + className + "(" + constructorSignature + ")");
        builder.AppendLine("        : base(" + constructorBaseArguments + ")");
        builder.AppendLine("    {");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    public override string? Name => TypeDefinition.Name;");
        builder.AppendLine("    public override TypeDefinition? Definition => TypeDefinition;");
        if (!string.IsNullOrEmpty(extraMembers))
        {
            builder.AppendLine();
            builder.Append(extraMembers);
        }
        builder.AppendLine("}");
        builder.AppendLine();
        builder.AppendLine("internal sealed class " + assemblyFormatType + " : ITypeAssemblyFormat");
        builder.AppendLine("{");
        builder.AppendLine("    public ParseResult<TypeSyntax> TryParse(TypeParsingContext context)");
        builder.AppendLine("    {");
        builder.AppendLine("        return ParseResult<TypeSyntax>.NoMatch();");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    public TypeReference Bind(TypeSyntax syntax, TypeDefinition definition, Binder binder)");
        builder.AppendLine("    {");
        builder.AppendLine("        if (syntax is not " + syntaxTypeName + " " + syntaxVariableName + ")");
        builder.AppendLine("        {");
        builder.AppendLine("            return new UnknownTypeReference(syntax, definition.Name, definition, syntax.Location);");
        builder.AppendLine("        }");
        builder.AppendLine();
        builder.AppendLine("        return " + bindExpression + ";");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    public TypeSyntax BuildCustomAssemblySyntax(TypeReference type, ConcreteSyntaxBuilderContext context)");
        builder.AppendLine("    {");
        builder.AppendLine("        return type.Syntax ?? throw new global::System.InvalidOperationException(\"Structured builtin type constraints require syntax to rebuild their assembly form.\");");
        builder.AppendLine("    }");
        builder.AppendLine("}");
    }

    private static void EmitPlainConstraint(StringBuilder builder, TypeConstraintModel typeConstraint)
    {
        var className = DialectGeneratorNaming.GetTypeConstraintClassName(typeConstraint);
        builder.AppendLine("public sealed partial class " + className + " : TypeReference");
        builder.AppendLine("{");
        if (typeConstraint.CanonicalTypeName != null)
        {
            builder.AppendLine("    public static TypeDefinition TypeDefinition { get; } =");
            builder.AppendLine("        new TypeDefinition(" + EmitterHelpers.ToCSharpStringLiteral(typeConstraint.CanonicalTypeName) + ", factory: static context => " + className + ".BindValue(context));");
            builder.AppendLine();
            builder.AppendLine("    public static " + className + " BindValue(TypeReferenceConstructionContext context)");
            builder.AppendLine("    {");
            builder.AppendLine("        return new " + className + "(context.Syntax);");
            builder.AppendLine("    }");
            builder.AppendLine();
        }

        builder.AppendLine("    public " + className + "(TypeSyntax? syntax = null)");
        builder.AppendLine("        : base(syntax, syntax?.Location ?? MLIR.Semantics.SourceLocation.Unknown)");
        builder.AppendLine("    {");
        if (typeConstraint.CanonicalTypeName == null)
        {
            builder.AppendLine("        NameValue = syntax?.ToString();");
        }
        builder.AppendLine("    }");
        builder.AppendLine();
        if (typeConstraint.CanonicalTypeName == null)
        {
            builder.AppendLine("    private string? NameValue { get; }");
            builder.AppendLine();
        }

        builder.AppendLine("    public override string? Name => " + (typeConstraint.CanonicalTypeName != null ? "TypeDefinition.Name" : "NameValue") + ";");
        builder.AppendLine("    public override TypeDefinition? Definition => " + (typeConstraint.CanonicalTypeName != null ? "TypeDefinition" : "null") + ";");
        builder.AppendLine("}");
    }

    private static string GetIntegerSignednessLiteral(string canonicalTypeName)
    {
        if (!BuiltinIntegerTypeName.TryParse(canonicalTypeName, out var signedness, out _))
        {
            return "global::MLIR.Semantics.Types.Primitives.IntegerTypeSignedness.Signless";
        }

        return signedness switch
        {
            BuiltinIntegerTypeName.Kind.Signed => "global::MLIR.Semantics.Types.Primitives.IntegerTypeSignedness.Signed",
            BuiltinIntegerTypeName.Kind.Unsigned => "global::MLIR.Semantics.Types.Primitives.IntegerTypeSignedness.Unsigned",
            _ => "global::MLIR.Semantics.Types.Primitives.IntegerTypeSignedness.Signless",
        };
    }

    private static string GetIntegerWidthLiteral(string canonicalTypeName)
    {
        return BuiltinIntegerTypeName.TryParse(canonicalTypeName, out _, out var width)
            ? width.ToString(global::System.Globalization.CultureInfo.InvariantCulture)
            : "0";
    }

    private static string GetShapeDecodeMembers()
    {
        return
            "    internal static global::System.Collections.Generic.IReadOnlyList<long?> DecodeDimensions(global::System.Collections.Generic.IReadOnlyList<ShapedTypeDimensionSyntax> dimensions)\n" +
            "    {\n" +
            "        var decoded = new long?[dimensions.Count];\n" +
            "        for (var i = 0; i < dimensions.Count; i++)\n" +
            "        {\n" +
            "            decoded[i] = dimensions[i] switch\n" +
            "            {\n" +
            "                StaticShapedTypeDimensionSyntax staticDimension => staticDimension.Size,\n" +
            "                DynamicShapedTypeDimensionSyntax => null,\n" +
            "                _ => null,\n" +
            "            };\n" +
            "        }\n" +
            "\n" +
            "        return decoded;\n" +
            "    }\n";
    }

}
