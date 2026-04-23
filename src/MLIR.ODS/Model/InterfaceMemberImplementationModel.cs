namespace MLIR.ODS.Model;

/// <summary>
/// Represents one concrete implementation for a generated C# interface member on a type.
/// </summary>
public sealed class InterfaceMemberImplementationModel(
    string interfaceRecordName,
    string csharpMemberName,
    string csharpExpression)
{
    /// <summary>
    /// Gets the TableGen record name of the interface whose member is being implemented.
    /// </summary>
    public string InterfaceRecordName { get; } = interfaceRecordName;

    /// <summary>
    /// Gets the C# member name being implemented.
    /// </summary>
    public string CsharpMemberName { get; } = csharpMemberName;

    /// <summary>
    /// Gets the raw C# expression used by the generated property body.
    /// </summary>
    public string CsharpExpression { get; } = csharpExpression;

    /// <summary>
    /// Gets the implementation expression as a normalized code template.
    /// </summary>
    public CodeTemplate? CsharpExpressionTemplate => CodeTemplate.From(CsharpExpression, CodeTemplateKind.Expression);
}
