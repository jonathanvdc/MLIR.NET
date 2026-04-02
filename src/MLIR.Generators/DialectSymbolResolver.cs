namespace MLIR.Generators;

using System;
using System.Collections.Generic;
using MLIR.ODS.Model;

internal sealed class DialectSymbolResolver
{
    private readonly Dictionary<string, string> attributeTypesByRecordName;
    private readonly Dictionary<string, string> attributeConstraintTypesByRecordName;
    private readonly Dictionary<string, string> typeTypesByRecordName;

    private DialectSymbolResolver(
        Dictionary<string, string> attributeTypesByRecordName,
        Dictionary<string, string> attributeConstraintTypesByRecordName,
        Dictionary<string, string> typeTypesByRecordName)
    {
        this.attributeTypesByRecordName = attributeTypesByRecordName;
        this.attributeConstraintTypesByRecordName = attributeConstraintTypesByRecordName;
        this.typeTypesByRecordName = typeTypesByRecordName;
    }

    public static DialectSymbolResolver Create(IReadOnlyList<DialectModel> dialects)
    {
        var attributeTypesByRecordName = new Dictionary<string, string>(StringComparer.Ordinal);
        var attributeConstraintTypesByRecordName = new Dictionary<string, string>(StringComparer.Ordinal);
        var typeTypesByRecordName = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var dialect in dialects)
        {
            var generatedNamespace = DialectGeneratorNaming.GetGeneratedNamespace(dialect);
            foreach (var attribute in dialect.Attributes)
            {
                attributeTypesByRecordName[attribute.RecordName] = generatedNamespace + "." + DialectGeneratorNaming.GetAttributeClassName(attribute);
            }

            foreach (var attributeConstraint in dialect.AttributeConstraints)
            {
                attributeConstraintTypesByRecordName[attributeConstraint.RecordName] = generatedNamespace + "." + DialectGeneratorNaming.GetAttributeConstraintClassName(attributeConstraint);
            }

            foreach (var type in dialect.Types)
            {
                typeTypesByRecordName[type.RecordName] = generatedNamespace + "." + DialectGeneratorNaming.GetTypeClassName(type);
            }
        }

        return new DialectSymbolResolver(attributeTypesByRecordName, attributeConstraintTypesByRecordName, typeTypesByRecordName);
    }

    public string? TryResolveAttributeDefinitionExpression(string recordName)
    {
        return attributeTypesByRecordName.TryGetValue(recordName, out var typeName)
            ? typeName + ".AttributeDefinition"
            : null;
    }

    public string? TryResolveAttributeConstraintDefinitionExpression(string recordName)
    {
        if (attributeTypesByRecordName.TryGetValue(recordName, out var attributeTypeName))
        {
            return attributeTypeName + ".AttributeDefinition";
        }

        return attributeConstraintTypesByRecordName.TryGetValue(recordName, out var constraintTypeName)
            ? constraintTypeName + ".AttributeConstraintDefinition"
            : null;
    }

    public string? TryResolveTypeDefinitionExpression(string recordName)
    {
        return typeTypesByRecordName.TryGetValue(recordName, out var typeName)
            ? typeName + ".TypeDefinition"
            : null;
    }
}
