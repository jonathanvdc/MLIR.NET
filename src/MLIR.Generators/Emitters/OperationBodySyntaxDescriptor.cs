namespace MLIR.Generators.Emitters;

using System;
using System.Collections.Generic;
using MLIR.ODS.Model;

/// <summary>
/// Describes how to reconstruct an <c>OperationBodySyntax</c> from the fields
/// collected in generated syntax node types.
/// </summary>
/// <remarks>
/// The plan is derived from <see cref="OperationBodySyntaxMetadata.ComponentFields"/>
/// and records which generated field stores each logical body component.
/// Consumers can then synthesize a generic operation body without reinterpreting
/// the metadata each time.
/// </remarks>
internal sealed class OperationBodySyntaxConstructionPlan
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OperationBodySyntaxConstructionPlan"/> class.
    /// </summary>
    /// <param name="regionFields">
    /// The generated field names that store region components, in metadata order.
    /// </param>
    /// <param name="attributeFields">
    /// A mapping from logical attribute names to the generated field names that store them.
    /// </param>
    /// <param name="attrDictField">
    /// The generated field name for the attribute dictionary component, if one is present.
    /// </param>
    /// <param name="attrDictWithKeywordField">
    /// The generated field name for the keyword-prefixed attribute dictionary component, if one is present.
    /// </param>
    /// <param name="propDictField">
    /// The generated field name for the property dictionary component, if one is present.
    /// </param>
    /// <param name="operandFields">
    /// A mapping from logical operand names to the generated field names that store them.
    /// </param>
    /// <param name="resultFields">
    /// A mapping from logical result names to the generated field names that store them.
    /// </param>
    /// <param name="typeField">
    /// The generated field name for the trailing type component, if one is present.
    /// </param>
    /// <param name="successorsField">
    /// The generated field name for the successor list component, if one is present.
    /// </param>
    /// <param name="operandsField">
    /// The generated field name for the aggregate operand list component, if one is present.
    /// </param>
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

    /// <summary>
    /// Gets the generated field names that store region components.
    /// </summary>
    public IReadOnlyList<string> RegionFields { get; }

    /// <summary>
    /// Gets a mapping from logical attribute names to generated field names.
    /// </summary>
    public IReadOnlyDictionary<string, string> AttributeFields { get; }

    /// <summary>
    /// Gets the generated field name for the attribute dictionary component, if present.
    /// </summary>
    public string? AttrDictField { get; }

    /// <summary>
    /// Gets the generated field name for the keyword-prefixed attribute dictionary component, if present.
    /// </summary>
    public string? AttrDictWithKeywordField { get; }

    /// <summary>
    /// Gets the generated field name for the property dictionary component, if present.
    /// </summary>
    public string? PropDictField { get; }

    /// <summary>
    /// Gets a mapping from logical operand names to generated field names.
    /// </summary>
    public IReadOnlyDictionary<string, string> OperandFields { get; }

    /// <summary>
    /// Gets a mapping from logical result names to generated field names.
    /// </summary>
    public IReadOnlyDictionary<string, string> ResultFields { get; }

    /// <summary>
    /// Gets the generated field name for the trailing type component, if present.
    /// </summary>
    public string? TypeField { get; }

    /// <summary>
    /// Gets the generated field name for the successor list component, if present.
    /// </summary>
    public string? SuccessorsField { get; }

    /// <summary>
    /// Gets the generated field name for the aggregate operand list component, if present.
    /// </summary>
    public string? OperandsField { get; }
}

/// <summary>
/// Interprets operation body metadata and produces a construction plan for
/// synthesizing a generic operation body view.
/// </summary>
/// <remarks>
/// This helper centralizes the mapping from <see cref="BodyComponentKind"/>
/// values to generated field names so that downstream code can work with a
/// normalized description of the body layout.
/// </remarks>
internal static class OperationBodySyntaxDescriptor
{
    /// <summary>
    /// Builds a construction plan from the supplied operation body metadata.
    /// </summary>
    /// <param name="metadata">
    /// The metadata that describes the generated fields associated with the body components.
    /// </param>
    /// <returns>
    /// A construction plan that records which generated field stores each logical body component.
    /// </returns>
    /// <remarks>
    /// When multiple metadata entries of the same singleton component kind are encountered,
    /// this method keeps the first field name and ignores subsequent duplicates.
    /// Components that may appear multiple times, such as regions, attributes, operands,
    /// and results, are accumulated into their respective collections.
    /// </remarks>
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
                    // Preserve all region fields in declaration order.
                    regionFields.Add(component.FieldName);
                    break;
                case BodyComponentKind.Attribute:
                    // Map each logical attribute name to its generated storage field.
                    attributeFields[component.ComponentName] = component.FieldName;
                    break;
                case BodyComponentKind.Operand:
                    // Map each logical operand name to its generated storage field.
                    operandFields[component.ComponentName] = component.FieldName;
                    break;
                case BodyComponentKind.Result:
                    // Map each logical result name to its generated storage field.
                    resultFields[component.ComponentName] = component.FieldName;
                    break;
                case BodyComponentKind.AttrDict:
                    // Keep only the first attribute dictionary field, if any.
                    attrDictField ??= component.FieldName;
                    break;
                case BodyComponentKind.AttrDictWithKeyword:
                    // Keep only the first keyword-prefixed attribute dictionary field, if any.
                    attrDictWithKeywordField ??= component.FieldName;
                    break;
                case BodyComponentKind.PropDict:
                    // Keep only the first property dictionary field, if any.
                    propDictField ??= component.FieldName;
                    break;
                case BodyComponentKind.Successors:
                    // Keep only the first successor list field, if any.
                    successorsField ??= component.FieldName;
                    break;
                case BodyComponentKind.Operands:
                    // Keep only the first aggregate operands field, if any.
                    operandsField ??= component.FieldName;
                    break;
                case BodyComponentKind.Type:
                    // Keep only the first trailing type field, if any.
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
