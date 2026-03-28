namespace MLIR.Generators;

using System.Text;
using MLIR.ODS.Model;

internal static class OdsDialectGeneratorNaming
{
    public static string GetHintName(OdsDialectModel dialect)
    {
        return ToPascalCase(dialect.Name) + "DialectRegistration.g.cs";
    }

    public static string GetGeneratedNamespace(OdsDialectModel dialect)
    {
        return "MLIR.Generated." + ToPascalCase(dialect.Name);
    }

    public static string GetDialectRegistrationClassName(OdsDialectModel dialect)
    {
        return ToPascalCase(dialect.Name) + "DialectRegistration";
    }

    public static string GetOperationClassName(OdsOperationModel operation)
    {
        return operation.ClassName ?? ToPascalCase(operation.Name.Replace('.', '_')) + "Operation";
    }

    public static string GetAttributeClassName(OdsAttributeModel attribute)
    {
        return attribute.ClassName ?? ToPascalCase(attribute.Name.Replace('.', '_')) + "AttributeValue";
    }

    public static string GetTypeClassName(OdsTypeModel type)
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
}
