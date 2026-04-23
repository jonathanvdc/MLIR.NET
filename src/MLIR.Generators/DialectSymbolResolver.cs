namespace MLIR.Generators;

using System;
using System.Collections.Generic;
using MLIR.ODS.Model;

internal sealed class DialectSymbolResolver
{
    private readonly Dictionary<string, string> attributeTypesByRecordName;
    private readonly Dictionary<string, AttrModel> attrsByRecordName;
    private readonly Dictionary<string, string> attributeConstraintTypesByRecordName;
    private readonly Dictionary<string, AttributeConstraintCodeStrategy> attributeConstraintStrategiesByRecordName;
    private readonly Dictionary<string, string?> attributeConstraintElementRecordNamesByRecordName;
    private readonly Dictionary<string, string> enumTypesByRecordName;
    private readonly Dictionary<string, string> typeConstraintTypesByRecordName;
    private readonly Dictionary<string, string> typeTypesByRecordName;
    // Maps (cppNamespace + "\0" + cppInterfaceName) → fully qualified C# marker interface name.
    private readonly Dictionary<string, string> typeInterfaceNamesByKey;
    private readonly Dictionary<string, InterfaceModel> typeInterfacesByKey;

    private DialectSymbolResolver(
        Dictionary<string, string> attributeTypesByRecordName,
        Dictionary<string, AttrModel> attrsByRecordName,
        Dictionary<string, string> attributeConstraintTypesByRecordName,
        Dictionary<string, AttributeConstraintCodeStrategy> attributeConstraintStrategiesByRecordName,
        Dictionary<string, string?> attributeConstraintElementRecordNamesByRecordName,
        Dictionary<string, string> enumTypesByRecordName,
        Dictionary<string, string> typeConstraintTypesByRecordName,
        Dictionary<string, string> typeTypesByRecordName,
        Dictionary<string, string> typeInterfaceNamesByKey,
        Dictionary<string, InterfaceModel> typeInterfacesByKey)
    {
        this.attributeTypesByRecordName = attributeTypesByRecordName;
        this.attrsByRecordName = attrsByRecordName;
        this.attributeConstraintTypesByRecordName = attributeConstraintTypesByRecordName;
        this.attributeConstraintStrategiesByRecordName = attributeConstraintStrategiesByRecordName;
        this.attributeConstraintElementRecordNamesByRecordName = attributeConstraintElementRecordNamesByRecordName;
        this.enumTypesByRecordName = enumTypesByRecordName;
        this.typeConstraintTypesByRecordName = typeConstraintTypesByRecordName;
        this.typeTypesByRecordName = typeTypesByRecordName;
        this.typeInterfaceNamesByKey = typeInterfaceNamesByKey;
        this.typeInterfacesByKey = typeInterfacesByKey;
    }

