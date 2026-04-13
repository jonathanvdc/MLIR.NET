namespace MLIR.Generators.Emitters;

using System.Collections.Generic;
using System.Text;
using MLIR.Generators.Emitters.Common;
using MLIR.ODS.Model;

internal static class AttributeConstraintEmitter
{
    public static void Emit(StringBuilder builder, AttributeConstraintModel attributeConstraint, DialectSymbolResolver resolver)
    {
        var strategy = resolver.TryResolveAttributeConstraintStrategy(attributeConstraint.RecordName);

        if (strategy.IsEnum && attributeConstraint.EnumModel != null)
        {
            EmitEnumConstraint(builder, attributeConstraint, attributeConstraint.EnumModel);
        }
        else if (strategy.IsTypedArray)
        {
            EmitTypedArrayConstraint(builder, attributeConstraint, resolver);
        }
        else if (strategy.IsDenseCollection)
        {
            EmitDenseCollectionConstraintDefinition(builder, attributeConstraint, strategy, resolver);
        }
        else if (strategy.IsPrimitive && strategy.GetFactoryExpression(attributeConstraint.RecordName) != null)
        {
            // Primitive (non-enum) constraint: emit only the static AttributeConstraintDefinition.
            // No constraint wrapper class is generated; the factory produces the typed attr directly.
            EmitPrimitiveConstraintDefinition(builder, attributeConstraint, strategy);
        }
        else
        {
            EmitStandardConstraint(builder, attributeConstraint, strategy);
        }
    }

    /// <summary>
    /// Emits a minimal static class that carries only the
    /// <c>AttributeConstraintDefinition</c> for a primitive (non-enum) constraint.
    /// The factory lambda creates a typed attr (<c>IntegerAttr</c>, <c>FloatAttr</c>,
    /// or <c>StringAttr</c>) directly, so no constraint wrapper class is needed.
    /// </summary>
    private static void EmitPrimitiveConstraintDefinition(
        StringBuilder builder,
        AttributeConstraintModel attributeConstraint,
        AttributeConstraintCodeStrategy strategy)
    {
        var className = DialectGeneratorNaming.GetAttributeConstraintClassName(attributeConstraint);
        var factoryExpression = strategy.GetFactoryExpression(attributeConstraint.RecordName)!;
        builder.AppendLine("public static class " + className);
        builder.AppendLine("{");
        builder.AppendLine("    public static AttributeConstraintDefinition AttributeConstraintDefinition { get; } =");
        builder.Append("        new AttributeConstraintDefinition(");
        builder.Append(EmitterHelpers.ToCSharpStringLiteral(attributeConstraint.Name));
        var assemblyFormatExpression = strategy.GetAssemblyFormatConstructionExpression(attributeConstraint.RecordName);
        if (assemblyFormatExpression != null)
        {
            builder.Append(", " + assemblyFormatExpression);
        }
        else
        {
            var assemblyFormatType = strategy.GetAssemblyFormatType(attributeConstraint.RecordName);
            if (assemblyFormatType != null)
            {
                builder.Append(", new " + assemblyFormatType + "()");
            }
        }

        builder.AppendLine(", factory: " + factoryExpression + ");");
        builder.AppendLine("}");
    }

