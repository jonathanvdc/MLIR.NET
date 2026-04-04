namespace MLIR.Generators.Emitters;

using MLIR.ODS.Model;

internal static class AttributeTypeResolver
{
    public static string? GetAttributeValueTypeName(string? constraintRecordName, DialectSymbolResolver resolver)
    {
        if (string.IsNullOrEmpty(constraintRecordName))
        {
            return null;
        }

        var nonNullRecordName = constraintRecordName!;
        var constraintKind = resolver.TryResolveAttributeConstraintKind(nonNullRecordName);
        var enumTypeName = resolver.TryResolveEnumTypeName(nonNullRecordName);
        return GetAttributeValueTypeName(nonNullRecordName, constraintKind, enumTypeName, resolver);
    }

    private static string? GetAttributeValueTypeName(
        string constraintRecordName,
        AttributeConstraintKind kind,
        string? enumTypeName,
        DialectSymbolResolver resolver)
    {
        if (kind == AttributeConstraintKind.UnitAttribute)
        {
            return "UnitAttributeValue";
        }

        if (kind == AttributeConstraintKind.EnumAttribute && !string.IsNullOrEmpty(enumTypeName))
        {
            return enumTypeName;
        }

        return kind switch
        {
            AttributeConstraintKind.IntegerLiteral => "BigInteger",
            AttributeConstraintKind.BooleanLiteral => "bool",
            AttributeConstraintKind.StringLiteral => "string",
            AttributeConstraintKind.FloatingPointLiteral => constraintRecordName switch
            {
                "F32Attr" => "float",
                "F64Attr" => "double",
                _ => "string",
            },
            AttributeConstraintKind.DenseBooleanArrayAttribute => "IReadOnlyList<bool>",
            AttributeConstraintKind.DenseIntegerArrayAttribute => "IReadOnlyList<BigInteger>",
            AttributeConstraintKind.DenseF32ArrayAttribute => "IReadOnlyList<float>",
            AttributeConstraintKind.DenseF64ArrayAttribute => "IReadOnlyList<double>",
            AttributeConstraintKind.ElementsAttribute => "ElementsAttributeValue",
            AttributeConstraintKind.DictionaryAttribute => "NamedAttributeCollection",
            AttributeConstraintKind.TypeAttribute => "TypeSyntax",
            AttributeConstraintKind.OpaqueAttribute => "OpaqueAttributeValue",
            AttributeConstraintKind.TypedArrayAttribute => GetTypedArrayValueTypeName(constraintRecordName, resolver),
            _ => null,
        };
    }

    private static string? GetTypedArrayValueTypeName(string constraintRecordName, DialectSymbolResolver resolver)
    {
        var elementRecordName = resolver.TryResolveAttributeConstraintElementRecordName(constraintRecordName);
        if (string.IsNullOrEmpty(elementRecordName))
        {
            return "IReadOnlyList<AttributeValue>";
        }

        var elementTypeName = GetAttributeValueTypeName(
            elementRecordName!,
            resolver.TryResolveAttributeConstraintKind(elementRecordName!),
            resolver.TryResolveEnumTypeName(elementRecordName!),
            resolver);

        if (elementTypeName is null
            || IsTypedArrayFallbackElementType(elementTypeName))
        {
            return "IReadOnlyList<AttributeValue>";
        }

        return "IReadOnlyList<" + elementTypeName + ">";
    }

    private static bool IsTypedArrayFallbackElementType(string elementTypeName)
    {
        return elementTypeName == "UnitAttributeValue"
            || elementTypeName == "OpaqueAttributeValue"
            || elementTypeName == "ElementsAttributeValue";
    }
}