    public static DialectSymbolResolver Create(IReadOnlyList<DialectModel> dialects)
    {
        var attributeTypesByRecordName = new Dictionary<string, string>(StringComparer.Ordinal);
        var attrsByRecordName = new Dictionary<string, AttrModel>(StringComparer.Ordinal);
        var attributeConstraintTypesByRecordName = new Dictionary<string, string>(StringComparer.Ordinal);
        var attributeConstraintStrategiesByRecordName = new Dictionary<string, AttributeConstraintCodeStrategy>(StringComparer.Ordinal);
        var attributeConstraintElementRecordNamesByRecordName = new Dictionary<string, string?>(StringComparer.Ordinal);
        var enumTypesByRecordName = new Dictionary<string, string>(StringComparer.Ordinal);
        var typeConstraintTypesByRecordName = new Dictionary<string, string>(StringComparer.Ordinal);
        var typeTypesByRecordName = new Dictionary<string, string>(StringComparer.Ordinal);
        var typeInterfaceNamesByKey = new Dictionary<string, string>(StringComparer.Ordinal);
        var typeInterfacesByKey = new Dictionary<string, InterfaceModel>(StringComparer.Ordinal);

        foreach (var dialect in dialects)
        {
            var generatedNamespace = DialectGeneratorNaming.GetGeneratedNamespace(dialect);
            foreach (var attribute in dialect.Attributes)
            {
                var attributeClassName = generatedNamespace + "." + DialectGeneratorNaming.GetAttributeClassName(attribute);
                attributeTypesByRecordName[attribute.RecordName] = attributeClassName;
                if (attribute.EnumModel != null)
                {
                    var enumTypeName = generatedNamespace + "." + EnumHelpers.GetCSharpEnumTypeName(attribute.EnumModel);
                    enumTypesByRecordName[attribute.RecordName] = enumTypeName;
                    attributeConstraintStrategiesByRecordName[attribute.RecordName] =
                        AttributeConstraintCodeStrategyFactory.GetEnumAttributeStrategy(
                            attribute.RecordName,
                            attribute.EnumModel,
                            enumTypeName,
                            attributeClassName);
                }
            }

            foreach (var attr in dialect.Attrs)
            {
                attrsByRecordName[attr.RecordName] = attr;
            }

            foreach (var attributeConstraint in dialect.AttributeConstraints)
            {
                var className = generatedNamespace + "." + DialectGeneratorNaming.GetAttributeConstraintClassName(attributeConstraint);
                attributeConstraintTypesByRecordName[attributeConstraint.RecordName] = className;
                attributeConstraintElementRecordNamesByRecordName[attributeConstraint.RecordName] = attributeConstraint.ElementConstraintRecordName;
                if (attributeConstraint.EnumModel != null)
                {
                    enumTypesByRecordName[attributeConstraint.RecordName] = generatedNamespace + "." + EnumHelpers.GetCSharpEnumTypeName(attributeConstraint.EnumModel);
                }

                var strategy = AttributeConstraintCodeStrategyFactory.GetStrategy(
                    attributeConstraint,
                    attrsByRecordName.TryGetValue(attributeConstraint.RecordName, out var attrModel)
                        && ShouldUseAttrModelTyping(attrModel)
                        ? attrModel
                        : null,
                    enumTypesByRecordName.TryGetValue(attributeConstraint.RecordName, out var enumTypeName)
                        ? enumTypeName
                        : null);
                attributeConstraintStrategiesByRecordName[attributeConstraint.RecordName] = strategy;
            }

            foreach (var type in dialect.Types)
            {
                typeTypesByRecordName[type.RecordName] = generatedNamespace + "." + DialectGeneratorNaming.GetTypeClassName(type);
            }

            foreach (var typeConstraint in dialect.TypeConstraints)
            {
                typeConstraintTypesByRecordName[typeConstraint.RecordName] = generatedNamespace + "." + DialectGeneratorNaming.GetTypeConstraintClassName(typeConstraint);
            }

            // Register type interfaces so type classes can reference them by cppNamespace+cppInterfaceName.
            foreach (var interfaceModel in dialect.Interfaces)
            {
                if (interfaceModel.Kind != InterfaceKind.Type)
                {
                    continue;
                }

                var key = MakeTypeInterfaceKey(interfaceModel.CppNamespace, interfaceModel.CppInterfaceName);
                var qualifiedName = generatedNamespace + "." + DialectGeneratorNaming.GetTypeInterfaceName(interfaceModel);
                typeInterfaceNamesByKey[key] = qualifiedName;
                typeInterfacesByKey[key] = interfaceModel;
            }
        }

        return new DialectSymbolResolver(
            attributeTypesByRecordName,
            attrsByRecordName,
            attributeConstraintTypesByRecordName,
            attributeConstraintStrategiesByRecordName,
            attributeConstraintElementRecordNamesByRecordName,
            enumTypesByRecordName,
            typeConstraintTypesByRecordName,
            typeTypesByRecordName,
            typeInterfaceNamesByKey,
            typeInterfacesByKey);
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

    /// <summary>
    /// Returns the code-generation strategy for the attribute constraint identified by
    /// <paramref name="recordName"/>.  Always returns a non-null value: records that have a
    /// specialised strategy return it; all others return
    /// <see cref="FallbackAttributeConstraintCodeStrategy.Instance"/>.
    /// </summary>
    public AttributeConstraintCodeStrategy TryResolveAttributeConstraintStrategy(string recordName)
    {
        return attributeConstraintStrategiesByRecordName.TryGetValue(recordName, out var strategy)
            ? strategy
            : FallbackAttributeConstraintCodeStrategy.Instance;
    }

    public string? TryResolveAttributeConstraintClassName(string recordName)
    {
        if (attributeTypesByRecordName.TryGetValue(recordName, out var attributeClassName))
        {
            return attributeClassName;
        }

        return attributeConstraintTypesByRecordName.TryGetValue(recordName, out var className) ? className : null;
    }

    public string? TryResolveAttributeConstraintElementRecordName(string recordName)
    {
        return attributeConstraintElementRecordNamesByRecordName.TryGetValue(recordName, out var elementRecordName)
            ? elementRecordName
            : null;
    }

    public AttrModel? TryResolveAttrModel(string recordName)
    {
        return attrsByRecordName.TryGetValue(recordName, out var attr) ? attr : null;
    }

    public string? TryResolveEnumTypeName(string recordName)
    {
        return enumTypesByRecordName.TryGetValue(recordName, out var enumTypeName) ? enumTypeName : null;
    }

    private static bool ShouldUseAttrModelTyping(AttrModel attrModel)
    {
        var returnType = attrModel.CsharpReturnType;
        if (string.IsNullOrEmpty(returnType)
            || string.Equals(returnType, "AttributeValue", StringComparison.Ordinal)
            || string.Equals(returnType, "global::MLIR.Semantics.AttributeValue", StringComparison.Ordinal))
        {
            return false;
        }

        // SymbolRefAttr storage is also a concrete semantic AttributeValue that carries
        // symbol-specific behavior; keep those properties on the storage class unless the
        // ODS record defines a more specific wrapper.
        return !string.Equals(attrModel.CsharpStorageType, "global::MLIR.Dialects.Builtin.SymbolRefAttr", StringComparison.Ordinal);
    }

    /// <summary>
    /// Returns the C# expression that evaluates to the <c>TypeDefinition</c> for a concrete
    /// ODS <c>TypeDef</c> record, or <see langword="null"/> when no TypeDef record with
    /// <paramref name="recordName"/> is known.
    /// </summary>
    /// <remarks>
    /// Only resolves concrete <c>TypeDef</c> records.  ODS <c>Type</c> constraint records are
    /// intentionally excluded; use <see cref="TryResolveTypeConstraintDefinitionExpression"/>
    /// for those.
    /// </remarks>
    public string? TryResolveTypeDefinitionExpression(string recordName)
    {
        return typeTypesByRecordName.TryGetValue(recordName, out var typeName)
            ? typeName + ".TypeDefinition"
            : null;
    }

    /// <summary>
    /// Returns the C# expression that evaluates to a <c>TypeConstraintDefinition</c> for
    /// any type record (either an ODS <c>TypeDef</c> or an ODS <c>Type</c> constraint), or
    /// <see langword="null"/> when the record name is not known.
    /// </summary>
    /// <remarks>
    /// For concrete <c>TypeDef</c> records this returns <c>Foo.TypeDefinition</c> because
    /// <c>TypeDefinition</c> derives from <c>TypeConstraintDefinition</c>.
    /// For ODS <c>Type</c> constraint records this returns <c>Foo.TypeConstraintDefinition</c>.
    /// </remarks>
    public string? TryResolveTypeConstraintDefinitionExpression(string recordName)
    {
        if (typeTypesByRecordName.TryGetValue(recordName, out var typeName))
        {
            return typeName + ".TypeDefinition";
        }

        return typeConstraintTypesByRecordName.TryGetValue(recordName, out var typeConstraintName)
            ? typeConstraintName + ".TypeConstraintDefinition"
            : null;
    }

    /// <summary>
    /// Returns the fully qualified C# marker interface name for the type interface identified
    /// by <paramref name="cppNamespace"/> and <paramref name="cppInterfaceName"/>, or
    /// <see langword="null"/> when no type interface with that identity has been registered.
    /// </summary>
    public string? TryResolveTypeInterfaceName(string? cppNamespace, string cppInterfaceName)
    {
        var key = MakeTypeInterfaceKey(cppNamespace, cppInterfaceName);
        return typeInterfaceNamesByKey.TryGetValue(key, out var name) ? name : null;
    }

    public InterfaceModel? TryResolveTypeInterfaceModel(string? cppNamespace, string cppInterfaceName)
    {
        var key = MakeTypeInterfaceKey(cppNamespace, cppInterfaceName);
        return typeInterfacesByKey.TryGetValue(key, out var model) ? model : null;
    }

    private static string MakeTypeInterfaceKey(string? cppNamespace, string cppInterfaceName)
    {
        return (cppNamespace ?? string.Empty) + "\0" + cppInterfaceName;
    }
}
