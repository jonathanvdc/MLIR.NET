namespace MLIR.Semantics;

using System.Collections.Generic;
using MLIR.Dialects;

/// <summary>
/// Verifies semantic modules using the operation definitions attached during binding.
/// </summary>
public static class Verifier
{
    /// <summary>
    /// Verifies a semantic module.
    /// </summary>
    /// <param name="module">The module to verify.</param>
    /// <returns>The verification result.</returns>
    public static VerificationResult Verify(Module module)
    {
        var diagnostics = new List<VerificationDiagnostic>();
        foreach (var operation in module.Operations)
        {
            VerifyOperation(operation, diagnostics);
        }

        return new VerificationResult(diagnostics);
    }

    private static void VerifyOperation(OperationBase operation, List<VerificationDiagnostic> diagnostics)
    {
        if (operation.Definition != null)
        {
            var context = new VerificationContext(operation, diagnostics);
            VerifyStructuralConstraints(operation, operation.Definition, context);
            operation.Definition.Verifier?.Verify(operation, context);
        }

        foreach (var region in operation.Regions)
        {
            foreach (var block in region.Blocks)
            {
                foreach (var nestedOperation in block.Operations)
                {
                    VerifyOperation(nestedOperation, diagnostics);
                }
            }
        }
    }

    private static void VerifyStructuralConstraints(OperationBase operation, OperationDefinition definition, VerificationContext context)
    {
        VerifyCount("operand", definition.OperandDefinitions, definition.OperandCount, operation.Operands.Count, operation, context);
        VerifyCount("result", definition.ResultDefinitions, definition.ResultCount, operation.Results.Count, operation, context);
        VerifyCount("region", definition.RegionDefinitions, definition.RegionCount, operation.Regions.Count, operation, context);
        VerifyCount("successor", definition.SuccessorDefinitions, definition.SuccessorCount, operation.Successors.Count, operation, context);

        foreach (var attributeName in definition.RequiredAttributes)
        {
            if (!operation.HasAttribute(attributeName))
            {
                context.Report($"'{operation.Name}' requires the '{attributeName}' attribute.");
            }
        }

        foreach (var attribute in definition.AttributeDefinitions)
        {
            if (attribute.IsRequired && !operation.HasAttribute(attribute.Name))
            {
                context.Report($"'{operation.Name}' requires the '{attribute.Name}' attribute.");
            }
        }
    }

    private static void VerifyCount(
        string noun,
        IReadOnlyList<OperationSegmentDefinition> segmentDefinitions,
        int? exactCount,
        int actualCount,
        OperationBase operation,
        VerificationContext context)
    {
        if (segmentDefinitions.Count > 0)
        {
            VerifySegmentedCount(noun, segmentDefinitions, actualCount, operation, context);
            return;
        }

        VerifyExactCount(noun, exactCount, actualCount, operation, context);
    }

    private static void VerifyExactCount(string noun, int? expectedCount, int actualCount, OperationBase operation, VerificationContext context)
    {
        if (!expectedCount.HasValue || expectedCount.Value == actualCount)
        {
            return;
        }

        context.Report($"'{operation.Name}' expects exactly {expectedCount.Value} {Pluralize(noun, expectedCount.Value)} but found {actualCount}.");
    }

    private static void VerifySegmentedCount(
        string noun,
        IReadOnlyList<OperationSegmentDefinition> segmentDefinitions,
        int actualCount,
        OperationBase operation,
        VerificationContext context)
    {
        var minimumCount = 0;
        var hasVariadicSegment = false;

        foreach (var segment in segmentDefinitions)
        {
            if (segment.IsVariadic)
            {
                hasVariadicSegment = true;
            }
            else
            {
                minimumCount++;
            }
        }

        if (hasVariadicSegment)
        {
            if (actualCount < minimumCount)
            {
                context.Report(
                    $"'{operation.Name}' expects at least {minimumCount} {Pluralize(noun, minimumCount)} but found {actualCount}.");
            }

            return;
        }

        if (actualCount != minimumCount)
        {
            context.Report(
                $"'{operation.Name}' expects exactly {minimumCount} {Pluralize(noun, minimumCount)} but found {actualCount}.");
        }
    }

    private static string Pluralize(string noun, int count)
    {
        return count == 1 ? noun : noun + "s";
    }
}
