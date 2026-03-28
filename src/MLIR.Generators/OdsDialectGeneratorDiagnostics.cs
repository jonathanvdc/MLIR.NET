namespace MLIR.Generators;

using Microsoft.CodeAnalysis;

internal static class OdsDialectGeneratorDiagnostics
{
    public static readonly DiagnosticDescriptor InvalidTableGenInput = new DiagnosticDescriptor(
        id: "MLIRGEN001",
        title: "Invalid TableGen input",
        messageFormat: "Could not process TableGen input '{0}': {1}",
        category: "MLIR.Generators",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);
}