    private static void EmitDenseCollectionConstraintDefinition(
        StringBuilder builder,
        AttributeConstraintModel attributeConstraint,
        AttributeConstraintCodeStrategy strategy,
        DialectSymbolResolver resolver)
    {
        var className = DialectGeneratorNaming.GetAttributeConstraintClassName(attributeConstraint);
        var valueTypeName = strategy.GetAttributeValueTypeName(attributeConstraint.RecordName, resolver)
            ?? "global::MLIR.Semantics.AttributeValue";
        var itemTypeName = GetDenseCollectionItemTypeName(attributeConstraint.RecordName);
        var typeFactoryExpression = GetDenseCollectionTypeFactoryExpression(attributeConstraint.RecordName);

        builder.AppendLine("public static class " + className);
        builder.AppendLine("{");
        builder.AppendLine("    public static AttributeConstraintDefinition AttributeConstraintDefinition { get; } =");
        builder.Append("        new AttributeConstraintDefinition(");
        builder.Append(EmitterHelpers.ToCSharpStringLiteral(attributeConstraint.Name));
        var assemblyFormatExpression = strategy.GetAssemblyFormatConstructionExpression(attributeConstraint.RecordName);
        if (assemblyFormatExpression != null)
        {
            builder.Append(", " + assemblyFormatExpression);
        }
        else
        {
            var assemblyFormatType = strategy.GetAssemblyFormatType(attributeConstraint.RecordName);
            if (assemblyFormatType != null)
            {
                builder.Append(", new " + assemblyFormatType + "()");
            }
        }

        builder.AppendLine(", factory: static context => CreateFromContext(context));");
        builder.AppendLine();
        builder.AppendLine("    public static global::MLIR.Semantics.AttributeValue Create(" + valueTypeName + " items)");
        builder.AppendLine("    {");
        builder.AppendLine("        return CreateDenseArray(items, syntax: null);");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    public static " + valueTypeName + " GetItems(global::MLIR.Semantics.AttributeValue attribute)");
        builder.AppendLine("    {");
        builder.AppendLine("        if (attribute is global::MLIR.DenseArrayAttr denseArrayAttr)");
        builder.AppendLine("        {");
        builder.AppendLine("            return DecodeItems(denseArrayAttr.RawData);");
        builder.AppendLine("        }");
        builder.AppendLine();
        builder.AppendLine("        if (attribute.Syntax is MLIR.Syntax.Attributes.Collections.DenseArrayAttributeValueSyntax denseArraySyntax)");
        builder.AppendLine("        {");
        builder.AppendLine("            return DecodeItems(denseArraySyntax);");
        builder.AppendLine("        }");
        builder.AppendLine();
        builder.AppendLine("        throw new global::System.InvalidOperationException(\"Expected a dense array attribute value for dense constraint item extraction.\");");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    private static global::MLIR.Semantics.AttributeValue CreateFromContext(AttributeValueConstructionContext context)");
        builder.AppendLine("    {");
        builder.AppendLine("        if (context.Syntax is not MLIR.Syntax.Attributes.Collections.DenseArrayAttributeValueSyntax denseArraySyntax)");
        builder.AppendLine("        {");
        builder.AppendLine("            throw new global::System.InvalidOperationException(\"Expected dense array syntax when binding a dense array attribute constraint.\");");
        builder.AppendLine("        }");
        builder.AppendLine();
        builder.AppendLine("        return CreateDenseArray(DecodeItems(denseArraySyntax), denseArraySyntax);");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    private static global::MLIR.DenseArrayAttr CreateDenseArray(" + valueTypeName + " items, MLIR.Syntax.AttributeValueSyntax? syntax)");
        builder.AppendLine("    {");
        builder.AppendLine("        return new global::MLIR.DenseArrayAttr(" + typeFactoryExpression + ", items.Count, EncodeRawData(items), syntax);");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    private static " + valueTypeName + " DecodeItems(MLIR.Syntax.Attributes.Collections.DenseArrayAttributeValueSyntax denseArraySyntax)");
        builder.AppendLine("    {");
        EmitDenseDecodeItemsFromSyntax(builder, attributeConstraint.RecordName);
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    private static " + valueTypeName + " DecodeItems(global::System.ReadOnlyMemory<byte> rawData)");
        builder.AppendLine("    {");
        EmitDenseDecodeItemsFromRawData(builder, attributeConstraint.RecordName, itemTypeName);
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    private static global::System.ReadOnlyMemory<byte> EncodeRawData(" + valueTypeName + " items)");
        builder.AppendLine("    {");
        EmitDenseEncodeRawData(builder, attributeConstraint.RecordName);
        builder.AppendLine("    }");
        if (IsDenseIntegerRecord(attributeConstraint.RecordName))
        {
            builder.AppendLine();
            builder.AppendLine("    private static " + itemTypeName + " ToIntegerItem(global::MLIR.Numerics.ApInt value)");
            builder.AppendLine("    {");
            EmitDenseIntegerConversion(builder, attributeConstraint.RecordName);
            builder.AppendLine("    }");
            builder.AppendLine();
            builder.AppendLine("    private static global::MLIR.Numerics.ApInt ToApInt(" + itemTypeName + " value)");
            builder.AppendLine("    {");
            EmitDenseIntegerApIntConversion(builder, attributeConstraint.RecordName);
            builder.AppendLine("    }");
        }
        builder.AppendLine("}");
    }

    private static string GetDenseCollectionTypeFactoryExpression(string recordName)
    {
        return recordName switch
        {
            "DenseBoolArrayAttr" => "global::MLIR.Semantics.TypeFactory.I1",
            "DenseI8ArrayAttr" => "global::MLIR.Semantics.TypeFactory.I8",
            "DenseI16ArrayAttr" => "global::MLIR.Semantics.TypeFactory.I16",
            "DenseI32ArrayAttr" => "global::MLIR.Semantics.TypeFactory.I32",
            "DenseI64ArrayAttr" => "global::MLIR.Semantics.TypeFactory.I64",
            "DenseF32ArrayAttr" => "global::MLIR.Semantics.TypeFactory.F32",
            "DenseF64ArrayAttr" => "global::MLIR.Semantics.TypeFactory.F64",
            _ => "global::MLIR.Semantics.TypeFactory.None",
        };
    }

