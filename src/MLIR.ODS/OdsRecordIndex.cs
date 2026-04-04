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
        var unnamedNonVariadicCount = 0;
        foreach (var argument in dag.Arguments)
        {
            if (argument.Name == null)
            {
                // Unnamed variadic results appear in upstream MLIR (for example func.call).
                // Preserve them with a synthesized "results" name so later layers still
                // understand that the operation can produce arbitrary result arity.
                if (kind == OperationMemberKind.Result && IsVariadicValue(argument.Value))
                {
                    members.Add(new DagMemberModel("results", GetConstraintName(argument.Value), kind, isVariadic: true));
                    continue;
                }

                // Variadic unnamed operands still have no well-defined cardinality in the
                // current model, so skip them rather than producing incorrect fixed-count
                // checks.
                if (IsVariadicValue(argument.Value))
                {
                    continue;
                }

                // Attributes in the `ins` dag are handled below; unnamed attributes in the
                // `outs` dag are unusual so synthesize a result name and continue.
                var constraintNameForUnnamed = GetConstraintName(argument.Value);
                if (kind == OperationMemberKind.Operand && constraintNameForUnnamed != null && IsAttributeConstraint(constraintNameForUnnamed))
                {
                    // Unnamed attributes in `ins` are rare; skip to avoid confusing the model.
                    continue;
                }

                var syntheticName = "result_" + unnamedNonVariadicCount.ToString(System.Globalization.CultureInfo.InvariantCulture);
                unnamedNonVariadicCount++;
                members.Add(new DagMemberModel(syntheticName, constraintNameForUnnamed, kind));
                continue;
            }

            var constraintName = GetConstraintName(argument.Value);
            var memberKind = kind;
            if (kind == OperationMemberKind.Operand
                && constraintName != null
                && IsAttributeConstraint(constraintName))
            {
                memberKind = OperationMemberKind.Attribute;
            }

            var isVariadic = IsVariadicValue(argument.Value);
            members.Add(new DagMemberModel(argument.Name, constraintName, memberKind, isVariadic));
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

        if (record.HasBaseClass("TypedArrayAttrBase"))
        {
            kind = AttributeConstraintKind.TypedArrayAttribute;
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

        // EnumAttrInfo (old-style I32EnumAttr, I64EnumAttr, I32BitEnumAttr, etc.) should be
        // classified as enum attributes before the generic integer-literal fallthrough.
        if (record.HasBaseClass("EnumAttrInfo"))
        {
            kind = AttributeConstraintKind.EnumAttribute;
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

    public bool TryGetTypeConstraintKind(Record record, out TypeConstraintKind kind, out string? canonicalTypeName)
    {
        canonicalTypeName = null;

        if (TryGetExactIntegerConstraintName(record.Name, out canonicalTypeName))
        {
            kind = TypeConstraintKind.ExactInteger;
            return true;
        }

        if (TryGetExactFloatConstraintName(record.Name, out canonicalTypeName))
        {
            kind = TypeConstraintKind.ExactFloat;
            return true;
        }

        if (record.Name == "Index")
        {
            kind = TypeConstraintKind.IndexType;
            canonicalTypeName = "index";
            return true;
        }

        if (record.Name == "NoneType")
        {
            kind = TypeConstraintKind.NoneType;
            canonicalTypeName = "none";
            return true;
        }

        if (record.Name == "AnyTuple")
        {
            kind = TypeConstraintKind.TupleType;
            canonicalTypeName = "tuple";
            return true;
        }

        if (record.Name == "FunctionType")
        {
            kind = TypeConstraintKind.FunctionType;
            canonicalTypeName = "function";
            return true;
        }

        if (record.Name == "AnyTensor")
        {
            kind = TypeConstraintKind.TensorType;
            canonicalTypeName = "tensor";
            return true;
        }

        if (record.Name == "AnyVectorOfAnyRank")
        {
            kind = TypeConstraintKind.VectorType;
            canonicalTypeName = "vector";
            return true;
        }

        if (record.Name == "AnyMemRef")
        {
            kind = TypeConstraintKind.MemRefType;
            canonicalTypeName = "memref";
            return true;
        }

        if (record.HasBaseClass("Type"))
        {
            kind = TypeConstraintKind.None;
            return true;
        }

        kind = TypeConstraintKind.None;
        return false;
    }

    /// <summary>
    /// Attempts to build an <see cref="EnumModel"/> from a record that represents an enum definition
    /// (<c>EnumAttrInfo</c> or any of its subclasses such as <c>I64EnumAttr</c> / <c>I32BitEnumAttr</c>).
    /// </summary>
    public bool TryGetEnumModel(Record record, out Model.EnumModel? enumModel)
    {
        if (!record.HasBaseClass("EnumInfo"))
        {
            enumModel = null;
            return false;
        }

        if (!TryGetStringField(record, "className", out var className) || string.IsNullOrEmpty(className))
        {
            enumModel = null;
            return false;
        }

        var cppNamespace = GetOptionalStringField(record, "cppNamespace");
        var bitwidth = 64;
        if (record.Fields.TryGetValue("bitwidth", out var bwField) && bwField is TableGen.Evaluation.IntegerValue bwInt)
        {
            bitwidth = bwInt.Value;
        }

        var isBitEnum = record.HasBaseClass("BitEnumBase");
        var separator = "|";
        if (isBitEnum)
        {
            separator = GetOptionalStringField(record, "separator") ?? "|";
        }

        var cases = ReadEnumCases(record);
        enumModel = new Model.EnumModel(className, cppNamespace, bitwidth, isBitEnum, separator, cases);
        return true;
    }

    private IReadOnlyList<Model.EnumCaseModel> ReadEnumCases(Record record)
    {
        if (!record.Fields.TryGetValue("enumerants", out var enumerantsField)
            || enumerantsField is not ListValue list)
        {
            return EmptyEnumCases;
        }

        var cases = new List<Model.EnumCaseModel>(list.Items.Count);
        foreach (var item in list.Items)
        {
            if (TryReadEnumCase(item, out var caseModel))
            {
                cases.Add(caseModel!);
            }
        }

        return cases;
    }

    private bool TryReadEnumCase(Value item, out Model.EnumCaseModel? caseModel)
    {
        IReadOnlyDictionary<string, Value>? caseFields = null;

        if (item is AnonymousRecordValue anon)
        {
            caseFields = anon.Fields;
        }
        else if (item is RecordReferenceValue recordRef && recordsByName.TryGetValue(recordRef.RecordName, out var caseRecord))
        {
            caseFields = caseRecord.Fields;
        }

        if (caseFields == null)
        {
            caseModel = null;
            return false;
        }

        if (!caseFields.TryGetValue("symbol", out var symField) || symField is not StringValue symStr)
        {
            caseModel = null;
            return false;
        }

        var symbol = symStr.Value;

        // The 'str' field is the text representation; fall back to 'symbol' when absent or equal to the
        // literal parameter name "sym" (which can happen before the evaluator fix is in effect).
        var str = symbol;
        if (caseFields.TryGetValue("str", out var strField) && strField is StringValue strStr
            && !string.IsNullOrEmpty(strStr.Value) && strStr.Value != "sym")
        {
            str = strStr.Value;
        }

        var value = 0L;
        if (caseFields.TryGetValue("value", out var valField) && valField is IntegerValue valInt)
        {
            value = valInt.Value;
        }

        caseModel = new Model.EnumCaseModel(symbol, str, value);
        return true;
    }

    public sealed class DagMemberModel
    {
        public DagMemberModel(string name, string? constraintRecordName, OperationMemberKind kind, bool isVariadic = false)
        {
            Name = name;
            ConstraintRecordName = constraintRecordName;
            Kind = kind;
            IsVariadic = isVariadic;
        }

        public string Name { get; }

        public string? ConstraintRecordName { get; }

        public OperationMemberKind Kind { get; }

        /// <summary>
        /// Gets a value indicating whether this member accepts zero or more values (variadic).
        /// </summary>
        public bool IsVariadic { get; }
    }

    private static string? GetConstraintName(Value value)
    {
        return value switch
        {
            SymbolReferenceValue symbol => symbol.SymbolName,
            RecordReferenceValue record => record.RecordName,
            StringValue str => str.Value,
            AnonymousRecordValue anonymous when anonymous.Fields.TryGetValue("baseAttr", out var baseAttr) => GetConstraintName(baseAttr),
            AnonymousRecordValue anonymous when anonymous.Fields.TryGetValue("baseType", out var baseType) => GetConstraintName(baseType),
            AnonymousRecordValue anonymous => anonymous.ClassName,
            _ => null,
        };
    }

    /// <summary>
    /// Returns true when <paramref name="value"/> represents a <c>Variadic&lt;T&gt;</c>
    /// or <c>VariadicOfVariadic&lt;T, …&gt;</c> type constraint.
    /// These constraints have no fixed cardinality, so they cannot be mapped to a
    /// single-element member in the generated code.
    /// </summary>
    private static bool IsVariadicValue(Value value)
    {
        // Both Variadic<T> and VariadicOfVariadic<T,…> are AnonymousRecordValues whose
        // class name starts with "Variadic".
        return value is AnonymousRecordValue anon &&
               anon.ClassName.StartsWith("Variadic", StringComparison.Ordinal);
    }

    private bool IsAttributeConstraint(string constraintName)
    {
        if (constraintName.EndsWith("Attr", StringComparison.Ordinal))
        {
            return true;
        }

        if (!TryGetRecord(constraintName, out var constraintRecord))
        {
            return false;
        }

        return TryGetAttributeConstraintKind(constraintRecord, out _)
            || constraintRecord.HasBaseClass("Attr")
            || constraintRecord.HasBaseClass("AttrInterface");
    }

    private static AttributeConstraintKind GetDenseArrayElementKind(string recordName)
    {
        return recordName switch
        {
            "DenseBoolArrayAttr" => AttributeConstraintKind.DenseBooleanArrayAttribute,
            "DenseI8ArrayAttr" or "DenseI16ArrayAttr" or "DenseI32ArrayAttr" or "DenseI64ArrayAttr"
                => AttributeConstraintKind.DenseIntegerArrayAttribute,
            "DenseF32ArrayAttr" => AttributeConstraintKind.DenseF32ArrayAttribute,
            "DenseF64ArrayAttr" => AttributeConstraintKind.DenseF64ArrayAttribute,
            _ => AttributeConstraintKind.DenseIntegerArrayAttribute,
        };
    }

    private static bool TryGetExactIntegerConstraintName(string recordName, out string? canonicalTypeName)
    {
        canonicalTypeName = null;
        if (recordName.Length < 2)
        {
            return false;
        }

        if (recordName[0] == 'I' && HasOnlyDigits(recordName, 1))
        {
            canonicalTypeName = "i" + recordName.Substring(1);
            return true;
        }

        if (recordName.Length >= 3
            && recordName[0] == 'S'
            && recordName[1] == 'I'
            && HasOnlyDigits(recordName, 2))
        {
            canonicalTypeName = "si" + recordName.Substring(2);
            return true;
        }

        if (recordName.Length >= 3
            && recordName[0] == 'U'
            && recordName[1] == 'I'
            && HasOnlyDigits(recordName, 2))
        {
            canonicalTypeName = "ui" + recordName.Substring(2);
            return true;
        }

        return false;
    }

    private static bool TryGetExactFloatConstraintName(string recordName, out string? canonicalTypeName)
    {
        canonicalTypeName = recordName switch
        {
            "F16" => "f16",
            "F32" => "f32",
            "F64" => "f64",
            "F80" => "f80",
            "F128" => "f128",
            "BF16" => "bf16",
            "TF32" => "tf32",
            _ => null,
        };

        return canonicalTypeName != null;
    }

    private static bool HasOnlyDigits(string text, int startIndex)
    {
        if (startIndex >= text.Length)
        {
            return false;
        }

        for (var i = startIndex; i < text.Length; i++)
        {
            if (!char.IsDigit(text[i]))
            {
                return false;
            }
        }

        return true;
    }

    private static readonly IReadOnlyList<string> EmptyStrings = new string[0];
    private static readonly IReadOnlyList<DagMemberModel> EmptyDagMembers = new DagMemberModel[0];
    private static readonly IReadOnlyList<Model.EnumCaseModel> EmptyEnumCases = new Model.EnumCaseModel[0];
}
