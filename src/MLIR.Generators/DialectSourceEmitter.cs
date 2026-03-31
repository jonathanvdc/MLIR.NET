namespace MLIR.Generators;

using MLIR.ODS.Model;

internal static class DialectSourceEmitter
{
    public static string GenerateDialectSource(DialectModel dialect)
    {
        return new Emitters.DialectEmitter().Generate(dialect);
    }
}