    private static string GetDenseCollectionItemTypeName(string recordName)
    {
        return recordName switch
        {
            "DenseBoolArrayAttr" => "bool",
            "DenseI8ArrayAttr" => "sbyte",
            "DenseI16ArrayAttr" => "short",
            "DenseI32ArrayAttr" => "int",
            "DenseI64ArrayAttr" => "long",
            "DenseF32ArrayAttr" => "float",
            "DenseF64ArrayAttr" => "double",
            _ => "int",
        };
    }

    private static bool IsDenseIntegerRecord(string recordName)
    {
        return recordName is "DenseI8ArrayAttr" or "DenseI16ArrayAttr" or "DenseI32ArrayAttr" or "DenseI64ArrayAttr";
    }

    private static void EmitDenseDecodeItemsFromSyntax(StringBuilder builder, string recordName)
    {
        if (recordName == "DenseBoolArrayAttr")
        {
            builder.AppendLine("        return global::MLIR.Semantics.Attributes.Collections.StructuredAttributeSemanticDecoder.DecodeBooleanItems(denseArraySyntax.Items.Items);");
            return;
        }

        if (recordName == "DenseF32ArrayAttr")
        {
            builder.AppendLine("        return global::MLIR.Semantics.Attributes.Collections.StructuredAttributeSemanticDecoder.DecodeFloatingPointItems(denseArraySyntax.Items.Items, global::MLIR.Numerics.FloatSemantics.IEEESingle);");
            return;
        }

        if (recordName == "DenseF64ArrayAttr")
        {
            builder.AppendLine("        return global::MLIR.Semantics.Attributes.Collections.StructuredAttributeSemanticDecoder.DecodeFloatingPointItems(denseArraySyntax.Items.Items, global::MLIR.Numerics.FloatSemantics.IEEEDouble);");
            return;
        }

        builder.AppendLine("        return global::MLIR.Semantics.Attributes.Collections.StructuredAttributeSemanticDecoder.DecodeIntegerItems(denseArraySyntax.Items.Items);");
    }

    private static void EmitDenseDecodeItemsFromRawData(StringBuilder builder, string recordName, string itemTypeName)
    {
        if (recordName == "DenseBoolArrayAttr")
        {
            builder.AppendLine("        var span = global::System.Runtime.InteropServices.MemoryMarshal.Cast<byte, bool>(rawData.Span);");
            builder.AppendLine("        var items = new global::System.Collections.Generic.List<bool>(span.Length);");
            builder.AppendLine("        for (var i = 0; i < span.Length; i++)");
            builder.AppendLine("        {");
            builder.AppendLine("            items.Add(span[i]);");
            builder.AppendLine("        }");
            builder.AppendLine("        return items;");
            return;
        }

        if (recordName == "DenseF32ArrayAttr" || recordName == "DenseF64ArrayAttr")
        {
            var floatType = recordName == "DenseF32ArrayAttr" ? "float" : "double";
            var semantics = recordName == "DenseF32ArrayAttr"
                ? "global::MLIR.Numerics.FloatSemantics.IEEESingle"
                : "global::MLIR.Numerics.FloatSemantics.IEEEDouble";
            builder.AppendLine("        var span = global::System.Runtime.InteropServices.MemoryMarshal.Cast<byte, " + floatType + ">(rawData.Span);");
            builder.AppendLine("        var items = new global::System.Collections.Generic.List<global::MLIR.Numerics.ApFloat>(span.Length);");
            builder.AppendLine("        for (var i = 0; i < span.Length; i++)");
            builder.AppendLine("        {");
            builder.AppendLine("            items.Add(global::MLIR.Numerics.ApFloat.FromDouble(" + semantics + ", span[i]));");
            builder.AppendLine("        }");
            builder.AppendLine("        return items;");
            return;
        }

        builder.AppendLine("        var span = global::System.Runtime.InteropServices.MemoryMarshal.Cast<byte, " + itemTypeName + ">(rawData.Span);");
        builder.AppendLine("        var items = new global::System.Collections.Generic.List<global::MLIR.Numerics.ApInt>(span.Length);");
        builder.AppendLine("        for (var i = 0; i < span.Length; i++)");
        builder.AppendLine("        {");
        builder.AppendLine("            items.Add(ToApInt(span[i]));");
        builder.AppendLine("        }");
        builder.AppendLine("        return items;");
    }

