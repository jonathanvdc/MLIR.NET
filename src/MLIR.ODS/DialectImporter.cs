namespace MLIR.ODS;

using System;
using System.Collections.Generic;
using System.Linq;
using MLIR.ODS.Model;
using TableGen.Evaluation;

/// <summary>
/// Translates interpreted TableGen records into a coarse ODS model.
/// </summary>
public static class DialectImporter
{
    /// <summary>
    /// Imports dialect models from an interpreted TableGen document.
    /// </summary>
    /// <remarks>
    /// The currently supported subset recognizes ODS-style records by inherited bases and common
    /// field names. Records may describe one of:
    /// <list type="bullet">
    /// <item><description>a dialect definition derived from <c>Dialect</c> with fields such as <c>name</c>, <c>cppNamespace</c>, <c>summary</c>, <c>description</c>, and <c>hasConstantMaterializer</c></description></item>
    /// <item><description>an operation derived from <c>Op</c> with fields such as <c>dialectName</c>, <c>mnemonic</c>, optional <c>cppClassName</c>, optional <c>operands</c>/<c>results</c>/<c>attributes</c>, and optional <c>hasCustomAssemblyFormat</c></description></item>
    /// <item><description>an attribute derived from <c>AttrDef</c> with fields such as <c>dialectName</c>, <c>attrName</c>, and optional <c>cppClassName</c></description></item>
    /// <item><description>an attribute constraint derived from <c>Attr</c> with context-directed parsing behavior such as <c>I32Attr</c></description></item>
    /// <item><description>a type derived from <c>TypeDef</c> with fields such as <c>dialectName</c>, <c>typeName</c>, and optional <c>cppClassName</c></description></item>
    /// </list>
    /// Unsupported records are ignored for now.
    /// </remarks>
    public static IReadOnlyList<DialectModel> Import(InterpretedDocument document)
    {
        var dialectsByName = new Dictionary<string, MutableDialectModel>(StringComparer.Ordinal);
        var sharedAttributeConstraints = new List<AttributeConstraintModel>();
        foreach (var record in document.Records)
        {
            if (record.HasBaseClass("Dialect") && TryGetStringField(record, "name", out var definedDialectName))
            {
                if (!dialectsByName.TryGetValue(definedDialectName, out var dialect))
                {
                    dialect = new MutableDialectModel(definedDialectName);
                    dialectsByName.Add(definedDialectName, dialect);
                }

                dialect.CppNamespace = GetOptionalStringField(record, "cppNamespace");
                dialect.Summary = GetOptionalStringField(record, "summary");
                dialect.Description = GetOptionalStringField(record, "description");
                dialect.HasConstantMaterializer = GetOptionalBitField(record, "hasConstantMaterializer");
                continue;
            }

            if (record.HasBaseClass("Op")
                && TryGetDialectName(record, document.Records, out var opDialectName)
                && TryGetOperationName(record, out var mnemonic))
            {
                var dialect = GetOrCreateDialect(dialectsByName, opDialectName);
                var argumentMembers = GetDagMembers(record, "arguments");
                var resultMembers = GetDagMembers(record, "results");
                var attributeConstraints = argumentMembers
                    .Where(static member => member.Kind == DagMemberKind.Attribute && member.ConstraintName != null)
                    .ToDictionary(static member => member.Name, static member => member.ConstraintName!, StringComparer.Ordinal);
                var assemblyFormatString = GetOptionalStringField(record, "assemblyFormat");
                var assemblyFormat = !string.IsNullOrEmpty(assemblyFormatString)
                    ? AssemblyFormatParser.Parse(assemblyFormatString!)
                    : null;
                dialect.Operations.Add(
                    new OperationModel(
                        opDialectName + "." + mnemonic,
                        GetOptionalStringField(record, "cppClassName") ?? record.Name,
                        argumentMembers.Where(static member => member.Kind == DagMemberKind.Operand).Select(static member => member.Name).ToArray(),
                        resultMembers.Where(static member => member.Kind == DagMemberKind.Result).Select(static member => member.Name).ToArray(),
                        argumentMembers.Where(static member => member.Kind == DagMemberKind.Attribute).Select(static member => member.Name).ToArray(),
                        attributeConstraints,
                        assemblyFormat != null,
                        GetOptionalStringField(record, "summary"),
                        GetOptionalStringField(record, "description"),
                        assemblyFormat,
                        GetStringListField(record, "traits")));
                continue;
            }

            if (record.HasBaseClass("AttrDef")
                && TryGetDialectName(record, document.Records, out var attrDialectName)
                && TryGetStringField(record, "attrName", out var attributeName))
            {
                var dialect = GetOrCreateDialect(dialectsByName, attrDialectName);
                dialect.Attributes.Add(new AttributeModel(attributeName, record.Name, GetOptionalStringField(record, "cppClassName") ?? record.Name));
                continue;
            }

            if (record.HasBaseClass("Attr")
                && !record.HasBaseClass("AttrDef")
                && TryGetAttributeConstraintKind(record, out var constraintKind))
            {
                sharedAttributeConstraints.Add(new AttributeConstraintModel(record.Name, record.Name, constraintKind));
                continue;
            }

            if (record.HasBaseClass("TypeDef")
                && TryGetDialectName(record, document.Records, out var typeDialectName)
                && TryGetStringField(record, "typeName", out var typeName))
            {
                var dialect = GetOrCreateDialect(dialectsByName, typeDialectName);
                dialect.Types.Add(new TypeModel(typeName, record.Name, GetOptionalStringField(record, "cppClassName") ?? record.Name));
            }
        }

        return dialectsByName.Values
            .Select(dialect => dialect.ToImmutable(sharedAttributeConstraints))
            .OrderBy(static dialect => dialect.Name, StringComparer.Ordinal)
            .ToArray();
    }

