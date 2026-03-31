namespace MLIR.Generators;

using MLIR.ODS.Model;

internal static partial class DialectSourceEmitter
{
    public static string GenerateDialectSource(DialectModel dialect)
    {
        return new Emitter().Generate(dialect);
    }
}