    private static void EmitDenseEncodeRawData(StringBuilder builder, string recordName)
    {
        if (recordName == "DenseBoolArrayAttr")
        {
            builder.AppendLine("        var values = new bool[items.Count];");
            builder.AppendLine("        for (var i = 0; i < items.Count; i++)");
            builder.AppendLine("        {");
            builder.AppendLine("            values[i] = items[i];");
            builder.AppendLine("        }");
            builder.AppendLine("        return global::System.Runtime.InteropServices.MemoryMarshal.AsBytes(values.AsSpan()).ToArray();");
            return;
        }

        if (recordName == "DenseF32ArrayAttr")
        {
            builder.AppendLine("        var values = new float[items.Count];");
            builder.AppendLine("        for (var i = 0; i < items.Count; i++)");
            builder.AppendLine("        {");
            builder.AppendLine("            values[i] = items[i].ToSingle();");
            builder.AppendLine("        }");
            builder.AppendLine("        return global::System.Runtime.InteropServices.MemoryMarshal.AsBytes(values.AsSpan()).ToArray();");
            return;
        }

        if (recordName == "DenseF64ArrayAttr")
        {
            builder.AppendLine("        var values = new double[items.Count];");
            builder.AppendLine("        for (var i = 0; i < items.Count; i++)");
            builder.AppendLine("        {");
            builder.AppendLine("            values[i] = items[i].ToDouble();");
            builder.AppendLine("        }");
            builder.AppendLine("        return global::System.Runtime.InteropServices.MemoryMarshal.AsBytes(values.AsSpan()).ToArray();");
            return;
        }

        var itemTypeName = GetDenseCollectionItemTypeName(recordName);
        builder.AppendLine("        var values = new " + itemTypeName + "[items.Count];");
        builder.AppendLine("        for (var i = 0; i < items.Count; i++)");
        builder.AppendLine("        {");
        builder.AppendLine("            values[i] = ToIntegerItem(items[i]);");
        builder.AppendLine("        }");
        builder.AppendLine("        return global::System.Runtime.InteropServices.MemoryMarshal.AsBytes(values.AsSpan()).ToArray();");
    }

    private static void EmitDenseIntegerConversion(StringBuilder builder, string recordName)
    {
        switch (recordName)
        {
            case "DenseI8ArrayAttr":
                builder.AppendLine("        return (sbyte)value.ToInt64();");
                break;
            case "DenseI16ArrayAttr":
                builder.AppendLine("        return (short)value.ToInt64();");
                break;
            case "DenseI32ArrayAttr":
                builder.AppendLine("        return (int)value.ToInt64();");
                break;
            case "DenseI64ArrayAttr":
                builder.AppendLine("        return value.ToInt64();");
                break;
            default:
                builder.AppendLine("        return 0;");
                break;
        }
    }

    private static void EmitDenseIntegerApIntConversion(StringBuilder builder, string recordName)
    {
        switch (recordName)
        {
            case "DenseI8ArrayAttr":
                builder.AppendLine("        return global::MLIR.Numerics.ApInt.FromInt64(8, value);");
                break;
            case "DenseI16ArrayAttr":
                builder.AppendLine("        return global::MLIR.Numerics.ApInt.FromInt64(16, value);");
                break;
            case "DenseI32ArrayAttr":
                builder.AppendLine("        return global::MLIR.Numerics.ApInt.FromInt64(32, value);");
                break;
            case "DenseI64ArrayAttr":
                builder.AppendLine("        return global::MLIR.Numerics.ApInt.FromInt64(64, value);");
                break;
            default:
                builder.AppendLine("        return global::MLIR.Numerics.ApInt.Zero(64);");
                break;
        }
    }

