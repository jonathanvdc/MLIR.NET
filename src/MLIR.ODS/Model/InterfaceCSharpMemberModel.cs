namespace MLIR.ODS.Model;

/// <summary>
/// Represents one explicit C# surface member mapped onto an upstream MLIR interface method.
/// </summary>
public sealed class InterfaceCSharpMemberModel(
    InterfaceCSharpMemberKind kind,
    string upstreamName,
    string csharpType,
    string csharpName,
    string? csharpSummary = null,
    string? csharpDescription = null)
{
    /// <summary>
    /// Gets the kind of C# member to emit.
    /// </summary>
    public InterfaceCSharpMemberKind Kind { get; } = kind;

    /// <summary>
    /// Gets the upstream MLIR interface method name that this member maps from.
    /// </summary>
    public string UpstreamName { get; } = upstreamName;

    /// <summary>
    /// Gets the C# type exposed by this mapped member.
    /// </summary>
    public string CsharpType { get; } = csharpType;

    /// <summary>
    /// Gets the C# member name to emit.
    /// </summary>
    public string CsharpName { get; } = csharpName;

    /// <summary>
    /// Gets the optional C# XML documentation summary override supplied by MLIR.NET overlay metadata.
    /// </summary>
    public string? CsharpSummary { get; } = csharpSummary;

    /// <summary>
    /// Gets the optional C# XML documentation description override supplied by MLIR.NET overlay metadata.
    /// When absent, the mapped upstream interface method description remains the default remarks source.
    /// </summary>
    public string? CsharpDescription { get; } = csharpDescription;
}
