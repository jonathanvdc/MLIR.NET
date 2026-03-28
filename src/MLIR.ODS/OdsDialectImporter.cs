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
    /// The currently supported subset is convention-based rather than full MLIR ODS. Records are
    /// grouped by a required <c>DialectName</c> string field, and may describe one of:
    /// <list type="bullet">
    /// <item><description>an operation with <c>OperationName</c>, optional <c>ClassName</c>, optional <c>Operands</c>/<c>Results</c>/<c>Attributes</c> string lists, and optional <c>HasCustomAssemblyFormat</c> bit</description></item>
    /// <item><description>an attribute with <c>AttributeName</c> and optional <c>ClassName</c></description></item>
    /// <item><description>a type with <c>TypeName</c> and optional <c>ClassName</c></description></item>
    /// </list>
    /// Unsupported records are ignored for now.
    /// </remarks>
    public static IReadOnlyList<OdsDialectModel> Import(InterpretedDocument document)
    {
        var dialectsByName = new Dictionary<string, MutableDialectModel>(StringComparer.Ordinal);
        foreach (var record in document.Records)
        {
            if (!TryGetStringField(record, "DialectName", out var dialectName))
            {
                continue;
            }

            if (!dialectsByName.TryGetValue(dialectName, out var dialect))
            {
                dialect = new MutableDialectModel(dialectName);
                dialectsByName.Add(dialectName, dialect);
            }

            if (TryGetStringField(record, "OperationName", out var operationName))
            {
                dialect.Operations.Add(
                    new OdsOperationModel(
                        operationName,
                        GetOptionalStringField(record, "ClassName"),
                        GetStringListField(record, "Operands"),
                        GetStringListField(record, "Results"),
                        GetStringListField(record, "Attributes"),
                        GetOptionalBitField(record, "HasCustomAssemblyFormat")));
                continue;
            }

            if (TryGetStringField(record, "AttributeName", out var attributeName))
            {
                dialect.Attributes.Add(new OdsAttributeModel(attributeName, GetOptionalStringField(record, "ClassName")));
                continue;
            }

            if (TryGetStringField(record, "TypeName", out var typeName))
            {
                dialect.Types.Add(new OdsTypeModel(typeName, GetOptionalStringField(record, "ClassName")));
            }
        }

        return dialectsByName.Values
            .Select(static dialect => dialect.ToImmutable())
            .OrderBy(static dialect => dialect.Name, StringComparer.Ordinal)
            .ToArray();
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
        public List<OdsOperationModel> Operations { get; } = new List<OdsOperationModel>();
        public List<OdsAttributeModel> Attributes { get; } = new List<OdsAttributeModel>();
        public List<OdsTypeModel> Types { get; } = new List<OdsTypeModel>();

        public OdsDialectModel ToImmutable()
        {
            return new OdsDialectModel(Name, Operations, Attributes, Types);
        }
    }

    private static readonly IReadOnlyList<string> EmptyStrings = new string[0];
}