    private static void EmitEnumConstraint(StringBuilder builder, AttributeConstraintModel attributeConstraint, EnumModel enumModel)
    {
        var className = DialectGeneratorNaming.GetAttributeConstraintClassName(attributeConstraint);
        var enumTypeName = EnumHelpers.GetCSharpEnumTypeName(enumModel);

        // Constraint class
        var assemblyFormatType = className + "AssemblyFormat";
        builder.AppendLine("public sealed class " + className + " : IntegerAttributeValue");
        builder.AppendLine("{");
        builder.AppendLine("    public static AttributeConstraintDefinition AttributeConstraintDefinition { get; } =");
        builder.AppendLine("        new AttributeConstraintDefinition(");
        builder.Append("            " + EmitterHelpers.ToCSharpStringLiteral(attributeConstraint.Name));
        builder.AppendLine(", new " + assemblyFormatType + "(), factory: static context => new " + className + "(context));");
        builder.AppendLine();
        builder.AppendLine("    public " + className + "(AttributeValueConstructionContext context)");
        builder.AppendLine("        : base(context, ParseValue(context.Syntax))");
        builder.AppendLine("    {");
        builder.AppendLine("        EnumValue = ParseEnumValue(context.Syntax);");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    public " + className + "(" + enumTypeName + " value)");
        builder.AppendLine("        : base(global::MLIR.Numerics.ApInt.FromUInt64(64, (ulong)value))");
        builder.AppendLine("    {");
        builder.AppendLine("        EnumValue = value;");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    public " + enumTypeName + " EnumValue { get; }");
        builder.AppendLine("    public " + enumTypeName + " TypedValue => EnumValue;");
        builder.AppendLine();
        builder.AppendLine("    public override string? Name => AttributeConstraintDefinition.Name;");
        builder.AppendLine("    public override AttributeConstraintDefinition? Definition => AttributeConstraintDefinition;");
        builder.AppendLine();

        // Enum value parser
        builder.AppendLine("    private static " + enumTypeName + " ParseEnumValue(MLIR.Syntax.AttributeValueSyntax? syntax)");
        builder.AppendLine("    {");
        builder.AppendLine("        if (syntax == null) return default;");
        builder.AppendLine("        if (syntax is MLIR.Syntax.Attributes.Primitives.IntegerAttributeValueSyntax integerSyntax)");
        builder.AppendLine("        {");
        builder.AppendLine("            return " + EnumEmitter.GetEnumInfoClassName(enumModel) + ".TryFromInteger(integerSyntax.Value, out var integerValue) ? integerValue : default;");
        builder.AppendLine("        }");
        builder.AppendLine("        var raw = syntax.ToString();");
        EnumEmitter.EmitParseExpression(builder, enumModel, enumTypeName, "raw", "        ");
        builder.AppendLine("    }");
        builder.AppendLine();

        // Integer value parser (to feed IntegerAttributeValue base constructor)
        builder.AppendLine("    private static global::MLIR.Numerics.ApInt ParseValue(MLIR.Syntax.AttributeValueSyntax? syntax)");
        builder.AppendLine("    {");
        builder.AppendLine("        return global::MLIR.Numerics.ApInt.FromUInt64(64, global::System.Convert.ToUInt64(ParseEnumValue(syntax)));");
        builder.AppendLine("    }");
        builder.AppendLine();

        // Print helper
        builder.AppendLine("    internal string PrintEnumValue(" + enumTypeName + " value)");
        builder.AppendLine("    {");
        EnumEmitter.EmitFormatExpression(builder, enumModel, "value", "        ");
        builder.AppendLine("    }");
        builder.AppendLine("}");
        builder.AppendLine();

        // Assembly format class for enum constraint
        builder.AppendLine("internal sealed class " + assemblyFormatType + " : IAttributeAssemblyFormat");
        builder.AppendLine("{");
        builder.AppendLine("    public ParseResult<AttributeValueSyntax> TryParse(AttributeParsingContext context)");
        builder.AppendLine("    {");
        builder.AppendLine("        if (!context.TryMatch(MLIR.Text.TokenKind.Identifier, out var firstToken)");
        builder.AppendLine("            && !context.TryMatch(MLIR.Text.TokenKind.StringLiteral, out firstToken))");
        builder.AppendLine("        {");
        builder.AppendLine("            return ParseResult<AttributeValueSyntax>.NoMatch();");
        builder.AppendLine("        }");
        builder.AppendLine();
        builder.AppendLine("        var rawText = firstToken.Text;");
        if (enumModel.IsBitEnum)
        {
            var sepKind = EnumEmitter.GetSeparatorTokenKind(enumModel);
            builder.AppendLine("        while (context.TryMatch(MLIR.Text." + sepKind + ", out _))");
            builder.AppendLine("        {");
            builder.AppendLine("            if (!context.TryMatch(MLIR.Text.TokenKind.Identifier, out var nextToken)");
            builder.AppendLine("                && !context.TryMatch(MLIR.Text.TokenKind.StringLiteral, out nextToken))");
            builder.AppendLine("            {");
            builder.AppendLine("                break;");
            builder.AppendLine("            }");
            builder.AppendLine();
            builder.AppendLine("            rawText += " + EmitterHelpers.ToCSharpStringLiteral(enumModel.Separator) + " + nextToken.Text;");
            builder.AppendLine("        }");
        }

        builder.AppendLine();
        builder.AppendLine("        return ParseResult<AttributeValueSyntax>.Success(new MLIR.Syntax.RawAttributeValueSyntax(new MLIR.Syntax.RawSyntaxText(rawText)));");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    public AttributeValue Bind(AttributeValueSyntax syntax, AttributeConstraintDefinition definition, Binder binder)");
        builder.AppendLine("    {");
        builder.AppendLine("        return definition.Factory(binder.CreateAttributeValueConstructionContext(syntax, definition.Name, definition, syntax.Location));");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    public AttributeValueSyntax BuildCustomAssemblySyntax(AttributeValue attribute, ConcreteSyntaxBuilderContext context)");
        builder.AppendLine("    {");
        builder.AppendLine("        var enumAttr = (" + className + ")attribute;");
        builder.AppendLine("        var text = enumAttr.PrintEnumValue(enumAttr.EnumValue);");
        builder.AppendLine("        return new MLIR.Syntax.RawAttributeValueSyntax(new MLIR.Syntax.RawSyntaxText(text));");
        builder.AppendLine("    }");
        builder.AppendLine("}");
    }

