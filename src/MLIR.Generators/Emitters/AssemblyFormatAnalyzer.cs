namespace MLIR.Generators.Emitters;

using System;
using System.Collections.Generic;
using MLIR.ODS.Model;
using MLIR.ODS.Model.AssemblyFormat;

/// <summary>
/// Analyzes a declarative assembly format to determine which variables are required
/// (always present) versus optional (conditionally present).
/// </summary>
internal static class AssemblyFormatAnalyzer
{
    /// <summary>
    /// Returns the set of variable names that are unconditionally required by the assembly format.
    /// A variable is required when it appears directly at the top level of the format, outside
    /// any <see cref="OptionalGroup"/> or <see cref="OilistDirectiveChunk"/>.
    /// </summary>
    /// <remarks>
    /// When <paramref name="operation"/> has no declarative assembly format an empty set is
    /// returned, which causes all variables to be treated as optional.
    /// </remarks>
    public static HashSet<string> GetRequiredVariables(OperationModel operation)
    {
        if (operation.AssemblyFormat is null)
        {
            return new HashSet<string>(StringComparer.Ordinal);
        }

        var required = new HashSet<string>(StringComparer.Ordinal);
        foreach (var element in operation.AssemblyFormat.Elements)
        {
            CollectRequiredVariables(element, required);
        }

        return required;
    }

    private static void CollectRequiredVariables(Element element, HashSet<string> required)
    {
        // Only a VariableChunk that appears directly at the top level of the format is
        // required. Variables inside OptionalGroup or OilistDirectiveChunk are conditional
        // and therefore optional.
        if (element is VariableChunk variable)
        {
            required.Add(variable.Name);
        }

        // All other element types are either non-variable directives (literals, attr-dict,
        // type(...), etc.) or conditional containers (OptionalGroup, OilistDirectiveChunk)
        // and do not contribute to the required variable set.
    }
}
