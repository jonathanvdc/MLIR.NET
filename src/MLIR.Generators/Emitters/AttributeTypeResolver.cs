namespace MLIR.Generators.Emitters;

internal static class AttributeTypeResolver
{
    /// <summary>
    /// Returns the C# type name for the unwrapped attribute value associated with the given
    /// constraint record, or <see langword="null"/> when no constraint record is present.
    /// </summary>
    /// <remarks>
    /// This type is used for typed-array element types and for primitive-style property access
    /// expressions.  It differs from the operation-member property type for some constraints
    /// (e.g. <c>TypeAttr</c> returns <c>"TypeReference"</c> here but <c>"TypeAttr"</c>
    /// as an operation property).
    /// </remarks>
    public static string? GetAttributeValueTypeName(string? constraintRecordName, DialectSymbolResolver resolver)
    {
        if (string.IsNullOrEmpty(constraintRecordName))
        {
            return null;
        }

        return resolver.TryResolveAttributeConstraintStrategy(constraintRecordName!).TypedArrayElementTypeName;
    }
}
