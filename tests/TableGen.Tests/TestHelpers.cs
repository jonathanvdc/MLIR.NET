namespace TableGen.Tests;

using System.Linq;
using TableGen.Evaluation;

internal static class TestHelpers
{
    public static Record EvaluateSingleRecord(string source)
    {
        return Document.Parse(source).Evaluate().Records.Single();
    }
}
