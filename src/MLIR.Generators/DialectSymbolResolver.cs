namespace MLIR.Generators;

using System;
using System.Collections.Generic;
using MLIR.ODS.Model;

internal sealed class DialectSymbolResolver
{
    private readonly Dictionary<string, string> attributeTypesByRecordName;
    private readonly Dictionary<string, string> typeTypesByRecordName;

    private DialectSymbolResolver(
        Dictionary<string, string> attributeTypesByRecordName,
        Dictionary<string, string> typeTypesByRecordName)
    {
        this.attributeTypesByRecordName = attributeTypesByRecordName;
        this.typeTypesByRecordName = typeTypesByRecordName;
    }

    public static DialectSymbolResolver Create(IReadOnlyList<DialectModel> dialects)
    {
        var attributeTypesByRecordName = new Dictionary<string, string>(StringComparer.Ordinal);
        var typeTypesByRecordName = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var dialect in dialects)
        {
            var generatedNamespace = DialectGeneratorNaming.GetGeneratedNamespace(dialect);
            foreach (var attribute in dialect.Attributes)
            {
                attributeTypesByRecordName[attribute.RecordName] = generatedNamespace + "." + DialectGeneratorNaming.GetAttributeClassName(attribute);
            }

            foreach (var type in dialect.Types)
            {
                typeTypesByRecordName[type.RecordName] = generatedNamespace + "." + DialectGeneratorNaming.GetTypeClassName(type);
            }
        }

        return new DialectSymbolResolver(attributeTypesByRecordName, typeTypesByRecordName);
    }

    public string? TryResolveAttributeDefinitionExpression(string recordName)
    {
        return attributeTypesByRecordName.TryGetValue(recordName, out var typeName)
            ? typeName + ".AttributeDefinition"
            : null;
    }

    public string? TryResolveTypeDefinitionExpression(string recordName)
    {
        return typeTypesByRecordName.TryGetValue(recordName, out var typeName)
            ? typeName + ".TypeDefinition"
            : null;
    }
}