    private static MutableDialectModel GetOrCreateDialect(
        IDictionary<string, MutableDialectModel> dialectsByName,
        string dialectName)
    {
        if (!dialectsByName.TryGetValue(dialectName, out var dialect))
        {
            dialect = new MutableDialectModel(dialectName);
            dialectsByName.Add(dialectName, dialect);
        }

        return dialect;
    }

    private static bool TryGetStringField(Record record, string fieldName, out string value)
    {
        if (record.Fields.TryGetValue(fieldName, out var field) && field is StringValue stringValue)
        {
            value = stringValue.Value;
            return true;
        }

        value = string.Empty;
        return false;
    }

    private static string? GetOptionalStringField(Record record, string fieldName)
    {
        if (!record.Fields.TryGetValue(fieldName, out var field))
        {
            return null;
        }

        // An empty string is treated as absent, matching the ODS convention that
        // empty string = "not set". This ensures prelude defaults like
        // `string cppClassName = ""` behave identically to a missing field.
        return field switch
        {
            StringValue { Value.Length: 0 } => null,
            StringValue stringValue => stringValue.Value,
            SymbolReferenceValue symbol => symbol.SymbolName,
            RecordReferenceValue recordReference => recordReference.RecordName,
            _ => null,
        };
    }

    private static IReadOnlyList<string> GetStringListField(Record record, string fieldName)
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

    private static bool TryGetDialectName(Record record, IReadOnlyList<Record> allRecords, out string dialectName)
    {
        if (!record.Fields.TryGetValue("dialect", out var dialectField)
            && !record.Fields.TryGetValue("opDialect", out dialectField))
        {
            dialectName = string.Empty;
            return false;
        }

        if (dialectField is RecordReferenceValue recordReference)
        {
            var dialectRecord = allRecords.FirstOrDefault(candidate => candidate.Name == recordReference.RecordName);
            if (dialectRecord != null && TryGetStringField(dialectRecord, "name", out dialectName))
            {
                return true;
            }
        }

        if (dialectField is StringValue stringValue)
        {
            dialectName = stringValue.Value;
            return true;
        }

        dialectName = string.Empty;
        return false;
    }

    private static bool TryGetOperationName(Record record, out string operationName)
    {
        return TryGetStringField(record, "mnemonic", out operationName)
            || TryGetStringField(record, "opName", out operationName);
    }

    private static IReadOnlyList<DagMember> GetDagMembers(Record record, string fieldName)
    {
        if (!record.Fields.TryGetValue(fieldName, out var field) || field is not DagValue dag)
        {
            return EmptyDagMembers;
        }

        var kind = dag.OperatorName switch
        {
            "ins" => DagMemberKind.Operand,
            "outs" => DagMemberKind.Result,
            _ => DagMemberKind.Operand,
        };

        var members = new List<DagMember>(dag.Arguments.Count);
        foreach (var argument in dag.Arguments)
        {
            if (argument.Name == null)
            {
                continue;
            }

            var constraintName = GetConstraintName(argument.Value);
            var memberKind = kind;
            if (kind == DagMemberKind.Operand && constraintName != null && constraintName.EndsWith("Attr", StringComparison.Ordinal))
            {
                memberKind = DagMemberKind.Attribute;
            }

            members.Add(new DagMember(argument.Name, constraintName, memberKind));
        }

        return members;
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

    private static bool GetOptionalBitField(Record record, string fieldName)
    {
        if (!record.Fields.TryGetValue(fieldName, out var field))
        {
            return false;
        }

        return field switch
        {
            BitValue bit => bit.Value,
            IntegerValue integer => integer.Value != 0,
            _ => false,
        };
    }

    private sealed class MutableDialectModel
    {
        public MutableDialectModel(string name)
        {
            Name = name;
        }

        public string Name { get; }
        public string? CppNamespace { get; set; }
        public string? Summary { get; set; }
        public string? Description { get; set; }
        public bool HasConstantMaterializer { get; set; }
        public List<OperationModel> Operations { get; } = new List<OperationModel>();
        public List<AttributeModel> Attributes { get; } = new List<AttributeModel>();
        public List<TypeModel> Types { get; } = new List<TypeModel>();

        public DialectModel ToImmutable(IReadOnlyList<AttributeConstraintModel> sharedAttributeConstraints)
        {
            return new DialectModel(Name, CppNamespace, Summary, Description, HasConstantMaterializer, Operations, Attributes, sharedAttributeConstraints, Types);
        }
    }

    private static readonly IReadOnlyList<string> EmptyStrings = new string[0];
    private static readonly IReadOnlyList<DagMember> EmptyDagMembers = new DagMember[0];

    private readonly struct DagMember
    {
        public DagMember(string name, string? constraintName, DagMemberKind kind)
        {
            Name = name;
            ConstraintName = constraintName;
            Kind = kind;
        }

        public string Name { get; }

        public string? ConstraintName { get; }

        public DagMemberKind Kind { get; }
    }

    private enum DagMemberKind
    {
        Operand,
        Result,
        Attribute,
    }

    private static bool TryGetAttributeConstraintKind(Record record, out AttributeConstraintKind kind)
    {
        if (record.Name == "BoolAttr")
        {
            kind = AttributeConstraintKind.BooleanLiteral;
            return true;
        }

        if (record.HasBaseClass("DenseArrayAttrBase"))
        {
            kind = AttributeConstraintKind.DenseArrayAttribute;
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
}
