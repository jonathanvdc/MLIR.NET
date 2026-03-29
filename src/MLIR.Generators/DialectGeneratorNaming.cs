namespace MLIR.Generators;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MLIR.ODS.Model;

internal static class DialectGeneratorNaming
{
    public static string GetHintName(DialectModel dialect)
    {
        return ToPascalCase(dialect.Name) + "DialectRegistration.g.cs";
    }

    public static string GetGeneratedNamespace(DialectModel dialect)
    {
        var cppNamespace = dialect.CppNamespace;
        if (!string.IsNullOrWhiteSpace(cppNamespace))
        {
            var segments = ParseCppNamespace(cppNamespace!);
            if (segments.Count != 0)
            {
                return string.Join(".", segments);
            }
        }

        return "MLIR.Generated." + ToPascalCase(dialect.Name);
    }

    public static string GetDialectRegistrationClassName(DialectModel dialect)
    {
        return ToPascalCase(dialect.Name) + "DialectRegistration";
    }

    public static string GetOperationClassName(OperationModel operation)
    {
        return operation.ClassName ?? ToPascalCase(operation.Name.Replace('.', '_')) + "Operation";
    }

    public static string GetAttributeClassName(AttributeModel attribute)
    {
        return attribute.ClassName ?? ToPascalCase(attribute.Name.Replace('.', '_')) + "AttributeValue";
    }

    public static string GetTypeClassName(TypeModel type)
    {
        return type.ClassName ?? ToPascalCase(type.Name.Replace('.', '_')) + "TypeReference";
    }

    public static string ToPascalCase(string value)
    {
        var builder = new StringBuilder(value.Length);
        var capitalize = true;
        foreach (var c in value)
        {
            if (!char.IsLetterOrDigit(c))
            {
                capitalize = true;
                continue;
            }

            builder.Append(capitalize ? char.ToUpperInvariant(c) : c);
            capitalize = false;
        }

        return builder.Length == 0 ? "Generated" : builder.ToString();
    }

    private static IReadOnlyList<string> ParseCppNamespace(string cppNamespace)
    {
        var segments = cppNamespace
            .Split(new[] { "::" }, StringSplitOptions.None)
            .Where(static segment => !string.IsNullOrWhiteSpace(segment))
            .ToArray();

        var result = new List<string>(segments.Length);
        for (var i = 0; i < segments.Length; i++)
        {
            var segment = segments[i].Trim();
            if (i == 0 && string.Equals(segment, "mlir", StringComparison.OrdinalIgnoreCase))
            {
                result.Add("MLIR");
                continue;
            }

            result.Add(ToPascalCase(segment));
        }

        return result;
    }
}
