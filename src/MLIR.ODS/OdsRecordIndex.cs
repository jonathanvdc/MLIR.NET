namespace MLIR.ODS;

using System;
using System.Collections.Generic;
using System.Linq;
using TableGen.Evaluation;
using MLIR.ODS.Model;

internal sealed class OdsRecordIndex
{
    private readonly IReadOnlyList<Record> records;
    private readonly Dictionary<string, Record> recordsByName;
    private readonly Dictionary<string, IReadOnlyList<Record>> recordsByBaseClass;

    public OdsRecordIndex(InterpretedDocument document)
    {
        records = document.Records;
        recordsByName = new Dictionary<string, Record>(StringComparer.Ordinal);
        recordsByBaseClass = new Dictionary<string, IReadOnlyList<Record>>(StringComparer.Ordinal);

        var baseClassBuckets = new Dictionary<string, List<Record>>(StringComparer.Ordinal);
        foreach (var record in document.Records)
        {
            recordsByName[record.Name] = record;
            foreach (var baseClass in record.BaseClasses)
            {
                if (!baseClassBuckets.TryGetValue(baseClass, out var bucket))
                {
                    bucket = new List<Record>();
                    baseClassBuckets.Add(baseClass, bucket);
                }

                bucket.Add(record);
            }
        }

        foreach (var pair in baseClassBuckets)
        {
            recordsByBaseClass[pair.Key] = pair.Value;
        }
    }

    public IReadOnlyList<Record> Records => records;

    public bool TryGetRecord(string name, out Record record)
    {
        return recordsByName.TryGetValue(name, out record!);
    }

    public bool HasBaseClass(Record record, string baseClass)
    {
        return record.HasBaseClass(baseClass);
    }

    public IEnumerable<Record> GetRecordsWithBaseClass(string baseClass)
    {
        return recordsByBaseClass.TryGetValue(baseClass, out var matches) ? matches : Array.Empty<Record>();
    }

    public bool TryGetStringField(Record record, string fieldName, out string value)
    {
        if (record.Fields.TryGetValue(fieldName, out var field) && field is StringValue stringValue)
        {
            value = stringValue.Value;
            return true;
        }

        value = string.Empty;
        return false;
    }

    public string? GetOptionalStringField(Record record, string fieldName)
    {
        if (!record.Fields.TryGetValue(fieldName, out var field))
        {
            return null;
        }

        return field switch
        {
            StringValue { Value.Length: 0 } => null,
            StringValue stringValue => stringValue.Value,
            SymbolReferenceValue symbol => symbol.SymbolName,
            RecordReferenceValue recordReference => recordReference.RecordName,
            _ => null,
        };
    }

    public IReadOnlyList<string> GetStringListField(Record record, string fieldName)
    {
        if (!record.Fields.TryGetValue(fieldName, out var field) || field is not ListValue list)
        {
            return EmptyStrings;
        }

        var values = new List<string>(list.Items.Count);
        foreach (var item in list.Items)
        {
            switch (item)
            {
                case StringValue stringValue:
                    values.Add(stringValue.Value);
                    break;
                case SymbolReferenceValue symbol:
                    values.Add(symbol.SymbolName);
                    break;
                case RecordReferenceValue recordReference:
                    values.Add(recordReference.RecordName);
                    break;
            }
        }

        return values;
    }

    public bool TryGetDialectName(Record record, out string dialectName)
    {
        if (!record.Fields.TryGetValue("dialect", out var dialectField)
            && !record.Fields.TryGetValue("opDialect", out dialectField))
        {
            dialectName = string.Empty;
            return false;
        }

        if (dialectField is RecordReferenceValue recordReference
            && TryGetRecord(recordReference.RecordName, out var dialectRecord)
            && TryGetStringField(dialectRecord, "name", out dialectName))
        {
            return true;
        }

        if (dialectField is StringValue stringValue)
        {
            dialectName = stringValue.Value;
            return true;
        }

        dialectName = string.Empty;
        return false;
    }

    public bool TryGetOperationName(Record record, out string operationName)
    {
        return TryGetStringField(record, "mnemonic", out operationName)
            || TryGetStringField(record, "opName", out operationName);
    }

