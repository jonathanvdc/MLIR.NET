namespace MLIR.Generators.Emitters;

using System;
using System.Collections.Generic;
using MLIR.ODS.Model;

internal sealed class OperationBodySyntaxConstructionPlan
{
    public OperationBodySyntaxConstructionPlan(
        IReadOnlyList<string> regionFields,
        IReadOnlyDictionary<string, string> attributeFields,
        string? attrDictField,
        string? attrDictWithKeywordField,
        string? propDictField,
        IReadOnlyDictionary<string, string> operandFields,
        IReadOnlyDictionary<string, string> resultFields,
        string? typeField,
        string? successorsField,
        string? operandsField)
    {
        RegionFields = regionFields;
        AttributeFields = attributeFields;
        AttrDictField = attrDictField;
        AttrDictWithKeywordField = attrDictWithKeywordField;
        PropDictField = propDictField;
        OperandFields = operandFields;
        ResultFields = resultFields;
        TypeField = typeField;
        SuccessorsField = successorsField;
        OperandsField = operandsField;
    }

    public IReadOnlyList<string> RegionFields { get; }
    public IReadOnlyDictionary<string, string> AttributeFields { get; }
    public string? AttrDictField { get; }
    public string? AttrDictWithKeywordField { get; }
    public string? PropDictField { get; }
    public IReadOnlyDictionary<string, string> OperandFields { get; }
    public IReadOnlyDictionary<string, string> ResultFields { get; }
    public string? TypeField { get; }
    public string? SuccessorsField { get; }
    public string? OperandsField { get; }
}

internal static class OperationBodySyntaxDescriptor
{
    public static OperationBodySyntaxConstructionPlan Describe(OperationBodySyntaxMetadata metadata)
    {
        var regionFields = new List<string>();
        var attributeFields = new Dictionary<string, string>(StringComparer.Ordinal);
        var operandFields = new Dictionary<string, string>(StringComparer.Ordinal);
        var resultFields = new Dictionary<string, string>(StringComparer.Ordinal);
        string? attrDictField = null;
        string? attrDictWithKeywordField = null;
        string? propDictField = null;
        string? successorsField = null;
        string? operandsField = null;
        string? typeField = null;

        foreach (var component in metadata.ComponentFields)
        {
            switch (component.Kind)
            {
                case BodyComponentKind.Regions:
                    regionFields.Add(component.FieldName);
                    break;
                case BodyComponentKind.Attribute:
                    attributeFields[component.ComponentName] = component.FieldName;
                    break;
                case BodyComponentKind.Operand:
                    operandFields[component.ComponentName] = component.FieldName;
                    break;
                case BodyComponentKind.Result:
                    resultFields[component.ComponentName] = component.FieldName;
                    break;
                case BodyComponentKind.AttrDict:
                    attrDictField ??= component.FieldName;
                    break;
                case BodyComponentKind.AttrDictWithKeyword:
                    attrDictWithKeywordField ??= component.FieldName;
                    break;
                case BodyComponentKind.PropDict:
                    propDictField ??= component.FieldName;
                    break;
                case BodyComponentKind.Successors:
                    successorsField ??= component.FieldName;
                    break;
                case BodyComponentKind.Operands:
                    operandsField ??= component.FieldName;
                    break;
                case BodyComponentKind.Type:
                    typeField ??= component.FieldName;
                    break;
            }
        }

        return new OperationBodySyntaxConstructionPlan(
            regionFields,
            attributeFields,
            attrDictField,
            attrDictWithKeywordField,
            propDictField,
            operandFields,
            resultFields,
            typeField,
            successorsField,
            operandsField);
    }
}
