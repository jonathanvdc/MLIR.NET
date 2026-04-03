namespace TableGen.Evaluation;

using System.Collections.Generic;
using System.Linq;

internal static class ValueFingerprint
{
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

    public static string Create(IReadOnlyList<Value> values)
    {
        return string.Join("|", values.Select(Create));
    }

    private static string CreateDagArgument(DagArgumentValue argument)
    {
        return argument.Name == null
            ? Create(argument.Value)
            : $"{argument.Name}={Create(argument.Value)}";
    }
}