    private static void EmitTypedArrayConstraint(StringBuilder builder, AttributeConstraintModel attributeConstraint, DialectSymbolResolver resolver)
    {
        var className = DialectGeneratorNaming.GetAttributeConstraintClassName(attributeConstraint);
        var elementRecordName = attributeConstraint.ElementConstraintRecordName;
        var elementStrategy = string.IsNullOrEmpty(elementRecordName)
            ? FallbackAttributeConstraintCodeStrategy.Instance
            : resolver.TryResolveAttributeConstraintStrategy(elementRecordName!);
        var elementTypeName = GetTypedArrayElementTypeName(elementRecordName, elementStrategy, resolver);
        // When the element strategy provides a direct decode expression, use it to avoid
        // instantiating a now-removed constraint wrapper class.
        var elementDecodeExpression = string.IsNullOrEmpty(elementRecordName)
            ? null
            : elementStrategy.GetTypedArrayElementDecodeExpression(elementRecordName!);
        var elementToSyntaxExpression = string.IsNullOrEmpty(elementRecordName)
            ? null
            : elementStrategy.GetTypedArrayElementToSyntaxExpression(elementRecordName!);
        var elementUsesPayload = elementDecodeExpression == null && elementStrategy.UsesTypedArrayElementPayload;
        var assemblyFormatType = className + "AssemblyFormat";

        builder.AppendLine("public static class " + className);
        builder.AppendLine("{");
        builder.AppendLine("    public static AttributeConstraintDefinition AttributeConstraintDefinition { get; } =");
        builder.AppendLine("        new AttributeConstraintDefinition(");
        builder.Append("            " + EmitterHelpers.ToCSharpStringLiteral(attributeConstraint.Name));
        // The constraint definition factory receives parsing context data; construct the
        // nested Value directly so Name/Definition and source locations are preserved.
        builder.AppendLine(", new " + assemblyFormatType + "(), factory: static context => new Value(context));");
        builder.AppendLine();
        builder.AppendLine("    public static global::MLIR.Semantics.AttributeValue Create(global::System.Collections.Generic.IReadOnlyList<" + elementTypeName + "> items)");
        builder.AppendLine("    {");
        builder.AppendLine("        return new Value(items);");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    public static global::System.Collections.Generic.IReadOnlyList<" + elementTypeName + "> GetItems(global::MLIR.Semantics.AttributeValue attribute)");
        builder.AppendLine("    {");
        builder.AppendLine("        if (attribute is Value value)");
        builder.AppendLine("        {");
        builder.AppendLine("            return value.Items;");
        builder.AppendLine("        }");
        builder.AppendLine();
        builder.AppendLine("        if (attribute is global::MLIR.Semantics.Attributes.Collections.TypedArrayAttributeValue<" + elementTypeName + "> typedArray)");
        builder.AppendLine("        {");
        builder.AppendLine("            return typedArray.Items;");
        builder.AppendLine("        }");
        builder.AppendLine();
        builder.AppendLine("        return DecodeItems(attribute.Syntax);");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    private static global::System.Collections.Generic.IReadOnlyList<" + elementTypeName + "> DecodeItems(MLIR.Syntax.AttributeValueSyntax? syntax)");
        builder.AppendLine("    {");
        builder.AppendLine("        if (syntax is not MLIR.Syntax.Attributes.Collections.ArrayAttributeValueSyntax arraySyntax)");
        builder.AppendLine("        {");
        builder.AppendLine("            return global::System.Array.Empty<" + elementTypeName + ">();");
        builder.AppendLine("        }");
        builder.AppendLine();
        builder.AppendLine("        var items = new global::System.Collections.Generic.List<" + elementTypeName + ">(arraySyntax.Items.Count);");
        builder.AppendLine("        for (var i = 0; i < arraySyntax.Items.Count; i++)");
        builder.AppendLine("        {");
        builder.AppendLine("            var itemSyntax = arraySyntax.Items[i];");
        if (elementDecodeExpression != null)
        {
            // Primitive element: decode directly without a wrapper class.
            var decodeExpr = elementDecodeExpression.Replace("{itemSyntax}", "itemSyntax");
            builder.AppendLine("            items.Add(" + decodeExpr + ");");
        }
        else if (elementStrategy.IsDenseCollection && !string.IsNullOrEmpty(elementRecordName))
        {
            var elementClassName = GetTypedArrayElementClassName(elementRecordName, resolver);
            builder.AppendLine("            items.Add(" + elementClassName + ".GetItems(MLIR.Semantics.Attributes.Collections.StructuredAttributeSemanticDecoder.DecodeValue(itemSyntax)));");
        }
        else if (elementStrategy.IsTypedArray && !string.IsNullOrEmpty(elementRecordName))
        {
            var elementClassName = GetTypedArrayElementClassName(elementRecordName, resolver);
            // Nested typed-array constraints recurse through the element constraint's
            // helper so arrays-of-arrays decode as IReadOnlyList<IReadOnlyList<...>>.
            builder.AppendLine("            items.Add(" + elementClassName + ".GetItems(MLIR.Semantics.Attributes.Collections.StructuredAttributeSemanticDecoder.DecodeValue(itemSyntax)));");
        }
        else if (elementUsesPayload)
        {
            var elementClassName = GetTypedArrayElementClassName(elementRecordName, resolver);
            var elementPayloadProperty = GetTypedArrayElementPayloadPropertyName(elementRecordName, elementStrategy);
            builder.AppendLine("            var itemValue = new " + elementClassName + "(new MLIR.Semantics.AttributeValueConstructionContext(itemSyntax, " + elementClassName + ".AttributeConstraintDefinition.Name, " + elementClassName + ".AttributeConstraintDefinition, itemSyntax.Location));");
            builder.AppendLine("            items.Add(itemValue." + elementPayloadProperty + ");");
        }
        else
        {
            builder.AppendLine("            items.Add(MLIR.Semantics.Attributes.Collections.StructuredAttributeSemanticDecoder.DecodeValue(itemSyntax));");
        }
        builder.AppendLine("        }");
        builder.AppendLine();
        builder.AppendLine("        return items;");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    private sealed class Value : TypedArrayAttributeValue<" + elementTypeName + ">");
        builder.AppendLine("    {");
        builder.AppendLine("        public Value(AttributeValueConstructionContext context)");
        builder.AppendLine("            : base(context, DecodeItems(context.Syntax))");
        builder.AppendLine("        {");
        builder.AppendLine("        }");
        builder.AppendLine();
        builder.AppendLine("        public Value(global::System.Collections.Generic.IReadOnlyList<" + elementTypeName + "> items)");
        builder.AppendLine("            : base(items)");
        builder.AppendLine("        {");
        builder.AppendLine("        }");
        builder.AppendLine();
        builder.AppendLine("        public override string? Name => AttributeConstraintDefinition.Name;");
        builder.AppendLine("        public override AttributeConstraintDefinition? Definition => AttributeConstraintDefinition;");
        builder.AppendLine("    }");
        builder.AppendLine("}");
        builder.AppendLine();

        builder.AppendLine("internal sealed class " + assemblyFormatType + " : TypedArrayAttributeAssemblyFormat<" + elementTypeName + ">");
        builder.AppendLine("{");
        builder.AppendLine("    protected override MLIR.Syntax.AttributeValueSyntax ElementToSyntax(" + elementTypeName + " element, MLIR.Transforms.ConcreteSyntaxBuilderContext context)");
        builder.AppendLine("    {");
        if (elementToSyntaxExpression != null)
        {
            // Primitive element: build syntax without a wrapper class instance.
            var toSyntaxExpr = elementToSyntaxExpression.Replace("{element}", "element").Replace("{context}", "context");
            builder.AppendLine("        return " + toSyntaxExpr + ";");
        }
        else if (elementStrategy.IsDenseCollection && !string.IsNullOrEmpty(elementRecordName))
        {
            var elementClassName = GetTypedArrayElementClassName(elementRecordName, resolver);
            builder.AppendLine("        return context.BuildAttributeValueSyntax(" + elementClassName + ".Create(element));");
        }
        else if (elementStrategy.IsTypedArray && !string.IsNullOrEmpty(elementRecordName))
        {
            var elementClassName = GetTypedArrayElementClassName(elementRecordName, resolver);
            builder.AppendLine("        return context.BuildAttributeValueSyntax(" + elementClassName + ".Create(element));");
        }
        else if (elementUsesPayload)
        {
            var elementClassName = GetTypedArrayElementClassName(elementRecordName, resolver);
            builder.AppendLine("        return context.BuildAttributeValueSyntax(new " + elementClassName + "(element));");
        }
        else
        {
            builder.AppendLine("        return context.BuildAttributeValueSyntax(element);");
        }
        builder.AppendLine("    }");
        builder.AppendLine("}");
    }

