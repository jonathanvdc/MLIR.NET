namespace MLIR.Generators.Emitters;

using System.Collections.Generic;

internal sealed class BodySyntaxField
{
    public BodySyntaxField(string name, string csType, string writeToCode)
    {
        Name = name;
        CsType = csType;
        WriteToCode = writeToCode;
    }

    public string Name { get; }
    public string CsType { get; }
    public string WriteToCode { get; }
}

internal sealed class BodyComponentField
{
    public BodyComponentField(BodyComponentKind kind, string componentName, string fieldName)
    {
        Kind = kind;
        ComponentName = componentName;
        FieldName = fieldName;
    }

    public BodyComponentKind Kind { get; }
    public string ComponentName { get; }
    public string FieldName { get; }
}

internal enum BodyComponentKind
{
    Literal,
    Attribute,
    Operand,
    Result,
    AttrDict,
    AttrDictWithKeyword,
    PropDict,
    Regions,
    Type,
    Successors,
    Operands,
    Unknown
}

internal sealed class OperationBodySyntaxMetadata
{
    private readonly List<BodySyntaxField> fields = new();
    private readonly List<BodyComponentField> componentFields = new();

    public OperationBodySyntaxMetadata(string operationClassName)
    {
        OperationClassName = operationClassName;
    }

    public string OperationClassName { get; }
    public string BodyClassName => OperationClassName + "BodySyntax";
    public IReadOnlyList<BodySyntaxField> Fields => fields;
    public IReadOnlyList<BodyComponentField> ComponentFields => componentFields;

    public void AddField(BodySyntaxField field) => fields.Add(field);
    public void AddComponentField(BodyComponentField componentField) => componentFields.Add(componentField);
}
