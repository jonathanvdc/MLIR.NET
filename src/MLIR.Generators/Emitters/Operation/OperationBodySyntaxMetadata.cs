namespace MLIR.Generators.Emitters.Operation;

using System.Collections.Generic;
using MLIR.Generators.Emitters.AssemblyFormat;

internal sealed class BodySyntaxField : AssemblyFormatSyntaxField
{
    public BodySyntaxField(string name, string csType, string writeToCode)
        : this(AssemblyFormatSyntaxFieldKind.Unknown, name, csType, writeToCode)
    {
    }

    public BodySyntaxField(AssemblyFormatSyntaxFieldKind kind, string name, string csType, string writeToCode)
        : base(kind, name, csType, writeToCode)
    {
    }
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
    TypeDirective,
    ResultsDirective,
    FunctionalTypeDirective,
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