    private static string GetTypedArrayElementTypeName(string? elementRecordName, AttributeConstraintCodeStrategy elementStrategy, DialectSymbolResolver resolver)
    {
        if (string.IsNullOrEmpty(elementRecordName) || elementStrategy.IsGenericTypedArrayElement)
        {
            return "global::MLIR.Semantics.AttributeValue";
        }

        var nonNullRecordName = elementRecordName!;
        return elementStrategy.GetAttributeValueTypeName(nonNullRecordName, resolver)
            ?? "global::MLIR.Semantics.AttributeValue";
    }

    private static string GetTypedArrayElementClassName(string? elementRecordName, DialectSymbolResolver resolver)
    {
        if (string.IsNullOrEmpty(elementRecordName))
        {
            return "global::MLIR.Semantics.AttributeValue";
        }

        return resolver.TryResolveAttributeConstraintClassName(elementRecordName!)
            ?? "global::MLIR.Semantics.AttributeValue";
    }

    private static string GetTypedArrayElementPayloadPropertyName(string? elementRecordName, AttributeConstraintCodeStrategy elementStrategy)
    {
        if (string.IsNullOrEmpty(elementRecordName))
        {
            return "Value";
        }

        return elementStrategy.GetTypedArrayElementPayloadPropertyName();
    }

