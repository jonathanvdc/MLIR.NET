namespace MLIR.Generators;

using MLIR.ODS.Model;

/// <summary>
/// Helpers for generating enum-related C# identifiers.
/// </summary>
internal static class EnumHelpers
{
    /// <summary>
    /// Gets the C# enum type name for the given <see cref="EnumModel"/>,
    /// using the model's <see cref="EnumModel.ClassName"/> in PascalCase.
    /// </summary>
    public static string GetCSharpEnumTypeName(EnumModel enumModel)
    {
        return DialectGeneratorNaming.ToPascalCase(enumModel.ClassName);
    }

    /// <summary>
    /// Gets a valid C# identifier for an enum member symbol.
    /// Numbers or keywords are prefixed with an underscore.
    /// </summary>
    public static string GetCSharpEnumMemberName(string symbol)
    {
        var pascalCase = DialectGeneratorNaming.ToPascalCase(symbol);
        if (string.IsNullOrEmpty(pascalCase))
        {
            return "_";
        }

        // Prefix with underscore if the first character is a digit.
        return char.IsDigit(pascalCase[0]) ? "_" + pascalCase : pascalCase;
    }
}
