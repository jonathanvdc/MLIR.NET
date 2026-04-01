namespace MLIR.ODS.Model;

using System;
using System.Collections.Generic;
using MLIR.ODS.Model.AssemblyFormat;

/// <summary>
/// Analyzes a declarative assembly format to determine which variables are required
/// (always present) versus optional (conditionally present).
/// </summary>
public static class AssemblyFormatAnalyzer
{
    /// <summary>
    /// Returns the set of variable names that are unconditionally required by the assembly format.
    /// A variable is required when it appears directly at the top level of the format, outside
    /// any <see cref="OptionalGroup"/> or <see cref="OilistDirectiveChunk"/>.
    /// </summary>
    /// <remarks>
    /// When <paramref name="operation"/> has no declarative assembly format an empty set is
    /// returned. An empty required set is the safe default: all variables are then treated as
    /// optional, so generated properties are nullable and registration calls use
    /// <c>OptionalAttribute</c>. This avoids false required-variable claims for operations
    /// whose format is not known statically.
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
        // A VariableChunk that appears directly at the top level of the format is required.
        if (element is VariableChunk variable)
        {
            required.Add(variable.Name);
            return;
        }

        // All non-variable top-level elements fall into one of two categories:
        //
        // 1. Leaf directives that contain no child Elements: LiteralChunk, TypeDirectiveChunk,
        //    AttrDictDirectiveChunk, AttrDictWithKeywordDirectiveChunk, PropDictDirectiveChunk,
        //    RegionsDirectiveChunk, SuccessorsDirectiveChunk, OperandsDirectiveChunk,
        //    ResultsDirectiveChunk, QualifiedDirectiveChunk, FunctionalTypeDirectiveChunk,
        //    CustomDirectiveChunk, RefDirectiveChunk.
        //    These do not contain child Element instances (their operands, if any, are
        //    DirectiveOperand nodes which are a separate type hierarchy), so no recursion
        //    is needed for them.
        //
        // 2. Conditional containers: OptionalGroup and OilistDirectiveChunk.
        //    Variables inside these containers are only conditionally present, so they are
        //    NOT required and we intentionally do not recurse into them.
    }
}