    private static void EmitStandardConstraint(StringBuilder builder, AttributeConstraintModel attributeConstraint, AttributeConstraintCodeStrategy strategy)
    {
        var className = DialectGeneratorNaming.GetAttributeConstraintClassName(attributeConstraint);
        var baseType = strategy.GetBaseType(attributeConstraint.RecordName);
        builder.AppendLine("public sealed class " + className + " : " + baseType);
        builder.AppendLine("{");
        builder.AppendLine("    public static AttributeConstraintDefinition AttributeConstraintDefinition { get; } =");
        builder.Append("        new AttributeConstraintDefinition(");
        builder.Append(EmitterHelpers.ToCSharpStringLiteral(attributeConstraint.Name));
        var assemblyFormatExpression = strategy.GetAssemblyFormatConstructionExpression(attributeConstraint.RecordName);
        if (assemblyFormatExpression != null)
        {
            builder.Append(", " + assemblyFormatExpression);
        }
        else
        {
            var assemblyFormatType = strategy.GetAssemblyFormatType(attributeConstraint.RecordName);
            if (assemblyFormatType != null)
            {
                builder.Append(", new " + assemblyFormatType + "()");
            }
        }

        builder.AppendLine(", factory: static context => new " + className + "(context));");
        builder.AppendLine();
        builder.AppendLine("    public " + className + "(AttributeValueConstructionContext context)");
        var primitiveBaseConstructor = strategy.GetPrimitiveBaseConstructor(attributeConstraint.RecordName);
        if (primitiveBaseConstructor != null)
        {
            builder.AppendLine("        : base(" + primitiveBaseConstructor + ")");
        }
        else
        {
            builder.AppendLine("        : base(context.Syntax, context.Location)");
        }
        builder.AppendLine("    {");
        builder.AppendLine("    }");

        var valueConstructorParam = strategy.GetValueConstructorParameter(attributeConstraint.RecordName);
        if (valueConstructorParam != null)
        {
            builder.AppendLine();
            builder.AppendLine("    public " + className + "(" + valueConstructorParam + " value)");
            builder.AppendLine("        : base(value)");
            builder.AppendLine("    {");
            builder.AppendLine("    }");
        }

        strategy?.EmitInnerHelpers(builder, className);

        builder.AppendLine();
        builder.AppendLine("    public override string? Name => AttributeConstraintDefinition.Name;");
        builder.AppendLine("    public override AttributeConstraintDefinition? Definition => AttributeConstraintDefinition;");
        builder.AppendLine("}");
    }
}