    public IReadOnlyList<DagMemberModel> GetDagMembers(Record record, string fieldName)
    {
        if (!record.Fields.TryGetValue(fieldName, out var field) || field is not DagValue dag)
        {
            return EmptyDagMembers;
        }

        var kind = dag.OperatorName switch
        {
            "ins" => OperationMemberKind.Operand,
            "outs" => OperationMemberKind.Result,
            _ => OperationMemberKind.Operand,
        };

        var members = new List<DagMemberModel>(dag.Arguments.Count);
        foreach (var argument in dag.Arguments)
        {
            if (argument.Name == null)
            {
                continue;
            }

            var constraintName = GetConstraintName(argument.Value);
            var memberKind = kind;
            if (kind == OperationMemberKind.Operand
                && constraintName != null
                && constraintName.EndsWith("Attr", StringComparison.Ordinal))
            {
                memberKind = OperationMemberKind.Attribute;
            }

            members.Add(new DagMemberModel(argument.Name, constraintName, memberKind));
        }

        return members;
    }

    public bool TryGetAttributeConstraintKind(Record record, out AttributeConstraintKind kind)
    {
        if (record.Name == "BoolAttr")
        {
            kind = AttributeConstraintKind.BooleanLiteral;
            return true;
        }

        if (record.HasBaseClass("DenseArrayAttrBase"))
        {
            kind = GetDenseArrayElementKind(record.Name);
            return true;
        }

        if (record.Name == "ElementsAttr"
            || record.Name == "AnyIntElementsAttr"
            || record.Name == "AnyI32ElementsAttr"
            || record.Name == "AnyI64ElementsAttr")
        {
            kind = AttributeConstraintKind.ElementsAttribute;
            return true;
        }

        if (record.Name == "DictionaryAttr")
        {
            kind = AttributeConstraintKind.DictionaryAttribute;
            return true;
        }

        if (record.Name == "LocationAttr"
            || record.Name == "AnyAttr")
        {
            kind = AttributeConstraintKind.OpaqueAttribute;
            return true;
        }

        if (record.HasBaseClass("AnyIntegerAttrBase")
            || record.Name == "APIntAttr"
            || record.Name == "IndexAttr"
            || record.HasBaseClass("SignlessIntegerAttrBase")
            || record.HasBaseClass("TypedSignlessIntegerAttrBase")
            || record.HasBaseClass("SignedIntegerAttrBase")
            || record.HasBaseClass("TypedSignedIntegerAttrBase")
            || record.HasBaseClass("UnsignedIntegerAttrBase")
            || record.HasBaseClass("TypedUnsignedIntegerAttrBase"))
        {
            kind = AttributeConstraintKind.IntegerLiteral;
            return true;
        }

        if (record.HasBaseClass("FloatAttrBase") || record.Name == "BF16Attr")
        {
            kind = AttributeConstraintKind.FloatingPointLiteral;
            return true;
        }

        if (record.HasBaseClass("StringBasedAttr"))
        {
            kind = AttributeConstraintKind.StringLiteral;
            return true;
        }

        if (record.Name == "TypeAttr")
        {
            kind = AttributeConstraintKind.TypeAttribute;
            return true;
        }

        if (record.Name == "UnitAttr")
        {
            kind = AttributeConstraintKind.UnitAttribute;
            return true;
        }

        kind = AttributeConstraintKind.None;
        return false;
    }

    public sealed class DagMemberModel
    {
        public DagMemberModel(string name, string? constraintRecordName, OperationMemberKind kind)
        {
            Name = name;
            ConstraintRecordName = constraintRecordName;
            Kind = kind;
        }

        public string Name { get; }

        public string? ConstraintRecordName { get; }

        public OperationMemberKind Kind { get; }
    }

    private static string? GetConstraintName(Value value)
    {
        return value switch
        {
            SymbolReferenceValue symbol => symbol.SymbolName,
            RecordReferenceValue record => record.RecordName,
            StringValue str => str.Value,
            _ => null,
        };
    }

    private static AttributeConstraintKind GetDenseArrayElementKind(string recordName)
    {
        return recordName switch
        {
            "DenseBoolArrayAttr" => AttributeConstraintKind.DenseBooleanArrayAttribute,
            "DenseI8ArrayAttr" or "DenseI16ArrayAttr" or "DenseI32ArrayAttr" or "DenseI64ArrayAttr"
                => AttributeConstraintKind.DenseIntegerArrayAttribute,
            "DenseF32ArrayAttr" => AttributeConstraintKind.DenseSinglePrecisionArrayAttribute,
            "DenseF64ArrayAttr" => AttributeConstraintKind.DenseDoublePrecisionArrayAttribute,
            _ => AttributeConstraintKind.DenseIntegerArrayAttribute,
        };
    }

    private static readonly IReadOnlyList<string> EmptyStrings = new string[0];
    private static readonly IReadOnlyList<DagMemberModel> EmptyDagMembers = new DagMemberModel[0];
}
