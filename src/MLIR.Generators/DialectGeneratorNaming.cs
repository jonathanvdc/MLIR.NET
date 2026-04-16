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

    /// <summary>
    /// Projects a dialect model into the public C# namespace for generated output.
    /// </summary>
    /// <remarks>
    /// Upstream <c>cppNamespace</c> is treated as provenance metadata, not as the final C#
    /// namespace.  The projection rules are:
    /// <list type="bullet">
    ///   <item>Prelude (shared constraints) → <c>MLIR.Dialects.Prelude</c></item>
    ///   <item>Any <c>::mlir::*</c> dialect → <c>MLIR.Dialects.&lt;PascalCase(dialect.Name)&gt;</c></item>
    ///   <item>Non-MLIR or unknown fallback → <c>MLIR.Generated.&lt;PascalCase(dialect.Name)&gt;</c></item>
    /// </list>
    /// This keeps generated dialect APIs predictably under <c>MLIR.Dialects.*</c> regardless of
    /// how the upstream C++ namespace is spelled, and avoids polluting the root <c>MLIR</c> namespace.
    /// </remarks>
    public static string GetGeneratedNamespace(DialectModel dialect)
    {
        // Prelude always lives in MLIR.Dialects.Prelude regardless of cppNamespace.
        if (dialect.IsPrelude)
        {
            return "MLIR.Dialects.Prelude";
        }

        // Determine whether this is an upstream MLIR dialect by inspecting cppNamespace.
        // Any namespace whose first segment is "mlir" (case-insensitive) is treated as an
        // MLIR dialect and projected into MLIR.Dialects.<PascalCase(dialect.Name)>.
        var cppNamespace = dialect.CppNamespace;
        if (!string.IsNullOrWhiteSpace(cppNamespace))
        {
            var firstSegment = cppNamespace!
                .Split(new[] { "::" }, StringSplitOptions.None)
                .FirstOrDefault(static s => !string.IsNullOrWhiteSpace(s))
                ?.Trim();

            if (string.Equals(firstSegment, "mlir", StringComparison.OrdinalIgnoreCase))
            {
                return "MLIR.Dialects." + ToPascalCase(dialect.Name);
            }
        }

        // Non-MLIR dialects fall back to MLIR.Generated.<PascalCase(dialect.Name)>.
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

    public static string GetAttributeConstraintClassName(AttributeConstraintModel attributeConstraint)
    {
        return ToPascalCase(attributeConstraint.RecordName.Replace('.', '_')) + "ConstraintAttributeValue";
    }

    public static string GetTypeClassName(TypeModel type)
    {
        return type.ClassName ?? ToPascalCase(type.Name.Replace('.', '_')) + "TypeReference";
    }

    public static string GetTypeConstraintClassName(TypeConstraintModel typeConstraint)
    {
        return ToPascalCase(typeConstraint.RecordName.Replace('.', '_')) + "ConstraintTypeReference";
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
