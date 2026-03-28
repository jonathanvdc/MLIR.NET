namespace MLIR.ODS;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using MLIR.ODS.Model;
using TableGen.Evaluation;

/// <summary>
/// Translates interpreted TableGen records into a coarse ODS model.
/// </summary>
public static class OdsDialectImporter
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
    /// <item><description>a type derived from <c>TypeDef</c> with fields such as <c>dialectName</c>, <c>typeName</c>, and optional <c>cppClassName</c></description></item>
    /// </list>
    /// Unsupported records are ignored for now.
    /// </remarks>
    public static IReadOnlyList<OdsDialectModel> Import(InterpretedDocument document)
    {
        var dialectsByName = new Dictionary<string, MutableDialectModel>(StringComparer.Ordinal);
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
                && TryGetStringField(record, "dialectName", out var opDialectName)
                && TryGetStringField(record, "mnemonic", out var mnemonic))
            {
                var dialect = GetOrCreateDialect(dialectsByName, opDialectName);
                dialect.Operations.Add(
                    new OdsOperationModel(
                        opDialectName + "." + mnemonic,
                        GetOptionalStringField(record, "cppClassName"),
                        GetStringListField(record, "operands"),
                        GetStringListField(record, "results"),
                        GetStringListField(record, "attributes"),
                        GetOptionalBitField(record, "hasCustomAssemblyFormat")));
                continue;
            }

            if (record.HasBaseClass("AttrDef")
                && TryGetStringField(record, "dialectName", out var attrDialectName)
                && TryGetStringField(record, "attrName", out var attributeName))
            {
                var dialect = GetOrCreateDialect(dialectsByName, attrDialectName);
                dialect.Attributes.Add(new OdsAttributeModel(attributeName, GetOptionalStringField(record, "cppClassName")));
                continue;
            }

            if (record.HasBaseClass("TypeDef")
                && TryGetStringField(record, "dialectName", out var typeDialectName)
                && TryGetStringField(record, "typeName", out var typeName))
            {
                var dialect = GetOrCreateDialect(dialectsByName, typeDialectName);
                dialect.Types.Add(new OdsTypeModel(typeName, GetOptionalStringField(record, "cppClassName")));
            }
        }

        return dialectsByName.Values
            .Select(static dialect => dialect.ToImmutable())
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

    private static bool TryGetStringField(TableGenRecord record, string fieldName, out string value)
    {
        if (record.Fields.TryGetValue(fieldName, out var field) && field is StringValue stringValue)
        {
            value = stringValue.Value;
            return true;
        }

        value = string.Empty;
        return false;
    }

    private static string? GetOptionalStringField(TableGenRecord record, string fieldName)
    {
        return record.Fields.TryGetValue(fieldName, out var field) && field is StringValue stringValue
            ? stringValue.Value
            : null;
    }

    private static IReadOnlyList<string> GetStringListField(TableGenRecord record, string fieldName)
    {
        if (!record.Fields.TryGetValue(fieldName, out var field) || field is not ListValue list)
        {
            return EmptyStrings;
        }

        var values = new List<string>(list.Items.Count);
        foreach (var item in list.Items)
        {
            if (item is StringValue stringValue)
            {
                values.Add(stringValue.Value);
            }
        }

        return values;
    }

    private static bool GetOptionalBitField(TableGenRecord record, string fieldName)
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
        public List<OdsOperationModel> Operations { get; } = new List<OdsOperationModel>();
        public List<OdsAttributeModel> Attributes { get; } = new List<OdsAttributeModel>();
        public List<OdsTypeModel> Types { get; } = new List<OdsTypeModel>();

        public OdsDialectModel ToImmutable()
        {
            return new OdsDialectModel(Name, CppNamespace, Summary, Description, HasConstantMaterializer, Operations, Attributes, Types);
        }
    }

    private static readonly IReadOnlyList<string> EmptyStrings = new string[0];
}
