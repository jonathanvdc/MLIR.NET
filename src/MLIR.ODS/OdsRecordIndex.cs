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
                if (!baseClassBuckets.TryGetValue(baseClass.Name, out var bucket))
                {
                    bucket = new List<Record>();
                    baseClassBuckets.Add(baseClass.Name, bucket);
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

    /// <summary>
    /// Extracts the trait list stored in <paramref name="fieldName"/> on <paramref name="record"/>
    /// and returns a typed model for each trait item. Items that are record references are
    /// resolved to determine whether they are <see cref="Model.NativeTraitModel"/>,
    /// <see cref="Model.TraitListModel"/>, <see cref="Model.GenInternalTraitModel"/>, or
    /// <see cref="Model.SimpleTraitModel"/> based on the base classes of the referenced record.
    /// Items that cannot be resolved to a known trait shape are silently skipped.
    /// </summary>
    public IReadOnlyList<Model.TraitModel> GetTraitListField(Record record, string fieldName)
    {
        if (!record.Fields.TryGetValue(fieldName, out var field) || field is not ListValue list)
        {
            return EmptyTraitModels;
        }

        var traits = new List<Model.TraitModel>(list.Items.Count);
        foreach (var item in list.Items)
        {
            var traitModel = TryBuildTraitModel(item);
            if (traitModel != null)
            {
                traits.Add(traitModel);
            }
        }

        return traits;
    }

    /// <summary>
    /// Attempts to construct a <see cref="Model.TraitModel"/> from a single list-item value.
    /// Returns <see langword="null"/> for value kinds that do not carry enough information to
    /// identify a trait (e.g., plain string values in a trait list are unusual and skipped).
    /// </summary>
    private Model.TraitModel? TryBuildTraitModel(Value item)
    {
        // Anonymous record values arise from inline class instantiations such as
        // DeclareOpInterfaceMethods<MyIface, ["foo"]>. These are not top-level defs so they
        // have no record name in the index, but they carry full field and base-class information
        // through the AnonymousRecordValue itself.
        if (item is AnonymousRecordValue anon)
        {
            return TryBuildTraitModelFromAnonymous(anon);
        }

        string? recordName = null;
        Record? traitRecord = null;

        switch (item)
        {
            case RecordReferenceValue recordRef:
                recordName = recordRef.RecordName;
                TryGetRecord(recordName, out traitRecord);
                break;
            case SymbolReferenceValue symbol:
                recordName = symbol.SymbolName;
                TryGetRecord(recordName, out traitRecord);
                break;
            default:
                // Plain strings or other value kinds are not expected in a trait list.
                return null;
        }

        if (recordName == null)
        {
            return null;
        }

        if (traitRecord != null)
        {
            // InterfaceTrait (and its subclasses OpInterfaceTrait, and concrete interfaces such as
            // OpInterface, TypeInterface, AttrInterface, DialectInterface) must be recognized
            // before the generic NativeTrait check because InterfaceTrait extends NativeTrait.
            // This includes DeclareXxxInterfaceMethods wrappers, which also extend InterfaceTrait.
            if (traitRecord.HasBaseClass("InterfaceTrait"))
            {
                return BuildInterfaceTraitModel(recordName, traitRecord);
            }

            // NativeTrait (and its subclass NativeOpTrait) carries a C++ trait name and
            // namespace. Check this before GenInternalTrait because NativeTrait is more
            // specific in the class hierarchy.
            if (traitRecord.HasBaseClass("NativeTrait"))
            {
                TryGetStringField(traitRecord, "trait", out var trait);
                var cppNamespace = GetOptionalStringField(traitRecord, "cppNamespace");
                return new Model.NativeTraitModel(
                    recordName,
                    string.IsNullOrEmpty(trait) ? null : trait,
                    cppNamespace);
            }

            // TraitList groups multiple traits under a single name (e.g., Pure).
            if (traitRecord.HasBaseClass("TraitList"))
            {
                var innerTraits = GetTraitListField(traitRecord, "traits");
                return new Model.TraitListModel(recordName, innerTraits);
            }

            // GenInternalTrait affects code generation rather than mapping to a C++ trait.
            if (traitRecord.HasBaseClass("GenInternalTrait"))
            {
                TryGetStringField(traitRecord, "trait", out var trait);
                return new Model.GenInternalTraitModel(
                    recordName,
                    string.IsNullOrEmpty(trait) ? null : trait);
            }
        }

        // Fall back to a simple trait wrapper that preserves the record name for inspection.
        return new Model.SimpleTraitModel(recordName);
    }

    /// <summary>
    /// Attempts to build a <see cref="Model.TraitModel"/> from an anonymously instantiated
    /// class value (e.g., <c>DeclareOpInterfaceMethods&lt;MyIface, ["foo"]&gt;</c>).
    /// The class name is used as the record name because anonymous instantiations are not
    /// registered as named top-level defs.
    /// </summary>
    private Model.TraitModel? TryBuildTraitModelFromAnonymous(AnonymousRecordValue anon)
    {
        var className = anon.ClassName;

        // InterfaceTrait check first — covers DeclareXxxInterfaceMethods and any inline
        // interface-backed trait instantiation.
        if (AnonHasBaseClass(anon, "InterfaceTrait"))
        {
            return BuildInterfaceTraitModelFromFields(className, anon.Fields, anon.BaseClasses);
        }

        if (AnonHasBaseClass(anon, "NativeTrait"))
        {
            var trait = GetStringFromValueDictionary(anon.Fields, "trait");
            var cppNamespace = GetStringFromValueDictionary(anon.Fields, "cppNamespace");
            return new Model.NativeTraitModel(className, trait, cppNamespace);
        }

        if (AnonHasBaseClass(anon, "GenInternalTrait"))
        {
            var trait = GetStringFromValueDictionary(anon.Fields, "trait");
            return new Model.GenInternalTraitModel(className, trait);
        }

        return new Model.SimpleTraitModel(className);
    }

    /// <summary>
    /// Returns <see langword="true"/> when the anonymous record's base-class list includes a
    /// class with the given name.
    /// </summary>
    private static bool AnonHasBaseClass(AnonymousRecordValue anon, string baseClassName)
    {
        foreach (var bc in anon.BaseClasses)
        {
            if (bc.Name == baseClassName)
            {
                return true;
            }
        }

        return false;
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
            "region" or "regions" => OperationMemberKind.Region,
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

        if (record.Name == "ArrayAttr"
            || record.Name == "AffineMapAttr"
            || record.Name == "IntegerSetAttr"
            || record.Name == "SymbolRefAttr"
            || record.Name == "FlatSymbolRefAttr")
        {
            kind = AttributeConstraintKind.OpaqueAttribute;
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
            canonicalTypeName = "builtin.tuple";
            return true;
        }

        if (record.Name == "FunctionType")
        {
            kind = TypeConstraintKind.FunctionType;
            canonicalTypeName = "builtin.function";
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

    /// <summary>
    /// Extracts the ordered list of <see cref="Model.AttrOrTypeParameterModel"/> instances declared
    /// in the <c>parameters</c> dag field of an <c>AttrDef</c> or <c>TypeDef</c> record.
    /// Returns an empty list when the record has no <c>parameters</c> field or when the field is empty.
    /// </summary>
    /// <remarks>
    /// Each dag argument in the <c>parameters</c> field corresponds to one parameter. Parameters
    /// are either specified as plain C++ type strings (e.g., <c>"unsigned":$width</c>) or as
    /// instantiations of <c>AttrOrTypeParameter</c> subclasses (e.g.,
    /// <c>StringRefParameter&lt;"desc"&gt;:$name</c>). Both forms are supported.
    ///
    /// When a <c>csharpParameters</c> dag is also present (contributed directly or via a
    /// record-level <c>extends MLIRNet_AttrOrTypeDefExtension</c> overlay), its entries
    /// override the C# type information inferred from the upstream <c>parameters</c> dag.
    /// String literals in <c>csharpParameters</c> are used directly as C# type names;
    /// class instances are resolved through their <c>MLIRNet_AttrOrTypeParameterExtension</c>
    /// fields.  The upstream <c>parameters</c> field remains the source of truth for C++
    /// semantics (cppType, storage types, default values, etc.).
    /// </remarks>
    public IReadOnlyList<Model.AttrOrTypeParameterModel> GetAttrOrTypeParameters(Record record)
    {
        if (!record.Fields.TryGetValue("parameters", out var field) || field is not DagValue dag)
        {
            return EmptyAttrOrTypeParameters;
        }

        var parameters = new List<Model.AttrOrTypeParameterModel>(dag.Arguments.Count);
        foreach (var argument in dag.Arguments)
        {
            if (argument.Name == null)
            {
                continue;
            }

            var paramModel = TryBuildAttrOrTypeParameterModel(argument.Name, argument.Value);
            if (paramModel != null)
            {
                parameters.Add(paramModel);
            }
        }

        // Check for a csharpParameters DAG that overrides C# type info per parameter.
        // This field may be present directly on the record or contributed by a
        // record-level `extends MLIRNet_AttrOrTypeDefExtension` overlay; in both cases
        // it is visible through the extension-aware Fields view.
        if (parameters.Count > 0
            && record.Fields.TryGetValue("csharpParameters", out var csharpParamsField)
            && csharpParamsField is DagValue csharpParamsDag)
        {
            // Parse each csharpParameters entry into a parameter model using the same
            // TryBuildAttrOrTypeParameterModel logic, then use the resulting model's
            // C# fields as overrides.  A placeholder name is used so the lookup works.
            var overrides = new Dictionary<string, Model.AttrOrTypeParameterModel>(
                csharpParamsDag.Arguments.Count, StringComparer.Ordinal);
            foreach (var argument in csharpParamsDag.Arguments)
            {
                if (argument.Name == null)
                {
                    continue;
                }

                var csharpModel = TryBuildAttrOrTypeParameterModel(argument.Name, argument.Value);
                if (csharpModel != null)
                {
                    overrides[argument.Name] = csharpModel;
                }
            }

            for (var i = 0; i < parameters.Count; i++)
            {
                if (overrides.TryGetValue(parameters[i].Name, out var csharpModel))
                {
                    parameters[i] = ApplyCsharpModelOverride(parameters[i], csharpModel);
                }
            }
        }

        return parameters;
    }

    /// <summary>
    /// Creates a new <see cref="Model.AttrOrTypeParameterModel"/> identical to
    /// <paramref name="parameter"/> except that the C# metadata fields are replaced by
    /// those from <paramref name="csharpOverride"/>.
    /// </summary>
    /// <remarks>
    /// When <paramref name="csharpOverride"/> was built from a string literal entry
    /// (i.e., <c>ConstraintRecordName</c> is <c>null</c>), the string was interpreted as
    /// a C# type name and stored in <c>CppType</c> by the shared parameter-building logic;
    /// it is therefore used directly as the C# type.  For record/class entries, the
    /// model's <c>CsharpType</c> and companion fields are used as-is.
    /// </remarks>
    private static Model.AttrOrTypeParameterModel ApplyCsharpModelOverride(
        Model.AttrOrTypeParameterModel parameter,
        Model.AttrOrTypeParameterModel csharpOverride)
    {
        // A plain string entry has no ConstraintRecordName: TryBuildAttrOrTypeParameterModel
        // stored the string literal in CppType.  Treat that as the C# type name.
        var csharpType = csharpOverride.ConstraintRecordName == null
            ? csharpOverride.CppType
            : csharpOverride.CsharpType;

        return new Model.AttrOrTypeParameterModel(
            parameter.Name,
            parameter.ConstraintRecordName,
            parameter.CppType,
            parameter.CppStorageType,
            parameter.CppAccessorType,
            parameter.Summary,
            parameter.DefaultValue,
            csharpType,
            csharpOverride.CsharpSyntaxType,
            csharpOverride.CsharpParser,
            csharpOverride.CsharpExtractor,
            csharpOverride.CsharpDefault,
            csharpOverride.CsharpPrinter);
    }

    /// <summary>
    /// Attempts to build a parameter model from a single dag argument value.
    /// Returns null when the value is not a recognizable parameter specification.
    /// </summary>
    private Model.AttrOrTypeParameterModel? TryBuildAttrOrTypeParameterModel(string name, Value value)
    {
        switch (value)
        {
            case StringValue str:
                // Shorthand form: "C++Type":$name — the argument value is just the type string.
                if (string.IsNullOrEmpty(str.Value))
                {
                    return null;
                }

                return new Model.AttrOrTypeParameterModel(name, null, str.Value);

            case AnonymousRecordValue anonymous:
                return TryBuildAttrOrTypeParameterModelFromAnonymousRecord(name, anonymous);

            case RecordReferenceValue recordRef:
                // A named record reference in a parameters dag can point either to an
                // AttrOrTypeParameter subclass or to a constraint/type record that still
                // carries a cppType field (for example, Builtin_VectorTypeElementType).
                if (TryGetRecord(recordRef.RecordName, out var referencedRecord)
                    && (referencedRecord.HasBaseClass("AttrOrTypeParameter")
                        || referencedRecord.Fields.ContainsKey("cppType")))
                {
                    return TryBuildAttrOrTypeParameterModelFromFields(name, recordRef.RecordName, referencedRecord.Fields);
                }

                return null;

            default:
                return null;
        }
    }

    /// <summary>
    /// Builds a parameter model from an anonymously instantiated <c>AttrOrTypeParameter</c> subclass
    /// (e.g., <c>StringRefParameter&lt;"desc"&gt;</c>).
    /// </summary>
    private Model.AttrOrTypeParameterModel? TryBuildAttrOrTypeParameterModelFromAnonymousRecord(
        string name, AnonymousRecordValue anonymous)
    {
        return TryBuildAttrOrTypeParameterModelFromFields(name, anonymous.ClassName, anonymous.Fields);
    }

    /// <summary>
    /// Extracts parameter model fields from a field dictionary representing an <c>AttrOrTypeParameter</c>
    /// subclass instance.
    /// </summary>
    private Model.AttrOrTypeParameterModel? TryBuildAttrOrTypeParameterModelFromFields(
        string name, string className, IReadOnlyDictionary<string, Value> fields)
    {
        var isSelfTypeParameter = string.Equals(className, "AttributeSelfTypeParameter", StringComparison.Ordinal);

        // cppType is required; without it we cannot model the parameter.
        var cppType = GetStringFromValueDictionary(fields, "cppType");
        if (cppType == null)
        {
            return null;
        }

        var rawStorageType = GetStringFromValueDictionary(fields, "cppStorageType");
        var rawAccessorType = GetStringFromValueDictionary(fields, "cppAccessorType");

        // Omit storage/accessor type when identical to cppType to keep the model clean.
        var cppStorageType = rawStorageType == cppType ? null : rawStorageType;
        var cppAccessorType = rawAccessorType == cppType ? null : rawAccessorType;

        var summary = GetStringFromValueDictionary(fields, "summary");
        var defaultValue = GetStringFromValueDictionary(fields, "defaultValue");

        // csharpType is an optional MLIR.NET annotation contributed via a class-level extends
        // on the parameter class (e.g., StringRefParameter → "string" via AttrTypeBaseExtensions.td).
        // AnonymousRecordValue.Fields is extension-aware, so the lookup finds it automatically.
        var csharpType = GetStringFromValueDictionary(fields, "csharpType");

        // csharpSyntaxType is the concrete syntax type for the generated per-parameter syntax
        // property (e.g., StringRefParameter → "StringAttributeValueSyntax").
        var csharpSyntaxType = GetStringFromValueDictionary(fields, "csharpSyntaxType");

        // csharpParser / csharpPrinter / csharpExtractor / csharpDefault are optional C# code
        // snippets that override the default parameter parsing and printing behaviour in generated
        // assembly format classes.
        var csharpParser = GetStringFromValueDictionary(fields, "csharpParser");
        var csharpExtractor = GetStringFromValueDictionary(fields, "csharpExtractor");
        var csharpDefault = GetStringFromValueDictionary(fields, "csharpDefault");
        var csharpPrinter = GetStringFromValueDictionary(fields, "csharpPrinter");

        return new Model.AttrOrTypeParameterModel(
            name,
            className,
            cppType,
            cppStorageType,
            cppAccessorType,
            summary,
            defaultValue,
            csharpType,
            csharpSyntaxType,
            csharpParser,
            csharpExtractor,
            csharpDefault,
            csharpPrinter,
            isSelfTypeParameter);
    }

    /// <summary>
    /// Reads a non-empty string value from an evaluated field dictionary,
    /// or returns null when the field is absent, unset, or empty.
    /// </summary>
    private static string? GetStringFromValueDictionary(IReadOnlyDictionary<string, Value> fields, string key)
    {
        if (fields.TryGetValue(key, out var value) && value is StringValue str && !string.IsNullOrEmpty(str.Value))
        {
            return str.Value;
        }

        return null;
    }

    private static string? GetRecordNameFromValueDictionary(IReadOnlyDictionary<string, Value> fields, string key)
    {
        if (!fields.TryGetValue(key, out var field))
        {
            return null;
        }

        return field switch
        {
            RecordReferenceValue record => record.RecordName,
            SymbolReferenceValue symbol => symbol.SymbolName,
            StringValue str when !string.IsNullOrEmpty(str.Value) => str.Value,
            _ => null,
        };
    }

    /// <summary>
    /// Builds an <see cref="Model.InterfaceTraitModel"/> from a record known to extend
    /// <c>InterfaceTrait</c>. Classifies the kind and extracts interface metadata.
    /// </summary>
    private Model.InterfaceTraitModel BuildInterfaceTraitModel(string recordName, Record traitRecord)
    {
        var kind = ClassifyInterfaceKind(traitRecord);
        var cppInterfaceName = GetOptionalStringField(traitRecord, "cppInterfaceName");
        var cppNamespace = GetOptionalStringField(traitRecord, "cppNamespace");
        var baseInterfaces = GetStringListField(traitRecord, "baseInterfaces");
        var declaresMethods = traitRecord.HasBaseClass("DeclareInterfaceMethods");
        var alwaysOverriddenMethods = GetStringListField(traitRecord, "alwaysOverriddenMethods");

        return new Model.InterfaceTraitModel(
            recordName,
            kind,
            cppInterfaceName,
            cppNamespace,
            baseInterfaces,
            declaresMethods,
            alwaysOverriddenMethods);
    }

    /// <summary>
    /// Builds an <see cref="Model.InterfaceTraitModel"/> from the fields and base-class list of
    /// an anonymously instantiated record. Used for inline trait expressions such as
    /// <c>DeclareOpInterfaceMethods&lt;MyIface, ["foo"]&gt;</c> that are not registered as
    /// named top-level defs.
    /// </summary>
    private static Model.InterfaceTraitModel BuildInterfaceTraitModelFromFields(
        string recordName,
        IReadOnlyDictionary<string, Value> fields,
        IReadOnlyList<TableGen.Evaluation.EvaluatedClass> baseClasses)
    {
        var kind = ClassifyInterfaceKindFromBaseClasses(baseClasses);
        var cppInterfaceName = GetStringFromValueDictionary(fields, "cppInterfaceName");
        var cppNamespace = GetStringFromValueDictionary(fields, "cppNamespace");
        var baseInterfaces = GetStringListFromValueDictionary(fields, "baseInterfaces");
        var declaresMethods = baseClasses.Any(static bc => bc.Name == "DeclareInterfaceMethods");
        var alwaysOverriddenMethods = GetStringListFromValueDictionary(fields, "alwaysOverriddenMethods");

        return new Model.InterfaceTraitModel(
            recordName,
            kind,
            cppInterfaceName,
            cppNamespace,
            baseInterfaces,
            declaresMethods,
            alwaysOverriddenMethods);
    }

    /// <summary>
    /// Classifies the <see cref="Model.InterfaceKind"/> from a list of base class names, used
    /// when the full <see cref="Record"/> is not available (anonymous instantiation case).
    /// </summary>
    private static Model.InterfaceKind ClassifyInterfaceKindFromBaseClasses(
        IReadOnlyList<TableGen.Evaluation.EvaluatedClass> baseClasses)
    {
        var hasDialect = false;
        var hasOp = false;
        var hasType = false;
        var hasAttr = false;

        foreach (var bc in baseClasses)
        {
            switch (bc.Name)
            {
                case "DialectInterface": hasDialect = true; break;
                case "OpInterfaceTrait": hasOp = true; break;
                case "TypeInterface": hasType = true; break;
                case "AttrInterface": hasAttr = true; break;
            }
        }

        if (hasDialect) return Model.InterfaceKind.Dialect;
        if (hasOp) return Model.InterfaceKind.Op;
        if (hasType) return Model.InterfaceKind.Type;
        if (hasAttr) return Model.InterfaceKind.Attr;
        return Model.InterfaceKind.Op;
    }

    /// <summary>
    /// Extracts a list of strings from a list-valued field in the given field dictionary.
    /// Returns an empty list when the field is absent or empty.
    /// </summary>
    private static IReadOnlyList<string> GetStringListFromValueDictionary(
        IReadOnlyDictionary<string, Value> fields, string key)
    {
        if (!fields.TryGetValue(key, out var field) || field is not ListValue list)
        {
            return EmptyStrings;
        }

        var values = new List<string>(list.Items.Count);
        foreach (var item in list.Items)
        {
            string? s = item switch
            {
                StringValue str when !string.IsNullOrEmpty(str.Value) => str.Value,
                SymbolReferenceValue sym => sym.SymbolName,
                RecordReferenceValue rec => rec.RecordName,
                _ => null,
            };
            if (s != null)
            {
                values.Add(s);
            }
        }

        return values.Count == 0 ? EmptyStrings : values;
    }

    /// <summary>
    /// Classifies the <see cref="Model.InterfaceKind"/> for a record that extends
    /// <c>InterfaceTrait</c>.
    /// </summary>
    private static Model.InterfaceKind ClassifyInterfaceKind(Record record)
    {
        // DialectInterface extends OpInterfaceTrait, so check it before OpInterfaceTrait to
        // distinguish dialect interfaces from ordinary op interfaces.
        if (record.HasBaseClass("DialectInterface"))
        {
            return Model.InterfaceKind.Dialect;
        }

        if (record.HasBaseClass("OpInterfaceTrait"))
        {
            return Model.InterfaceKind.Op;
        }

        if (record.HasBaseClass("TypeInterface"))
        {
            return Model.InterfaceKind.Type;
        }

        if (record.HasBaseClass("AttrInterface"))
        {
            return Model.InterfaceKind.Attr;
        }

        // Fallback for plain InterfaceTrait subclasses that do not fit a specific category.
        return Model.InterfaceKind.Op;
    }

    /// <summary>
    /// Attempts to build an <see cref="Model.InterfaceModel"/> from a record that represents
    /// an interface definition (a record with base class <c>Interface</c>).
    /// Returns <see langword="null"/> when the record is not a recognizable interface definition.
    /// </summary>
    public Model.InterfaceModel? TryBuildInterfaceModel(Record record)
    {
        if (!record.HasBaseClass("Interface"))
        {
            return null;
        }

        var cppInterfaceName = GetOptionalStringField(record, "cppInterfaceName");
        if (string.IsNullOrEmpty(cppInterfaceName))
        {
            // Without a cppInterfaceName the interface cannot be identified; skip it.
            return null;
        }

        var kind = ClassifyInterfaceKind(record);
        var cppNamespace = GetOptionalStringField(record, "cppNamespace");
        var description = GetOptionalStringField(record, "description");
        var baseInterfaces = GetStringListField(record, "baseInterfaces");
        var methods = BuildInterfaceMethods(record);
        var csharpName = GetOptionalStringField(record, "csharpName");
        var csharpSummary = GetOptionalStringField(record, "csharpSummary");
        var csharpDescription = GetOptionalStringField(record, "csharpDescription");
        var csharpMembers = BuildInterfaceCSharpMembers(record);

        return new Model.InterfaceModel(
            record.Name,
            kind,
            cppInterfaceName!,
            cppNamespace,
            description,
            baseInterfaces,
            methods,
            csharpName,
            csharpSummary,
            csharpDescription,
            csharpMembers);
    }

    /// <summary>
    /// Extracts the explicit C# interface members declared for an interface record through
    /// MLIR.NET overlay metadata.
    /// </summary>
    private IReadOnlyList<Model.InterfaceCSharpMemberModel> BuildInterfaceCSharpMembers(Record record)
    {
        if (!record.Fields.TryGetValue("csharpMembers", out var field) || field is not ListValue list)
        {
            return EmptyInterfaceCSharpMembers;
        }

        var members = new List<Model.InterfaceCSharpMemberModel>(list.Items.Count);
        foreach (var item in list.Items)
        {
            var member = TryBuildInterfaceCSharpMemberModel(item);
            if (member != null)
            {
                members.Add(member);
            }
        }

        return members.Count == 0 ? EmptyInterfaceCSharpMembers : members;
    }

    /// <summary>
    /// Extracts the explicit interface-member implementations attached to a type record through
    /// MLIR.NET overlay metadata.
    /// </summary>
    public IReadOnlyList<Model.InterfaceMemberImplementationModel> GetInterfaceMemberImplementations(Record record)
    {
        if (!record.Fields.TryGetValue("csharpInterfaceImplementations", out var field) || field is not ListValue list)
        {
            return EmptyInterfaceMemberImplementations;
        }

        var implementations = new List<Model.InterfaceMemberImplementationModel>(list.Items.Count);
        foreach (var item in list.Items)
        {
            var implementation = TryBuildInterfaceMemberImplementationModel(item);
            if (implementation != null)
            {
                implementations.Add(implementation);
            }
        }

        return implementations.Count == 0 ? EmptyInterfaceMemberImplementations : implementations;
    }

    /// <summary>
    /// Extracts the list of interface method models from the <c>methods</c> field of an
    /// interface record.
    /// </summary>
    private IReadOnlyList<Model.InterfaceMethodModel> BuildInterfaceMethods(Record record)
    {
        if (!record.Fields.TryGetValue("methods", out var field) || field is not ListValue list)
        {
            return EmptyInterfaceMethods;
        }

        var methods = new List<Model.InterfaceMethodModel>(list.Items.Count);
        foreach (var item in list.Items)
        {
            var method = TryBuildInterfaceMethodModel(item);
            if (method != null)
            {
                methods.Add(method);
            }
        }

        return methods;
    }

    /// <summary>
    /// Attempts to build an <see cref="Model.InterfaceMethodModel"/> from a single list-item
    /// value representing an <c>InterfaceMethod</c> (or subclass) record.
    /// </summary>
    private Model.InterfaceMethodModel? TryBuildInterfaceMethodModel(Value item)
    {
        IReadOnlyDictionary<string, Value>? fields = null;
        string className = "InterfaceMethod";

        switch (item)
        {
            case AnonymousRecordValue anon:
                fields = anon.Fields;
                className = anon.ClassName;
                break;
            case RecordReferenceValue recordRef:
                if (TryGetRecord(recordRef.RecordName, out var referencedRecord))
                {
                    fields = referencedRecord.Fields;
                    className = recordRef.RecordName;
                    // Determine method kind from base classes of the referenced record.
                    return TryBuildInterfaceMethodModelFromRecord(referencedRecord);
                }
                return null;
            default:
                return null;
        }

        if (fields == null)
        {
            return null;
        }

        return TryBuildInterfaceMethodModelFromFields(className, fields);
    }

    /// <summary>
    /// Builds an interface method model from a concrete <c>InterfaceMethod</c> record,
    /// using base class information to determine the method kind.
    /// </summary>
    private Model.InterfaceMethodModel? TryBuildInterfaceMethodModelFromRecord(Record record)
    {
        var kind = ClassifyInterfaceMethodKind(record);
        return TryBuildInterfaceMethodModelFromFields(record.Name, record.Fields, kind);
    }

    /// <summary>
    /// Builds an interface method model from a field dictionary, optionally with a known
    /// method kind (when the record is available).
    /// </summary>
    private static Model.InterfaceMethodModel? TryBuildInterfaceMethodModelFromFields(
        string className,
        IReadOnlyDictionary<string, Value> fields,
        Model.InterfaceMethodKind? kindOverride = null)
    {
        var name = GetStringFromValueDictionary(fields, "name");
        var returnType = GetStringFromValueDictionary(fields, "returnType");

        if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(returnType))
        {
            return null;
        }

        var kind = kindOverride ?? ClassifyInterfaceMethodKindFromClassName(className);
        var description = GetStringFromValueDictionary(fields, "description");
        var arguments = ExtractMethodArguments(fields);
        var body = GetStringFromValueDictionary(fields, "body");
        var defaultBody = GetStringFromValueDictionary(fields, "defaultBody");

        return new Model.InterfaceMethodModel(name!, returnType!, kind, description, arguments, body, defaultBody);
    }

    /// <summary>
    /// Classifies an interface method kind from the base classes of a concrete record.
    /// </summary>
    private static Model.InterfaceMethodKind ClassifyInterfaceMethodKind(Record record)
    {
        if (record.HasBaseClass("StaticInterfaceMethod"))
        {
            return Model.InterfaceMethodKind.Static;
        }

        if (record.HasBaseClass("PureVirtualInterfaceMethod"))
        {
            return Model.InterfaceMethodKind.PureVirtual;
        }

        if (record.HasBaseClass("InterfaceMethodDeclaration"))
        {
            return Model.InterfaceMethodKind.Declaration;
        }

        return Model.InterfaceMethodKind.Regular;
    }

    /// <summary>
    /// Classifies an interface method kind based on the TableGen class name of an
    /// anonymous record (when no concrete record is available for base-class inspection).
    /// </summary>
    private static Model.InterfaceMethodKind ClassifyInterfaceMethodKindFromClassName(string className)
    {
        if (className.StartsWith("Static", StringComparison.Ordinal))
        {
            return Model.InterfaceMethodKind.Static;
        }

        if (className.StartsWith("PureVirtual", StringComparison.Ordinal))
        {
            return Model.InterfaceMethodKind.PureVirtual;
        }

        if (className.EndsWith("Declaration", StringComparison.Ordinal))
        {
            return Model.InterfaceMethodKind.Declaration;
        }

        return Model.InterfaceMethodKind.Regular;
    }

    private Model.InterfaceCSharpMemberModel? TryBuildInterfaceCSharpMemberModel(Value item)
    {
        IReadOnlyDictionary<string, Value>? fields = null;
        string className = "MLIRNet_InterfaceProperty";

        switch (item)
        {
            case AnonymousRecordValue anonymous:
                fields = anonymous.Fields;
                className = anonymous.ClassName;
                break;
            case RecordReferenceValue recordRef:
                if (TryGetRecord(recordRef.RecordName, out var referencedRecord))
                {
                    fields = referencedRecord.Fields;
                    className = recordRef.RecordName;
                }
                break;
        }

        if (fields == null)
        {
            return null;
        }

        var upstreamName = GetStringFromValueDictionary(fields, "upstreamName");
        var csharpType = GetStringFromValueDictionary(fields, "csharpType");
        var csharpName = GetStringFromValueDictionary(fields, "csharpName");
        var csharpSummary = GetStringFromValueDictionary(fields, "csharpSummary");
        var csharpDescription = GetStringFromValueDictionary(fields, "csharpDescription");
        if (string.IsNullOrEmpty(upstreamName) || string.IsNullOrEmpty(csharpType) || string.IsNullOrEmpty(csharpName))
        {
            return null;
        }

        var kind = className switch
        {
            "MLIRNet_InterfaceProperty" => Model.InterfaceCSharpMemberKind.Property,
            _ => Model.InterfaceCSharpMemberKind.Property,
        };

        return new Model.InterfaceCSharpMemberModel(
            kind,
            upstreamName!,
            csharpType!,
            csharpName!,
            csharpSummary,
            csharpDescription);
    }

    private Model.InterfaceMemberImplementationModel? TryBuildInterfaceMemberImplementationModel(Value item)
    {
        IReadOnlyDictionary<string, Value>? fields = null;

        switch (item)
        {
            case AnonymousRecordValue anonymous:
                fields = anonymous.Fields;
                break;
            case RecordReferenceValue recordRef:
                if (TryGetRecord(recordRef.RecordName, out var referencedRecord))
                {
                    fields = referencedRecord.Fields;
                }
                break;
        }

        if (fields == null)
        {
            return null;
        }

        var interfaceRecordName = GetRecordNameFromValueDictionary(fields, "interfaceDef");
        var csharpMemberName = GetStringFromValueDictionary(fields, "csharpMemberName");
        var csharpExpression = GetStringFromValueDictionary(fields, "csharpExpression");
        if (string.IsNullOrEmpty(interfaceRecordName)
            || string.IsNullOrEmpty(csharpMemberName)
            || string.IsNullOrEmpty(csharpExpression))
        {
            return null;
        }

        return new Model.InterfaceMemberImplementationModel(interfaceRecordName!, csharpMemberName!, csharpExpression!);
    }

    /// <summary>
    /// Extracts C++ argument type/name pairs from the <c>arguments</c> dag field of an
    /// interface method record.
    /// </summary>
    private static IReadOnlyList<(string ArgType, string ArgName)> ExtractMethodArguments(
        IReadOnlyDictionary<string, Value> fields)
    {
        if (!fields.TryGetValue("arguments", out var field) || field is not DagValue dag)
        {
            return EmptyMethodArguments;
        }

        var args = new List<(string, string)>(dag.Arguments.Count);
        foreach (var argument in dag.Arguments)
        {
            if (argument.Name == null)
            {
                continue;
            }

            // The value is expected to be a string containing the C++ type.
            var argType = argument.Value switch
            {
                StringValue str when !string.IsNullOrEmpty(str.Value) => str.Value,
                _ => null,
            };

            if (argType != null)
            {
                args.Add((argType, argument.Name));
            }
        }

        return args.Count == 0 ? EmptyMethodArguments : args;
    }

    private static readonly IReadOnlyList<string> EmptyStrings = new string[0];
    private static readonly IReadOnlyList<Model.TraitModel> EmptyTraitModels = new Model.TraitModel[0];
    private static readonly IReadOnlyList<DagMemberModel> EmptyDagMembers = new DagMemberModel[0];
    private static readonly IReadOnlyList<Model.EnumCaseModel> EmptyEnumCases = new Model.EnumCaseModel[0];
    private static readonly IReadOnlyList<Model.AttrOrTypeParameterModel> EmptyAttrOrTypeParameters = new Model.AttrOrTypeParameterModel[0];
    private static readonly IReadOnlyList<Model.InterfaceMethodModel> EmptyInterfaceMethods = new Model.InterfaceMethodModel[0];
    private static readonly IReadOnlyList<Model.InterfaceCSharpMemberModel> EmptyInterfaceCSharpMembers = new Model.InterfaceCSharpMemberModel[0];
    private static readonly IReadOnlyList<Model.InterfaceMemberImplementationModel> EmptyInterfaceMemberImplementations =
        new Model.InterfaceMemberImplementationModel[0];
    private static readonly IReadOnlyList<(string, string)> EmptyMethodArguments = new (string, string)[0];
}
