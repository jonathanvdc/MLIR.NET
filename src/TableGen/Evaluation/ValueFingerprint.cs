namespace TableGen.Evaluation;

using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Produces deterministic string keys for evaluated values so class instantiations can be cached.
/// </summary>
internal static class ValueFingerprint
{
    /// <summary>
    /// Creates a fingerprint for a single value.
    /// </summary>
    /// <param name="value">The value to serialize into a cache key.</param>
    /// <returns>A stable textual fingerprint.</returns>
    public static string Create(Value value)
    {
        return value switch
        {
            IntegerValue integer => $"i:{integer.Value}",
            StringValue str => $"s:{str.Value.Length}:{str.Value}",
            BitValue bit => bit.Value ? "b:1" : "b:0",
            RecordReferenceValue record => $"r:{record.RecordName}",
            SymbolReferenceValue symbol => $"y:{symbol.SymbolName}",
            UnsetValue => "u:",
            ListValue list => $"l:[{string.Join(",", list.Items.Select(Create))}]",
            DagValue dag => $"d:{dag.OperatorName}({string.Join(",", dag.Arguments.Select(CreateDagArgument))})",
            AnonymousRecordValue record => $"a:{record.ClassName}{{{string.Join(",", record.Fields.OrderBy(static kv => kv.Key).Select(static kv => $"{kv.Key}={Create(kv.Value)}"))}}}",
            _ => value.GetType().Name,
        };
    }

    /// <summary>
    /// Creates a fingerprint for an argument list.
    /// </summary>
    /// <param name="values">The argument values to fingerprint.</param>
    /// <returns>A stable textual fingerprint for the sequence.</returns>
    public static string Create(IReadOnlyList<Value> values)
    {
        return string.Join("|", values.Select(Create));
    }

    /// <summary>
    /// Creates a fingerprint for a dag argument, preserving both the value and optional name.
    /// </summary>
    /// <param name="argument">The dag argument to fingerprint.</param>
    /// <returns>A stable textual fingerprint.</returns>
    private static string CreateDagArgument(DagArgumentValue argument)
    {
        return argument.Name == null
            ? Create(argument.Value)
            : $"{argument.Name}={Create(argument.Value)}";
    }
}
