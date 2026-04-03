namespace MLIR.ODS;

using System.Linq;
using MLIR.ODS.Model;

internal static class OperationRecordImporter
{
    public static void Import(OdsRecordIndex index, DialectModelBuilder builder)
    {
        foreach (var record in index.GetRecordsWithBaseClass("Op"))
        {
            if (!index.TryGetDialectName(record, out var opDialectName)
                || !index.TryGetOperationName(record, out var mnemonic))
            {
                continue;
            }

            var dialect = builder.GetOrCreateDialect(opDialectName);
            var argumentMembers = index.GetDagMembers(record, "arguments");
            var resultMembers = index.GetDagMembers(record, "results");
            var assemblyFormatString = index.GetOptionalStringField(record, "assemblyFormat");
            var assemblyFormat = !string.IsNullOrEmpty(assemblyFormatString)
                ? AssemblyFormatParser.Parse(assemblyFormatString!)
                : null;

            dialect.Operations.Add(
                new OperationModel(
                    opDialectName + "." + mnemonic,
                    index.GetOptionalStringField(record, "cppClassName") ?? record.Name,
                    argumentMembers.Where(static member => member.Kind == OperationMemberKind.Operand)
                        .Select(static member => new OperandModel(member.Name, member.ConstraintRecordName, isVariadic: member.IsVariadic))
                        .ToArray(),
                    resultMembers.Where(static member => member.Kind == OperationMemberKind.Result)
                        .Select(static member => new ResultModel(member.Name, member.ConstraintRecordName, isVariadic: member.IsVariadic))
                        .ToArray(),
                    argumentMembers.Where(static member => member.Kind == OperationMemberKind.Attribute)
                        .Select(static member => new AttributeUseModel(member.Name, member.ConstraintRecordName))
                        .ToArray(),
                    index.GetOptionalStringField(record, "summary"),
                    index.GetOptionalStringField(record, "description"),
                    assemblyFormat,
                    index.GetStringListField(record, "traits")));
        }
    }
}
