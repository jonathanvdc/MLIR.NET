namespace MLIR.Generators;

using MLIR.ODS.Model;

internal static class DialectSourceEmitter
{
    public static GeneratedDialectSourceResult GenerateDialectSource(DialectModel dialect, DialectSymbolResolver resolver)
    {
        return new Emitters.DialectEmitter(resolver).Generate(dialect);
    }
}
