namespace MLIR.Generators;

using System;
using System.Collections.Generic;
using MLIR.ODS.Model;

internal sealed class DialectSymbolResolver
{
    private readonly Dictionary<string, string> attributeTypesByRecordName;
    private readonly Dictionary<string, string> attributeConstraintTypesByRecordName;
    private readonly Dictionary<string, AttributeConstraintKind> attributeConstraintKindsByRecordName;
    private readonly Dictionary<string, string> typeTypesByRecordName;

    private DialectSymbolResolver(
        Dictionary<string, string> attributeTypesByRecordName,
        Dictionary<string, string> attributeConstraintTypesByRecordName,
        Dictionary<string, AttributeConstraintKind> attributeConstraintKindsByRecordName,
        Dictionary<string, string> typeTypesByRecordName)
    {
        this.attributeTypesByRecordName = attributeTypesByRecordName;
        this.attributeConstraintTypesByRecordName = attributeConstraintTypesByRecordName;
        this.attributeConstraintKindsByRecordName = attributeConstraintKindsByRecordName;
        this.typeTypesByRecordName = typeTypesByRecordName;
    }

    public static DialectSymbolResolver Create(IReadOnlyList<DialectModel> dialects)
    {
        var attributeTypesByRecordName = new Dictionary<string, string>(StringComparer.Ordinal);
        var attributeConstraintTypesByRecordName = new Dictionary<string, string>(StringComparer.Ordinal);
        var attributeConstraintKindsByRecordName = new Dictionary<string, AttributeConstraintKind>(StringComparer.Ordinal);
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
                var className = generatedNamespace + "." + DialectGeneratorNaming.GetAttributeConstraintClassName(attributeConstraint);
                attributeConstraintTypesByRecordName[attributeConstraint.RecordName] = className;
                attributeConstraintKindsByRecordName[attributeConstraint.RecordName] = attributeConstraint.Kind;
            }

            foreach (var type in dialect.Types)
            {
                typeTypesByRecordName[type.RecordName] = generatedNamespace + "." + DialectGeneratorNaming.GetTypeClassName(type);
            }
        }

        return new DialectSymbolResolver(attributeTypesByRecordName, attributeConstraintTypesByRecordName, attributeConstraintKindsByRecordName, typeTypesByRecordName);
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

    public AttributeConstraintKind TryResolveAttributeConstraintKind(string recordName)
    {
        return attributeConstraintKindsByRecordName.TryGetValue(recordName, out var kind) ? kind : AttributeConstraintKind.None;
    }

    public string? TryResolveAttributeConstraintClassName(string recordName)
    {
        return attributeConstraintTypesByRecordName.TryGetValue(recordName, out var className) ? className : null;
    }

    public string? TryResolveTypeDefinitionExpression(string recordName)
    {
        return typeTypesByRecordName.TryGetValue(recordName, out var typeName)
            ? typeName + ".TypeDefinition"
            : null;
    }
}
