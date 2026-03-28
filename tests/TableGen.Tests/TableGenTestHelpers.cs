namespace TableGen.Tests;

using System.Linq;
using TableGen.Evaluation;

internal static class TableGenTestHelpers
{
    public static TableGenRecord EvaluateSingleRecord(string source)
    {
        return TableGenDocument.Parse(source).Evaluate().Records.Single();
    }
}
